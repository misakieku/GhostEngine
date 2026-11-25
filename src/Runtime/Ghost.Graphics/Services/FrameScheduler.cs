using Ghost.Graphics.FrameScheduling;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;

namespace Ghost.Graphics.Services;

/// <summary>
/// Coordinates deferred GPU command-buffer submissions, cross-queue fence dependencies, and transaction rollback.
/// </summary>
/// <remarks>
/// <see cref="FrameScheduler"/> is single-threaded and must only be accessed from the render thread.
/// </remarks>
public sealed class FrameScheduler : IDisposable
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

    private struct SubmissionDependency
    {
        public SubmissionHandle producer;
        public int dependentIndex;
        public int nextOut;
        public int nextIn;

        public SubmissionDependency(SubmissionHandle producer, int dependentIndex)
        {
            this.producer = producer;
            this.dependentIndex = dependentIndex;
            nextOut = -1;
            nextIn = -1;
        }
    }

    [InlineArray(QUEUE_COUNT)]
    private struct QueueUlongArray
    {
        private ulong _element0;
    }

    [InlineArray(QUEUE_COUNT)]
    private struct QueueHandleArray
    {
        private SubmissionHandle _element0;
    }

    [InlineArray(QUEUE_COUNT)]
    private struct QueueIntArray
    {
        private int _element0;
    }

    [InlineArray(QUEUE_COUNT)]
    private struct QueuePendingDependencyArray
    {
        private UnsafeList<SubmissionHandle> _element0;
    }

    [InlineArray(QUEUE_COUNT * QUEUE_COUNT)]
    private struct QueueWaitMatrix
    {
        private ulong _element0;
    }

    private struct TransactionSnapshot
    {
        public QueueHandleArray latestSubmissions;
        public QueueIntArray queueTailIndices;
        public int submissionCount;
        public int dependencyCount;
    }

    private const int QUEUE_COUNT = 3;

    private static int s_nextSchedulerId;

    private readonly IGraphicsEngine _graphicsEngine;
    private readonly ICommandQueue[] _queues;
    private readonly IFence[] _fences;

    private QueueUlongArray _nextFenceValues;
    private QueueHandleArray _latestSubmissions;
    private QueueHandleArray _flushedSubmissions;
    private QueueIntArray _currentQueueTailIndices;
    private QueuePendingDependencyArray _pendingDependencies;
    private QueuePendingDependencyArray _transactionPendingDependencies;
    private QueueWaitMatrix _lastWaitedFenceValues;
    private TransactionSnapshot _transactionSnapshot;

    private UnsafeList<SubmissionDependency> _dependencies;
    private readonly HashSet<SubmissionHandle> _invalidatedSubmissions;

    private readonly List<SubmissionRecord> _submissions;
    private readonly HashSet<ICommandBuffer> _unsubmittedCommandBuffers;

    private UnsafeArray<int> _indegrees;
    private UnsafeArray<int> _executionOrder;
    private UnsafeArray<bool> _scheduled;
    private UnsafeArray<int> _outHeads;
    private UnsafeArray<int> _inHeads;
    private UnsafeArray<bool> _crossQueueSignaled;
    private UnsafeArray<int> _readyList;
    private ICommandBuffer[] _batchCommandBuffers;

    private readonly int _schedulerId;
    private uint _nextTransactionId;
    private SubmissionTransaction _activeTransaction;
    private uint _generation;
    private bool _disposed;
#if DEBUG
    private int _debugCmdIndex;
