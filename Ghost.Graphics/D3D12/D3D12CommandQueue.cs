using Ghost.Core.Utilities;
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
    private UniquePtr<ID3D12Fence1> _fence;
    private readonly AutoResetEvent _fenceEvent;
    private ulong _fenceValue;

    public CommandQueueType Type
    {
        get;
    }

    public ID3D12CommandQueue* NativeQueue => nativeObject.Get();

    public D3D12CommandQueue(ID3D12Device14* pDevice, CommandQueueType type)
    {
        Type = type;
        _fenceEvent = new AutoResetEvent(false);
        _fenceValue = 0;

        var queueDesc = new D3D12_COMMAND_QUEUE_DESC
        {
            Type = ConvertCommandQueueType(type),
            Priority = (int)D3D12_COMMAND_QUEUE_PRIORITY.D3D12_COMMAND_QUEUE_PRIORITY_NORMAL,
            Flags = D3D12_COMMAND_QUEUE_FLAGS.D3D12_COMMAND_QUEUE_FLAG_NONE,
        };

        ID3D12CommandQueue* pQueue = default;
        ID3D12Fence1* pFence = default;
        ThrowIfFailed(pDevice->CreateCommandQueue(&queueDesc, __uuidof(pQueue), (void**)&pQueue));
        ThrowIfFailed(pDevice->CreateFence(0, D3D12_FENCE_FLAGS.D3D12_FENCE_FLAG_NONE, __uuidof(pFence), (void**)&pFence));

        nativeObject.Attach(pQueue);
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
        ThrowIfDisposed();

        if (commandBuffer.IsEmpty)
        {
            return;
        }

        if (commandBuffer is D3D12CommandBuffer d3d12CommandBuffer)
        {
            var commandList = d3d12CommandBuffer.NativeCommandList;
            var commandListPtr = (ID3D12CommandList*)commandList;
            nativeObject.Get()->ExecuteCommandLists(1, &commandListPtr);
        }
        else
        {
            throw new ArgumentException("Command buffer must be a D3D12CommandBuffer", nameof(commandBuffer));
        }
    }

    public void Submit(params ReadOnlySpan<ICommandBuffer> commandBuffers)
    {
        ThrowIfDisposed();

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
                ppCommandLists[currentIndex] = (ID3D12CommandList*)d3d12CommandBuffer.NativeCommandList;
            }
            else
            {
                throw new ArgumentException("Command buffer must be a D3D12CommandBuffer", nameof(commandBuffers));
            }

            currentIndex++;
        }

        nativeObject.Get()->ExecuteCommandLists((uint)currentIndex, ppCommandLists);
    }

    public ulong Signal(ulong value)
    {
        ThrowIfDisposed();

        _fenceValue = value;
        nativeObject.Get()->Signal((ID3D12Fence*)_fence.Get(), _fenceValue);
        return _fenceValue;
    }

    public void WaitForValue(ulong value)
    {
        ThrowIfDisposed();

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
        ThrowIfDisposed();
        return _fence.Get()->GetCompletedValue();
    }

    public void WaitIdle()
    {
        ThrowIfDisposed();

        var fenceValue = Signal(Interlocked.Increment(ref _fenceValue));
        WaitForValue(fenceValue);
    }

    protected override void Dispose(bool disposing)
    {
        if (Disposed)
        {
            return;
        }

        _fenceEvent?.Dispose();
        _fence.Dispose();

        base.Dispose(disposing);
    }
}
