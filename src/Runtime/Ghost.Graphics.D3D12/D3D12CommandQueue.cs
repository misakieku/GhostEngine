using Ghost.Graphics.D3D12.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Ghost.Graphics.D3D12;

/// <summary>
/// D3D12 implementation of command queue interface
/// </summary>
internal unsafe class D3D12CommandQueue : D3D12Object<ID3D12CommandQueue>, ICommandQueue
{
    private UniquePtr<ID3D12Fence> _fence;

    private readonly AutoResetEvent _fenceEvent;
    private ulong _fenceValue;

    public CommandQueueType Type
    {
        get;
    }

    private static ID3D12CommandQueue* CreateCommandQueue(ID3D12Device14* device, CommandQueueType type)
    {
        var queueDesc = new D3D12_COMMAND_QUEUE_DESC
        {
            Type = ConvertCommandQueueType(type),
            Priority = (int)D3D12_COMMAND_QUEUE_PRIORITY.D3D12_COMMAND_QUEUE_PRIORITY_NORMAL,
            Flags = D3D12_COMMAND_QUEUE_FLAGS.D3D12_COMMAND_QUEUE_FLAG_NONE,
        };

        ID3D12CommandQueue* pQueue = default;
        ThrowIfFailed(device->CreateCommandQueue(&queueDesc, __uuidof(pQueue), (void**)&pQueue));
        return pQueue;
    }

    public D3D12CommandQueue(D3D12RenderDevice device, CommandQueueType type)
        : base(CreateCommandQueue(device.NativeObject, type))
    {
        Type = type;
        _fenceEvent = new AutoResetEvent(false);
        _fenceValue = 0;

        ID3D12Fence* pFence = default;
        ThrowIfFailed(device.NativeObject.Get()->CreateFence(0, D3D12_FENCE_FLAGS.D3D12_FENCE_FLAG_NONE, __uuidof(pFence), (void**)&pFence));

        _fence.Attach(pFence);
    }

    private static D3D12_COMMAND_LIST_TYPE ConvertCommandQueueType(CommandQueueType type)
    {
        return type switch
        {
            CommandQueueType.Graphics => D3D12_COMMAND_LIST_TYPE.D3D12_COMMAND_LIST_TYPE_DIRECT,
            CommandQueueType.Compute => D3D12_COMMAND_LIST_TYPE.D3D12_COMMAND_LIST_TYPE_COMPUTE,
            CommandQueueType.Copy => D3D12_COMMAND_LIST_TYPE.D3D12_COMMAND_LIST_TYPE_COPY,
            _ => throw new ArgumentException($"Unknown command queue type: {type}")
        };
    }

    public void Submit(ICommandBuffer commandBuffer)
    {
        AssertNotDisposed();

        if (commandBuffer.IsEmpty)
        {
            return;
        }

        if (commandBuffer is D3D12CommandBuffer d3d12CommandBuffer)
        {
            var commandList = d3d12CommandBuffer.NativeObject;
            var commandListPtr = (ID3D12CommandList*)commandList.Get();
            pNativeObject->ExecuteCommandLists(1, &commandListPtr);
        }
        else
        {
            throw new ArgumentException("Command buffer must be a D3D12CommandBuffer", nameof(commandBuffer));
        }
    }

    public void Submit(params ReadOnlySpan<ICommandBuffer> commandBuffers)
    {
        AssertNotDisposed();

        Span<int> executableIndices = stackalloc int[commandBuffers.Length];
        executableIndices.Fill(-1);

        var currentIndex = 0;
        for (var i = 0; i < commandBuffers.Length; i++)
        {
            if (!commandBuffers[i].IsEmpty)
            {
                executableIndices[currentIndex] = i;
                currentIndex++;
            }
        }

        var ppCommandLists = stackalloc ID3D12CommandList*[commandBuffers.Length];

        currentIndex = 0;
        while (currentIndex < commandBuffers.Length)
        {
            var cmdIndex = executableIndices[currentIndex];
            if (cmdIndex == -1)
            {
                break;
            }

            if (commandBuffers[cmdIndex] is D3D12CommandBuffer d3d12CommandBuffer)
            {
                ppCommandLists[currentIndex] = (ID3D12CommandList*)d3d12CommandBuffer.NativeObject.Get();
            }
            else
            {
                throw new ArgumentException("Command buffer must be a D3D12CommandBuffer", nameof(commandBuffers));
            }

            currentIndex++;
        }

        pNativeObject->ExecuteCommandLists((uint)currentIndex, ppCommandLists);
    }

    public ulong Signal(ulong value)
    {
        AssertNotDisposed();

        _fenceValue = value;
        ThrowIfFailed(pNativeObject->Signal(_fence.Get(), _fenceValue));
        return _fenceValue;
    }

    public void WaitForValue(ulong value)
    {
        AssertNotDisposed();

        if (_fence.Get()->GetCompletedValue() < value)
        {
            var handle = new HANDLE((void*)_fenceEvent.SafeWaitHandle.DangerousGetHandle());
            if (_fence.Get()->SetEventOnCompletion(value, handle).SUCCEEDED)
            {
                _fenceEvent.WaitOne();
            }
        }
    }

    public ulong GetCompletedValue()
    {
        AssertNotDisposed();
        return _fence.Get()->GetCompletedValue();
    }

    public void WaitIdle()
    {
        AssertNotDisposed();

        var fenceValue = Signal(Interlocked.Increment(ref _fenceValue));
        WaitForValue(fenceValue);
    }

    public Task WaitAsync()
    {
        AssertNotDisposed();

        var fenceValue = Signal(Interlocked.Increment(ref _fenceValue));

        if (_fence.Get()->GetCompletedValue() >= fenceValue)
        {
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource();
        var handle = new HANDLE((void*)_fenceEvent.SafeWaitHandle.DangerousGetHandle());
        
        if (_fence.Get()->SetEventOnCompletion(fenceValue, handle).FAILED)
        {
            throw new InvalidOperationException("Failed to set event on completion.");
        }

        var registeredWait = ThreadPool.RegisterWaitForSingleObject(
            _fenceEvent,
            (state, timedOut) =>
            {
                var capturedTcs = (TaskCompletionSource)state!;
                capturedTcs.SetResult();
                _fenceEvent.Dispose();
            },
            tcs,
            Timeout.Infinite,
            executeOnlyOnce: true
        );

        tcs.Task.ContinueWith(_ => registeredWait.Unregister(null));

        return tcs.Task;
    }

    protected override void Dispose(bool disposing)
    {
        _fence.Dispose();
        _fenceEvent.Dispose();
    }
}
