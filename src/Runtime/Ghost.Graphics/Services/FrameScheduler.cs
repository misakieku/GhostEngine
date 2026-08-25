using Ghost.Graphics.FrameScheduling;
using Ghost.Graphics.RHI;

namespace Ghost.Graphics.Services;

internal sealed class FrameScheduler : IFrameScheduler
{
    private readonly struct SubmissionRecord
    {
        public readonly ICommandBuffer commandBuffer;
        public readonly SubmissionHandle handle;

        public SubmissionRecord(ICommandBuffer commandBuffer, SubmissionHandle handle)
        {
            this.commandBuffer = commandBuffer;
            this.handle = handle;
        }
    }

    private readonly struct SubmissionDependency
    {
        public readonly SubmissionHandle producer;
        public readonly int dependentIndex;

        public SubmissionDependency(SubmissionHandle producer, int dependentIndex)
        {
            this.producer = producer;
            this.dependentIndex = dependentIndex;
        }
    }

    private const int QUEUE_COUNT = 3;

    private static int s_nextSchedulerId;

    private readonly IGraphicsEngine _graphicsEngine;
    private readonly SwapChainManager? _swapChainManager;
    private readonly ICommandQueue[] _queues;
    private readonly IFence[] _fences;
    private readonly ulong[] _nextFenceValues;
    private readonly SubmissionHandle[] _latestSubmissions;
    private readonly SubmissionHandle[] _flushedSubmissions;
    private readonly int[] _currentQueueTailIndices;
    private readonly List<SubmissionHandle>[] _pendingDependencies;
    private readonly List<SubmissionRecord> _submissions;
    private readonly List<SubmissionDependency> _dependencies;
    private readonly SubmissionHandle[] _transactionLatestSubmissions;
    private readonly int[] _transactionQueueTailIndices;
    private readonly List<SubmissionHandle>[] _transactionPendingDependencies;
    private readonly List<SubmissionHandle> _invalidatedSubmissions;
    private readonly ulong[,] _lastWaitedFenceValues;

    private int[] _indegrees;
    private int[] _executionOrder;
    private bool[] _scheduled;
    private readonly int _schedulerId;

    private int _transactionSubmissionCount;
    private int _transactionDependencyCount;
    private uint _nextTransactionId;
    private SubmissionTransaction _activeTransaction;
    private uint _generation;
    private bool _disposed;

    public ulong SubmittedFrame
    {
        get;
        private set;
    }

    public FrameScheduler(IGraphicsEngine graphicsEngine, SwapChainManager? swapChainManager = null)
    {
        ArgumentNullException.ThrowIfNull(graphicsEngine);

        _graphicsEngine = graphicsEngine;
        _swapChainManager = swapChainManager;
        _queues = new ICommandQueue[QUEUE_COUNT];
        _fences = new IFence[QUEUE_COUNT];
        _nextFenceValues = new ulong[QUEUE_COUNT];
        _latestSubmissions = new SubmissionHandle[QUEUE_COUNT];
        _flushedSubmissions = new SubmissionHandle[QUEUE_COUNT];
        _currentQueueTailIndices = new int[QUEUE_COUNT];
        _pendingDependencies = new List<SubmissionHandle>[QUEUE_COUNT];
        _submissions = new List<SubmissionRecord>(16);
        _dependencies = new List<SubmissionDependency>(16);
        _transactionLatestSubmissions = new SubmissionHandle[QUEUE_COUNT];
        _transactionQueueTailIndices = new int[QUEUE_COUNT];
        _transactionPendingDependencies = new List<SubmissionHandle>[QUEUE_COUNT];
        _invalidatedSubmissions = new List<SubmissionHandle>(4);
        _lastWaitedFenceValues = new ulong[QUEUE_COUNT, QUEUE_COUNT];
        _indegrees = Array.Empty<int>();
        _executionOrder = Array.Empty<int>();
        _scheduled = Array.Empty<bool>();

        _queues[GetQueueIndex(CommandQueueType.Graphics)] = graphicsEngine.Device.GraphicsQueue;
        _queues[GetQueueIndex(CommandQueueType.Compute)] = graphicsEngine.Device.ComputeQueue;
        _queues[GetQueueIndex(CommandQueueType.Copy)] = graphicsEngine.Device.CopyQueue;

        for (var i = 0; i < QUEUE_COUNT; i++)
        {
            _fences[i] = graphicsEngine.CreateFence();
            _fences[i].Name = $"FrameScheduler_{(CommandQueueType)i}_Fence";
            _pendingDependencies[i] = new List<SubmissionHandle>(2);
            _transactionPendingDependencies[i] = new List<SubmissionHandle>(2);
            _currentQueueTailIndices[i] = -1;
        }

        _schedulerId = Interlocked.Increment(ref s_nextSchedulerId);
        _generation = 1;
    }

