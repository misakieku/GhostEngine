using Ghost.Graphics.Data;
using Ghost.Graphics.RHI;
using System.Runtime.InteropServices;
using Win32;
using Win32.Graphics.Direct3D12;
using Win32.Numerics;

namespace Ghost.Graphics.D3D12;

/// <summary>
/// D3D12 implementation of command buffer interface
/// </summary>
internal unsafe class D3D12CommandBuffer : ICommandBuffer
{
    private ComPtr<ID3D12CommandAllocator> _allocator;
    private ComPtr<ID3D12GraphicsCommandList10> _commandList;
    private readonly CommandBufferType _type;
    private bool _isRecording;
    private bool _disposed;

    public ID3D12GraphicsCommandList10* NativeCommandList => _commandList.Get();

    public D3D12CommandBuffer(ComPtr<ID3D12Device14> device, CommandBufferType type)
    {
        _type = type;
        var commandListType = ConvertCommandBufferType(type);

        device.Get()->CreateCommandAllocator(commandListType, __uuidof<ID3D12CommandAllocator>(), _allocator.GetVoidAddressOf());
        device.Get()->CreateCommandList(0u, commandListType, _allocator.Get(), null, __uuidof<ID3D12GraphicsCommandList10>(), _commandList.GetVoidAddressOf());

        // Command lists are created in recording state, so close it
        _commandList.Get()->Close();
        _isRecording = false;
    }

    public void Begin()
    {
        if (_isRecording)
            throw new InvalidOperationException("Command buffer is already recording");

        _allocator.Get()->Reset();
        _commandList.Get()->Reset(_allocator.Get(), null);
        _isRecording = true;
    }

    public void End()
    {
        if (!_isRecording)
            throw new InvalidOperationException("Command buffer is not recording");

        _commandList.Get()->Close();
        _isRecording = false;
    }

    public void BeginRenderPass(IRenderTarget renderTarget, Color128 clearColor)
    {
        // TODO: Implement render pass begin
        throw new NotImplementedException();
    }

    public void EndRenderPass()
    {
        // TODO: Implement render pass end
        throw new NotImplementedException();
    }

    public void SetViewport(ViewportDesc viewport)
    {
        var d3d12Viewport = new Viewport(viewport.Width, viewport.Height, viewport.X, viewport.Y, viewport.MinDepth, viewport.MaxDepth);
        _commandList.Get()->RSSetViewports(1, &d3d12Viewport);
    }

    public void SetScissorRect(RectDesc rect)
    {
        var d3d12Rect = new Rect(rect.Left, rect.Top, rect.Right, rect.Bottom);
        _commandList.Get()->RSSetScissorRects(1, &d3d12Rect);
    }

    public void ResourceBarrier(IResource resource, ResourceState before, ResourceState after)
    {
        if (resource is D3D12Texture d3d12Texture)
        {
            _commandList.Get()->ResourceBarrierTransition(d3d12Texture.NativeResource,
                ConvertResourceState(before), ConvertResourceState(after));
        }
        else if (resource is D3D12Buffer d3d12Buffer)
        {
            _commandList.Get()->ResourceBarrierTransition(d3d12Buffer.NativeResource,
                ConvertResourceState(before), ConvertResourceState(after));
        }
        else
        {
            throw new ArgumentException("Resource must be a D3D12 resource", nameof(resource));
        }
    }

    public void SetGraphicsRootSignature(IRootSignature rootSignature)
    {
        // TODO: Implement root signature setting
        throw new NotImplementedException();
    }

    public void SetPipelineState(IPipelineState pipelineState)
    {
        // TODO: Implement pipeline state setting
        throw new NotImplementedException();
    }

    public void SetDescriptorHeaps(IDescriptorHeap[] heaps)
    {
        // TODO: Implement descriptor heap setting
        throw new NotImplementedException();
    }

    public void DrawIndexedInstanced(uint indexCount, uint instanceCount = 1, uint startIndex = 0, int baseVertex = 0, uint startInstance = 0)
    {
        _commandList.Get()->DrawIndexedInstanced(indexCount, instanceCount, startIndex, baseVertex, startInstance);
    }

    public void Dispatch(uint threadGroupCountX, uint threadGroupCountY = 1, uint threadGroupCountZ = 1)
    {
        _commandList.Get()->Dispatch(threadGroupCountX, threadGroupCountY, threadGroupCountZ);
    }

    private static CommandListType ConvertCommandBufferType(CommandBufferType type)
    {
        return type switch
        {
            CommandBufferType.Graphics => CommandListType.Direct,
            CommandBufferType.Compute => CommandListType.Compute,
            CommandBufferType.Copy => CommandListType.Copy,
            _ => throw new ArgumentException($"Unknown command buffer type: {type}")
        };
    }

    private static Win32.Graphics.Direct3D12.ResourceStates ConvertResourceState(ResourceState state)
    {
        return state switch
        {
            ResourceState.Common or ResourceState.Present => Win32.Graphics.Direct3D12.ResourceStates.Common,
            ResourceState.VertexAndConstantBuffer => Win32.Graphics.Direct3D12.ResourceStates.VertexAndConstantBuffer,
            ResourceState.IndexBuffer => Win32.Graphics.Direct3D12.ResourceStates.IndexBuffer,
            ResourceState.RenderTarget => Win32.Graphics.Direct3D12.ResourceStates.RenderTarget,
            ResourceState.UnorderedAccess => Win32.Graphics.Direct3D12.ResourceStates.UnorderedAccess,
            ResourceState.DepthWrite => Win32.Graphics.Direct3D12.ResourceStates.DepthWrite,
            ResourceState.DepthRead => Win32.Graphics.Direct3D12.ResourceStates.DepthRead,
            ResourceState.PixelShaderResource => Win32.Graphics.Direct3D12.ResourceStates.PixelShaderResource,
            ResourceState.CopyDest => Win32.Graphics.Direct3D12.ResourceStates.CopyDest,
            ResourceState.CopySource => Win32.Graphics.Direct3D12.ResourceStates.CopySource,
            _ => throw new ArgumentException($"Unknown resource state: {state}")
        };
    }

    public void Dispose()
    {
        if (_disposed) return;

        _commandList.Dispose();
        _allocator.Dispose();
        _disposed = true;
    }
}
