using Ghost.Graphics.RHI;
using System.Diagnostics;
using TerraFX.Interop.DirectX;

namespace Ghost.Graphics.D3D12;

/// <summary>
/// D3D12 implementation of command queue interface
/// </summary>
internal unsafe class D3D12CommandQueue : D3D12Object<ID3D12CommandQueue>, ICommandQueue
{
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

    public ulong Signal(IFence fence, ulong value)
    {
        AssertNotDisposed();

        var d3d12Fence = fence as D3D12Fence;
        Debug.Assert(d3d12Fence != null, "Fence must be a D3D12Fence");

        ThrowIfFailed(pNativeObject->Signal(d3d12Fence.NativeObject, value));
        return value;
    }

    public void Wait(IFence fence, ulong value)
    {
        AssertNotDisposed();

        var d3d12Fence = fence as D3D12Fence;
        Debug.Assert(d3d12Fence != null, "Fence must be a D3D12Fence");

        pNativeObject->Wait(d3d12Fence.NativeObject, value);
    }
}