    public ICommandBuffer GetPooledCommandBuffer(CommandBufferType type = CommandBufferType.Graphics)
    {
        ThrowIfDisposed();
        return _graphicsEngine.GetPooledCommandBuffer(type);
    }

    public void ReturnPooledCommandBuffer(ICommandBuffer commandBuffer)
    {
        ThrowIfDisposed();
        _graphicsEngine.ReturnPooledCommandBuffer(commandBuffer);
    }

    public void PrepareSubmissions(int additionalSubmissionCount)
    {
        ThrowIfDisposed();
        ArgumentOutOfRangeException.ThrowIfNegative(additionalSubmissionCount);
        EnsureSubmissionCapacity(additionalSubmissionCount, 0);
    }

    public SubmissionTransaction BeginSubmissionTransaction(int additionalSubmissionCount, int additionalDependencyCount)
    {
        ThrowIfDisposed();
        ArgumentOutOfRangeException.ThrowIfNegative(additionalSubmissionCount);
        ArgumentOutOfRangeException.ThrowIfNegative(additionalDependencyCount);
        if (_activeTransaction.IsValid)
        {
            throw new InvalidOperationException("A frame-scheduler submission transaction is already active.");
        }

        EnsureSubmissionCapacity(additionalSubmissionCount, additionalDependencyCount);

        _transactionSubmissionCount = _submissions.Count;
        _transactionDependencyCount = _dependencies.Count;
        Array.Copy(_latestSubmissions, _transactionLatestSubmissions, QUEUE_COUNT);
        Array.Copy(_currentQueueTailIndices, _transactionQueueTailIndices, QUEUE_COUNT);
        for (var i = 0; i < QUEUE_COUNT; i++)
        {
            var snapshot = _transactionPendingDependencies[i];
            snapshot.Clear();
            snapshot.AddRange(_pendingDependencies[i]);
        }

        _nextTransactionId++;
        if (_nextTransactionId == 0)
        {
            _nextTransactionId = 1;
        }

        _activeTransaction = new SubmissionTransaction(_schedulerId, _generation, _nextTransactionId);
        return _activeTransaction;
    }

    public void CommitSubmissionTransaction(SubmissionTransaction transaction)
    {
        ThrowIfDisposed();
        ValidateTransaction(transaction);
        CompleteSubmissionTransaction();
    }

    public void RollbackSubmissionTransaction(SubmissionTransaction transaction)
    {
        ThrowIfDisposed();
        ValidateTransaction(transaction);

        for (var i = _transactionSubmissionCount; i < _submissions.Count; i++)
        {
            var submission = _submissions[i];
            _invalidatedSubmissions.Add(submission.handle);
            _graphicsEngine.ReturnPooledCommandBuffer(submission.commandBuffer);
        }

        _submissions.RemoveRange(_transactionSubmissionCount, _submissions.Count - _transactionSubmissionCount);
        _dependencies.RemoveRange(_transactionDependencyCount, _dependencies.Count - _transactionDependencyCount);
        Array.Copy(_transactionLatestSubmissions, _latestSubmissions, QUEUE_COUNT);
        Array.Copy(_transactionQueueTailIndices, _currentQueueTailIndices, QUEUE_COUNT);
        for (var i = 0; i < QUEUE_COUNT; i++)
        {
            var pendingDependencies = _pendingDependencies[i];
            pendingDependencies.Clear();
            pendingDependencies.AddRange(_transactionPendingDependencies[i]);
        }

        CompleteSubmissionTransaction();
    }

    public SubmissionHandle Submit(ICommandBuffer commandBuffer)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(commandBuffer);

        if (commandBuffer.State.IsRecording)
        {
            throw new InvalidOperationException("A command buffer must be ended before it is submitted to the frame scheduler.");
        }

        var queueType = GetQueueType(commandBuffer.Type);
        var queueIndex = GetQueueIndex(queueType);
        var submissionIndex = _submissions.Count;
        var handle = new SubmissionHandle(
            _schedulerId,
            submissionIndex,
            _generation,
            queueType,
            ++_nextFenceValues[queueIndex]);

        _submissions.Add(new SubmissionRecord(commandBuffer, handle));