#endif

    public ulong SubmittedFrame
    {
        get;
        private set;
    }

    public FrameScheduler(IGraphicsEngine graphicsEngine)
    {
        ArgumentNullException.ThrowIfNull(graphicsEngine);

        _graphicsEngine = graphicsEngine;
        _queues = new ICommandQueue[QUEUE_COUNT];
        _fences = new IFence[QUEUE_COUNT];

        _dependencies = new UnsafeList<SubmissionDependency>(16, AllocationHandle.Persistent);
        _invalidatedSubmissions = new HashSet<SubmissionHandle>(4);
        _submissions = new List<SubmissionRecord>(16);
        _unsubmittedCommandBuffers = new HashSet<ICommandBuffer>(8);
        _batchCommandBuffers = Array.Empty<ICommandBuffer>();

        _queues[GetQueueIndex(CommandQueueType.Graphics)] = graphicsEngine.Device.GraphicsQueue;
        _queues[GetQueueIndex(CommandQueueType.Compute)] = graphicsEngine.Device.ComputeQueue;
        _queues[GetQueueIndex(CommandQueueType.Copy)] = graphicsEngine.Device.CopyQueue;

        for (var i = 0; i < QUEUE_COUNT; i++)
        {
            _fences[i] = graphicsEngine.CreateFence();
            _fences[i].Name = $"FrameScheduler_{(CommandQueueType)i}_Fence";
            _pendingDependencies[i] = new UnsafeList<SubmissionHandle>(2, AllocationHandle.Persistent);
            _transactionPendingDependencies[i] = new UnsafeList<SubmissionHandle>(2, AllocationHandle.Persistent);
            _currentQueueTailIndices[i] = -1;
        }

        _schedulerId = Interlocked.Increment(ref s_nextSchedulerId);
        _generation = 1;
    }

    public ICommandBuffer GetPooledCommandBuffer(CommandBufferType type = CommandBufferType.Graphics)
    {
        ThrowIfDisposed();
        var cmd = _graphicsEngine.GetPooledCommandBuffer(type);
#if DEBUG
        cmd.Name = $"FrameScheduler_{type}_Cmd_{_debugCmdIndex++}";
#endif
        _unsubmittedCommandBuffers.Add(cmd);
        return cmd;
    }

    public void ReturnPooledCommandBuffer(ICommandBuffer commandBuffer)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(commandBuffer);
        _unsubmittedCommandBuffers.Remove(commandBuffer);
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

        _transactionSnapshot.submissionCount = _submissions.Count;
        _transactionSnapshot.dependencyCount = _dependencies.Count;
        _transactionSnapshot.latestSubmissions = _latestSubmissions;
        _transactionSnapshot.queueTailIndices = _currentQueueTailIndices;

        for (var i = 0; i < QUEUE_COUNT; i++)
        {
            ref var snapshot = ref _transactionPendingDependencies[i];
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

        for (var i = _transactionSnapshot.submissionCount; i < _submissions.Count; i++)
        {
            var submission = _submissions[i];
            _invalidatedSubmissions.Add(submission.handle);
            _graphicsEngine.ReturnPooledCommandBuffer(submission.commandBuffer);
        }

        _submissions.RemoveRange(_transactionSnapshot.submissionCount, _submissions.Count - _transactionSnapshot.submissionCount);
        _dependencies.RemoveRange(_transactionSnapshot.dependencyCount, _dependencies.Count - _transactionSnapshot.dependencyCount);

        _latestSubmissions = _transactionSnapshot.latestSubmissions;
        _currentQueueTailIndices = _transactionSnapshot.queueTailIndices;

        for (var i = 0; i < QUEUE_COUNT; i++)
        {
            ref var pendingDependencies = ref _pendingDependencies[i];
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

        _unsubmittedCommandBuffers.Remove(commandBuffer);

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

        ref var pendingDependencies = ref _pendingDependencies[queueIndex];
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

        ref var pendingDependencies = ref _pendingDependencies[GetQueueIndex(destination)];
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

    public void FlushPending()
    {
        ThrowIfDisposed();
        if (_activeTransaction.IsValid)
        {
            throw new InvalidOperationException("An active submission transaction must be committed or rolled back before flushing.");
        }

        if (_submissions.Count == 0)
        {
            return;
        }

        try
        {
            FlushPendingCore();
        }
        catch
        {
            RecoverFailedPendingBatch();
            throw;
        }
    }

    public FrameCompletionInfo Flush()
    {
        ThrowIfDisposed();
        if (_activeTransaction.IsValid)
        {
            throw new InvalidOperationException("An active submission transaction must be committed or rolled back before flushing.");
        }

#if DEBUG
        _debugCmdIndex = 0;
#endif

        try
        {
            ValidateNoUnresolvedTransitions();

            if (_submissions.Count > 0)
            {
                FlushPendingCore();
            }

            SubmittedFrame++;

            var completion = new FrameCompletionInfo(
                SubmittedFrame,
                _flushedSubmissions[GetQueueIndex(CommandQueueType.Graphics)],
                _flushedSubmissions[GetQueueIndex(CommandQueueType.Compute)],
                _flushedSubmissions[GetQueueIndex(CommandQueueType.Copy)]);

            ResetFrameState();
            return completion;
        }
        catch
        {
            RecoverFailedPendingBatch();
            ResetFrameState();
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
            FlushPending();
        }

        for (var i = 0; i < QUEUE_COUNT; i++)
        {
            var fenceValue = ++_nextFenceValues[i];
            _queues[i].Signal(_fences[i], fenceValue);
            _fences[i].WaitForValue(fenceValue);
        }
    }

    private void FlushPendingCore()
    {
        var submissionCount = _submissions.Count;
        EnsureScratchCapacity(submissionCount);
        _scheduled.Clear();

        ExecutePendingSubmissions();

        _latestSubmissions = _flushedSubmissions;
        ClearPendingBatchState();
    }

    private void ClearPendingBatchState()
    {
        // Clear only the current batch; do NOT increment SubmittedFrame and do NOT advance _generation.
        _submissions.Clear();
        _dependencies.Clear();
        for (var i = 0; i < QUEUE_COUNT; i++)
        {
            _currentQueueTailIndices[i] = -1;
        }
    }

    private void RecoverFailedPendingBatch()
    {
        ReturnAndInvalidateUnscheduledSubmissions();
        _latestSubmissions = _flushedSubmissions;
        ClearPendingBatchState();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsPendingInCurrentBatch(SubmissionHandle handle)
    {
        var idx = handle.SubmissionIndex;
        return handle.Generation == _generation
            && (uint)idx < (uint)_submissions.Count
            && _submissions[idx].handle == handle;
    }

    private void ExecutePendingSubmissions()
    {
        var submissionCount = _submissions.Count;
        if (submissionCount == 0)
        {
            return;
        }

        _indegrees.AsSpan().Slice(0, submissionCount).Clear();
        _outHeads.AsSpan().Slice(0, submissionCount).Fill(-1);
        _inHeads.AsSpan().Slice(0, submissionCount).Fill(-1);
        _crossQueueSignaled.AsSpan().Slice(0, submissionCount).Clear();

        for (var i = 0; i < _dependencies.Count; i++)
        {
            ref var dependency = ref _dependencies[i];
            var dependentIndex = dependency.dependentIndex;
            dependency.nextIn = _inHeads[dependentIndex];
            _inHeads[dependentIndex] = i;

            if (IsPendingInCurrentBatch(dependency.producer))
            {
                var producerIndex = dependency.producer.SubmissionIndex;
                _indegrees[dependentIndex]++;
                dependency.nextOut = _outHeads[producerIndex];
                _outHeads[producerIndex] = i;

                if (dependency.producer.QueueType != _submissions[dependentIndex].handle.QueueType)
                {
                    _crossQueueSignaled[producerIndex] = true;
                }
            }
            else
            {
                dependency.nextOut = -1;
            }
        }

        var readyCount = 0;
        for (var i = 0; i < submissionCount; i++)
        {
            if (_indegrees[i] == 0)
            {
                _readyList[readyCount++] = i;
            }
        }

        for (var orderIndex = 0; orderIndex < submissionCount; orderIndex++)
        {
            if (readyCount == 0)
            {
                _scheduled.Clear();
                throw new InvalidOperationException("The frame submission graph contains a dependency cycle.");
            }

            // Pick lowest submission index to preserve deterministic submission order on ties.
            var bestPos = 0;
            var bestSubmission = _readyList[0];
            for (var pos = 1; pos < readyCount; pos++)
            {
                var candidate = _readyList[pos];
                if (candidate < bestSubmission)
                {
                    bestSubmission = candidate;
                    bestPos = pos;
                }
            }

            _readyList[bestPos] = _readyList[--readyCount];
            _executionOrder[orderIndex] = bestSubmission;

            var edgeIndex = _outHeads[bestSubmission];
            while (edgeIndex >= 0)
            {
                ref readonly var dependency = ref _dependencies[edgeIndex];
                var dependentIndex = dependency.dependentIndex;
                _indegrees[dependentIndex]--;
                if (_indegrees[dependentIndex] == 0)
                {
                    _readyList[readyCount++] = dependentIndex;
                }

                edgeIndex = dependency.nextOut;
            }
        }

        _scheduled.Clear();
        Span<ulong> waitValues = stackalloc ulong[QUEUE_COUNT];
        Span<ulong> nextWaitValues = stackalloc ulong[QUEUE_COUNT];

        var execIndex = 0;
        while (execIndex < submissionCount)
        {
            var batchStartOrder = execIndex;
            var firstIndex = _executionOrder[execIndex];
            var firstRecord = _submissions[firstIndex];
            var destinationQueueType = firstRecord.handle.QueueType;
            var destinationQueueIndex = GetQueueIndex(destinationQueueType);

            if (CollectRequiredWaits(firstIndex, destinationQueueType, destinationQueueIndex, waitValues))
            {
                for (var q = 0; q < QUEUE_COUNT; q++)
                {
                    var requiredFence = waitValues[q];
                    if (requiredFence > GetLastWaitedFenceValue(destinationQueueIndex, q))
                    {
                        _queues[destinationQueueIndex].Wait(_fences[q], requiredFence);
                        SetLastWaitedFenceValue(destinationQueueIndex, q, requiredFence);
                    }
                }
            }

            _batchCommandBuffers[0] = firstRecord.commandBuffer;
            var batchCount = 1;
            var lastIndex = firstIndex;
            execIndex++;

            while (execIndex < submissionCount && !_crossQueueSignaled[lastIndex])
            {
                var nextIndex = _executionOrder[execIndex];
                var nextRecord = _submissions[nextIndex];
                if (nextRecord.handle.QueueType != destinationQueueType)
                {
                    break;
                }

                if (CollectRequiredWaits(nextIndex, destinationQueueType, destinationQueueIndex, nextWaitValues))
                {
                    break;
                }

                _batchCommandBuffers[batchCount++] = nextRecord.commandBuffer;
                lastIndex = nextIndex;
                execIndex++;
            }

            if (batchCount == 1)
            {
                _queues[destinationQueueIndex].Submit(_batchCommandBuffers[0]);
            }
            else
            {
                _queues[destinationQueueIndex].Submit(new ReadOnlySpan<ICommandBuffer>(_batchCommandBuffers, 0, batchCount));
            }

            for (var k = 0; k < batchCount; k++)
            {
                _scheduled[_executionOrder[batchStartOrder + k]] = true;
            }

            var lastHandle = _submissions[lastIndex].handle;
            _queues[destinationQueueIndex].Signal(_fences[destinationQueueIndex], lastHandle.FenceValue);
            _flushedSubmissions[destinationQueueIndex] = lastHandle;

            for (var k = 0; k < batchCount; k++)
            {
                var cb = _batchCommandBuffers[k];
                _batchCommandBuffers[k] = null!;
                _graphicsEngine.ReturnPooledCommandBuffer(cb);
            }
        }
    }

    private bool CollectRequiredWaits(int submissionIndex, CommandQueueType destinationQueueType, int destinationQueueIndex, Span<ulong> waitValues)
    {
        waitValues.Clear();
        var hasNewWait = false;
        var edgeIndex = _inHeads[submissionIndex];

        while (edgeIndex >= 0)
        {
            ref readonly var dependency = ref _dependencies[edgeIndex];
            if (dependency.producer.QueueType != destinationQueueType)
            {
                var sourceQueueIndex = GetQueueIndex(dependency.producer.QueueType);
                var fenceValue = Math.Max(waitValues[sourceQueueIndex], dependency.producer.FenceValue);
                waitValues[sourceQueueIndex] = fenceValue;
                if (fenceValue > GetLastWaitedFenceValue(destinationQueueIndex, sourceQueueIndex))
                {
                    hasNewWait = true;
                }
            }

            edgeIndex = dependency.nextIn;
        }

        return hasNewWait;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ulong GetLastWaitedFenceValue(int destinationQueueIndex, int sourceQueueIndex)
    {
        return _lastWaitedFenceValues[destinationQueueIndex * QUEUE_COUNT + sourceQueueIndex];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetLastWaitedFenceValue(int destinationQueueIndex, int sourceQueueIndex, ulong value)
    {
        _lastWaitedFenceValues[destinationQueueIndex * QUEUE_COUNT + sourceQueueIndex] = value;
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

    private void ReturnAndInvalidateUnscheduledSubmissions()
    {
        for (var i = 0; i < _submissions.Count; i++)
        {
            if (_scheduled.IsCreated && i < _scheduled.Length && _scheduled[i])
            {
                continue;
            }

            var submission = _submissions[i];
            _invalidatedSubmissions.Add(submission.handle);
            _graphicsEngine.ReturnPooledCommandBuffer(submission.commandBuffer);
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

        if (_invalidatedSubmissions.Count > 0 && _invalidatedSubmissions.Contains(submission))
        {
            throw new ArgumentException("The submission handle was invalidated by transaction rollback.", parameterName);
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

        if (_dependencies.Capacity < dependencyCapacity)
        {
            _dependencies.Resize(dependencyCapacity);
        }
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
        _transactionSnapshot = default;
    }

    private void EnsureScratchCapacity(int submissionCount)
    {
        if (_indegrees.IsCreated && _indegrees.Length >= submissionCount)
        {
            return;
        }

        var currentLength = _indegrees.IsCreated ? _indegrees.Length : 0;
        var capacity = Math.Max(submissionCount, Math.Max(8, currentLength * 2));

        EnsureArrayCapacity(ref _indegrees, capacity);
        EnsureArrayCapacity(ref _executionOrder, capacity);
        EnsureArrayCapacity(ref _scheduled, capacity);
        EnsureArrayCapacity(ref _outHeads, capacity);
        EnsureArrayCapacity(ref _inHeads, capacity);
        EnsureArrayCapacity(ref _crossQueueSignaled, capacity);
        EnsureArrayCapacity(ref _readyList, capacity);

        if (_batchCommandBuffers.Length < capacity)
        {
            Array.Resize(ref _batchCommandBuffers, capacity);
        }
    }

    private static void EnsureArrayCapacity<T>(ref UnsafeArray<T> array, int capacity)
        where T : unmanaged
    {
        if (!array.IsCreated)
        {
            array = new UnsafeArray<T>(capacity, AllocationHandle.Persistent, AllocationOption.Clear);
        }
        else if (array.Length < capacity)
        {
            array.Resize(capacity);
        }
    }

    private void ResetFrameState()
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
            foreach (var cmd in _unsubmittedCommandBuffers)
            {
                _graphicsEngine.ReturnPooledCommandBuffer(cmd);
            }
            _unsubmittedCommandBuffers.Clear();

            for (var i = 0; i < QUEUE_COUNT; i++)
            {
                _fences[i].Dispose();
                _pendingDependencies[i].Dispose();
                _transactionPendingDependencies[i].Dispose();
            }

            _dependencies.Dispose();
            _indegrees.Dispose();
            _executionOrder.Dispose();
            _scheduled.Dispose();
            _outHeads.Dispose();
            _inHeads.Dispose();
            _crossQueueSignaled.Dispose();
            _readyList.Dispose();

            _disposed = true;
        }
    }
}
