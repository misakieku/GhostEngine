using Ghost.Core;
using Ghost.Graphics.RHI;
using System.Diagnostics;
using TerraFX.Interop.DirectX;

namespace Ghost.Graphics.D3D12;

/// <summary>
/// D3D12 implementation of command queue interface
/// </summary>
internal unsafe class D3D12CommandQueue : D3D12Object<ID3D12CommandQueue>, ICommandQueue
{
    private readonly ID3D12Device14* _pDevice;

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
        _pDevice = device.NativeObject.Get();
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

        if (commandBuffer.State.CommandCount == 0
            || commandBuffer.State.Error.IsFailure)
        {
            return;
        }

        if (commandBuffer is not D3D12CommandBuffer d3d12CommandBuffer)
        {
            throw new ArgumentException("Command buffer must be a D3D12CommandBuffer", nameof(commandBuffer));
        }

        var commandList = d3d12CommandBuffer.NativeObject;
        var commandListPtr = (ID3D12CommandList*)commandList.Get();
        pNativeObject->ExecuteCommandLists(1, &commandListPtr);
    }

    public void Submit(params ReadOnlySpan<ICommandBuffer> commandBuffers)
    {
        AssertNotDisposed();

        var ppCommandLists = stackalloc ID3D12CommandList*[commandBuffers.Length];

        var currentIndex = 0u;
        for (var i = 0; i < commandBuffers.Length; i++)
        {
            var cmb = commandBuffers[i];
            if (cmb.State.CommandCount == 0
                || cmb.State.Error.IsFailure)
            {
                continue;
            }

            if (cmb is not D3D12CommandBuffer d3d12CommandBuffer)
            {
                throw new ArgumentException("Command buffer must be a D3D12CommandBuffer", nameof(commandBuffers));
            }

            ppCommandLists[currentIndex] = (ID3D12CommandList*)d3d12CommandBuffer.NativeObject.Get();
            currentIndex++;
        }

        pNativeObject->ExecuteCommandLists(currentIndex, ppCommandLists);
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

        ThrowIfFailed(pNativeObject->Wait(d3d12Fence.NativeObject, value));
    }
}