        var previousSubmissionIndex = _currentQueueTailIndices[queueIndex];
        if (previousSubmissionIndex >= 0)
        {
            AddDependencyCore(_submissions[previousSubmissionIndex].handle, submissionIndex);
        }

        var pendingDependencies = _pendingDependencies[queueIndex];
        for (var i = 0; i < pendingDependencies.Count; i++)
        {
            AddDependencyCore(pendingDependencies[i], submissionIndex);
        }
        pendingDependencies.Clear();

        _currentQueueTailIndices[queueIndex] = submissionIndex;
        _latestSubmissions[queueIndex] = handle;
        return handle;
    }

    public void AddDependency(SubmissionHandle producer, SubmissionHandle dependent)
    {
        ThrowIfDisposed();
        ValidateHandle(producer, nameof(producer));
        ValidateCurrentSubmission(dependent, nameof(dependent));

        if (producer == dependent)
        {
            throw new InvalidOperationException("A submission cannot depend on itself.");
        }

        AddDependencyCore(producer, dependent.SubmissionIndex);
    }

    public void Transition(CommandQueueType source, CommandQueueType destination)
    {
        ThrowIfDisposed();

        if (source == destination)
        {
            return;
        }

        var producer = _latestSubmissions[GetQueueIndex(source)];
        if (!producer.IsValid)
        {
            throw new InvalidOperationException($"Queue '{source}' has no submission to transition from.");
        }

        var pendingDependencies = _pendingDependencies[GetQueueIndex(destination)];
        for (var i = 0; i < pendingDependencies.Count; i++)
        {
            if (pendingDependencies[i] == producer)
            {
                return;
            }
        }

        pendingDependencies.Add(producer);
    }

    public bool IsComplete(SubmissionHandle submission)
    {
        ThrowIfDisposed();
        if (!submission.IsValid)
        {
            return true;
        }

        ValidateHandle(submission, nameof(submission));
        return _fences[GetQueueIndex(submission.QueueType)].CompletedValue >= submission.FenceValue;
    }

    public FrameCompletionInfo Flush()
    {
        ThrowIfDisposed();
        if (_activeTransaction.IsValid)
        {
            throw new InvalidOperationException("An active submission transaction must be committed or rolled back before flushing.");
        }

        var submissionCount = _submissions.Count;
        EnsureScratchCapacity(submissionCount);
        Array.Clear(_scheduled, 0, submissionCount);

        try
        {
            ValidateNoUnresolvedTransitions();
            ExecutePendingSubmissions();
            SubmittedFrame++;

            Array.Copy(_latestSubmissions, _flushedSubmissions, QUEUE_COUNT);
            var completion = new FrameCompletionInfo(
                SubmittedFrame,
                _flushedSubmissions[GetQueueIndex(CommandQueueType.Graphics)],
                _flushedSubmissions[GetQueueIndex(CommandQueueType.Compute)],
                _flushedSubmissions[GetQueueIndex(CommandQueueType.Copy)]);

            ResetPendingState();
            return completion;
        }
        catch
        {
            ReturnUnsubmittedCommandBuffers();
            InvalidatePendingSubmissions();
            Array.Copy(_flushedSubmissions, _latestSubmissions, QUEUE_COUNT);
            ResetPendingState();
            throw;
        }
    }

    public void WaitForFrame(scoped in FrameCompletionInfo completion)
    {
        ThrowIfDisposed();
        if (!completion.IsValid)
        {
            return;
        }

        WaitForSubmission(completion.GraphicsSubmission);
        WaitForSubmission(completion.ComputeSubmission);
        WaitForSubmission(completion.CopySubmission);
    }

    public void WaitIdle()
    {
        ThrowIfDisposed();
        if (_submissions.Count > 0)
        {
            Flush();
        }

        for (var i = 0; i < QUEUE_COUNT; i++)
        {
            var fenceValue = ++_nextFenceValues[i];
            _queues[i].Signal(_fences[i], fenceValue);
            _fences[i].WaitForValue(fenceValue);
        }
    }

    private void ExecutePendingSubmissions()
    {
        var submissionCount = _submissions.Count;
        if (submissionCount == 0)
        {
            return;
        }

        Array.Clear(_lastWaitedFenceValues, 0, QUEUE_COUNT * QUEUE_COUNT);
        Array.Clear(_indegrees, 0, submissionCount);
        Array.Clear(_scheduled, 0, submissionCount);

        for (var i = 0; i < _dependencies.Count; i++)
        {
            var dependency = _dependencies[i];
            if (dependency.producer.Generation == _generation)
            {
                _indegrees[dependency.dependentIndex]++;
            }
        }

        for (var orderIndex = 0; orderIndex < submissionCount; orderIndex++)
        {
            var submissionIndex = FindNextReadySubmission(submissionCount);
            if (submissionIndex < 0)
            {
                Array.Clear(_scheduled, 0, submissionCount);
                throw new InvalidOperationException("The frame submission graph contains a dependency cycle.");
            }

            _executionOrder[orderIndex] = submissionIndex;
            _scheduled[submissionIndex] = true;

            for (var i = 0; i < _dependencies.Count; i++)
            {
                var dependency = _dependencies[i];
                if (dependency.producer.Generation == _generation && dependency.producer.SubmissionIndex == submissionIndex)
                {
                    _indegrees[dependency.dependentIndex]--;
                }
            }
        }

        var lastGraphicsSubmissionIndex = -1;
        for (var orderIndex = submissionCount - 1; orderIndex >= 0; orderIndex--)
        {
            var idx = _executionOrder[orderIndex];
            if (_submissions[idx].handle.QueueType == CommandQueueType.Graphics)
            {
                lastGraphicsSubmissionIndex = idx;
                break;
            }
        }

        Array.Clear(_scheduled, 0, submissionCount);
        for (var orderIndex = 0; orderIndex < submissionCount; orderIndex++)
        {
            var submissionIndex = _executionOrder[orderIndex];
            ExecuteSubmission(submissionIndex, submissionIndex == lastGraphicsSubmissionIndex);
            _scheduled[submissionIndex] = true;
        }
    }

    private int FindNextReadySubmission(int submissionCount)
    {
        for (var i = 0; i < submissionCount; i++)
        {
            if (!_scheduled[i] && _indegrees[i] == 0)
            {
                return i;
            }
        }

        return -1;
    }

    private void ExecuteSubmission(int submissionIndex, bool isLastGraphicsSubmission)
    {
        var record = _submissions[submissionIndex];
        var destinationQueueIndex = GetQueueIndex(record.handle.QueueType);
        Span<ulong> waitValues = stackalloc ulong[QUEUE_COUNT];
        waitValues.Clear();

        for (var i = 0; i < _dependencies.Count; i++)
        {
            var dependency = _dependencies[i];
            if (dependency.dependentIndex != submissionIndex || dependency.producer.QueueType == record.handle.QueueType)
            {
                continue;
            }

            var sourceQueueIndex = GetQueueIndex(dependency.producer.QueueType);
            waitValues[sourceQueueIndex] = Math.Max(waitValues[sourceQueueIndex], dependency.producer.FenceValue);
        }

        for (var i = 0; i < QUEUE_COUNT; i++)
        {
            if (waitValues[i] != 0 && waitValues[i] > _lastWaitedFenceValues[destinationQueueIndex, i])
            {
                _queues[destinationQueueIndex].Wait(_fences[i], waitValues[i]);
                _lastWaitedFenceValues[destinationQueueIndex, i] = waitValues[i];
            }
        }

        _queues[destinationQueueIndex].Submit(record.commandBuffer);

        if (isLastGraphicsSubmission && _swapChainManager != null)
        {
            _swapChainManager.PresentAll();
        }

        _queues[destinationQueueIndex].Signal(_fences[destinationQueueIndex], record.handle.FenceValue);
        _graphicsEngine.ReturnPooledCommandBuffer(record.commandBuffer);
    }

    private void AddDependencyCore(SubmissionHandle producer, int dependentIndex)
    {
        for (var i = 0; i < _dependencies.Count; i++)
        {
            var dependency = _dependencies[i];
            if (dependency.producer == producer && dependency.dependentIndex == dependentIndex)
            {
                return;
            }
        }

        _dependencies.Add(new SubmissionDependency(producer, dependentIndex));
    }

    private void ReturnUnsubmittedCommandBuffers()
    {
        for (var i = 0; i < _submissions.Count; i++)
        {
            if (_scheduled[i])
            {
                continue;
            }

            _graphicsEngine.ReturnPooledCommandBuffer(_submissions[i].commandBuffer);
        }
    }

    private void InvalidatePendingSubmissions()
    {
        for (var i = 0; i < _submissions.Count; i++)
        {
            _invalidatedSubmissions.Add(_submissions[i].handle);
        }
    }

    private void WaitForSubmission(SubmissionHandle submission)
    {
        if (!submission.IsValid)
        {
            return;
        }

        ValidateHandle(submission, nameof(submission));
        _fences[GetQueueIndex(submission.QueueType)].WaitForValue(submission.FenceValue);
    }

    private void ValidateNoUnresolvedTransitions()
    {
        for (var i = 0; i < QUEUE_COUNT; i++)
        {
            if (_pendingDependencies[i].Count > 0)
            {
                throw new InvalidOperationException($"Queue '{(CommandQueueType)i}' has a transition without a destination submission.");
            }
        }
    }

    private void ValidateHandle(SubmissionHandle submission, string parameterName)
    {
        if (!submission.IsValid || submission.SchedulerId != _schedulerId)
        {
            throw new ArgumentException("The submission handle does not belong to this frame scheduler.", parameterName);
        }

        for (var i = 0; i < _invalidatedSubmissions.Count; i++)
        {
            if (_invalidatedSubmissions[i] == submission)
            {
                throw new ArgumentException("The submission handle was invalidated by transaction rollback.", parameterName);
            }
        }
    }

    private void ValidateCurrentSubmission(SubmissionHandle submission, string parameterName)
    {
        ValidateHandle(submission, parameterName);
        if (submission.Generation != _generation
            || submission.SubmissionIndex < 0
            || submission.SubmissionIndex >= _submissions.Count
            || _submissions[submission.SubmissionIndex].handle != submission)
        {
            throw new ArgumentException("The dependent submission is not pending in the current frame.", parameterName);
        }
    }

    private void EnsureSubmissionCapacity(int additionalSubmissionCount, int additionalDependencyCount)
    {
        if (additionalSubmissionCount == 0 && additionalDependencyCount == 0)
        {
            return;
        }

        var submissionCapacity = checked(_submissions.Count + additionalSubmissionCount);
        var dependencyCapacity = checked(_dependencies.Count + additionalDependencyCount + additionalSubmissionCount);
        for (var i = 0; i < QUEUE_COUNT; i++)
        {
            dependencyCapacity = checked(dependencyCapacity + _pendingDependencies[i].Count);
        }

        _submissions.EnsureCapacity(submissionCapacity);
        _dependencies.EnsureCapacity(dependencyCapacity);
    }

    private void ValidateTransaction(SubmissionTransaction transaction)
    {
        if (!_activeTransaction.IsValid
            || transaction.SchedulerId != _schedulerId
            || transaction.SchedulerGeneration != _generation
            || transaction.TransactionId != _activeTransaction.TransactionId)
        {
            throw new InvalidOperationException("The submission transaction is not active on this frame scheduler.");
        }
    }

    private void CompleteSubmissionTransaction()
    {
        for (var i = 0; i < QUEUE_COUNT; i++)
        {
            _transactionPendingDependencies[i].Clear();
        }

        _activeTransaction = default;
        _transactionSubmissionCount = 0;
        _transactionDependencyCount = 0;
    }

    private void EnsureScratchCapacity(int submissionCount)
    {
        if (_indegrees.Length >= submissionCount)
        {
            return;
        }

        var capacity = Math.Max(submissionCount, Math.Max(8, _indegrees.Length * 2));
        Array.Resize(ref _indegrees, capacity);
        Array.Resize(ref _executionOrder, capacity);
        Array.Resize(ref _scheduled, capacity);
    }

    private void ResetPendingState()
    {
        _submissions.Clear();
        _dependencies.Clear();
        for (var i = 0; i < QUEUE_COUNT; i++)
        {
            _pendingDependencies[i].Clear();
            _currentQueueTailIndices[i] = -1;
        }

        _generation++;
        if (_generation == 0)
        {
            _generation = 1;
        }
    }

    private static CommandQueueType GetQueueType(CommandBufferType type)
    {
        return type switch
        {
            CommandBufferType.Graphics => CommandQueueType.Graphics,
            CommandBufferType.Compute => CommandQueueType.Compute,
            CommandBufferType.Copy => CommandQueueType.Copy,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    private static int GetQueueIndex(CommandQueueType type)
    {
        return type switch
        {
            CommandQueueType.Graphics => 0,
            CommandQueueType.Compute => 1,
            CommandQueueType.Copy => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            if (_activeTransaction.IsValid)
            {
                RollbackSubmissionTransaction(_activeTransaction);
            }

            WaitIdle();
        }
        finally
        {
            for (var i = 0; i < QUEUE_COUNT; i++)
            {
                _fences[i].Dispose();
            }

            _disposed = true;
        }
    }
}
