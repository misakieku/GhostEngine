using Ghost.Graphics.Data;
using Ghost.Graphics.RHI;
using Win32;
using Win32.Graphics.Direct3D;
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

    private readonly D3D12PipelineStateController _stateController;
    private readonly D3D12DescriptorAllocator _descriptorAllocator;

    private readonly CommandBufferType _type;
    private bool _isRecording;
    private bool _disposed;

    public ID3D12GraphicsCommandList10* NativeCommandList => _commandList.Get();

    public D3D12CommandBuffer(D3D12RenderDevice device, D3D12PipelineStateController stateController, D3D12DescriptorAllocator descriptorAllocator, CommandBufferType type)
    {
        _type = type;
        var commandListType = ConvertCommandBufferType(type);

        device.NativeDevice->CreateCommandAllocator(commandListType, __uuidof<ID3D12CommandAllocator>(), _allocator.GetVoidAddressOf());
        device.NativeDevice->CreateCommandList(0u, commandListType, _allocator.Get(), null, __uuidof<ID3D12GraphicsCommandList10>(), _commandList.GetVoidAddressOf());

        _stateController = stateController;
        _descriptorAllocator = descriptorAllocator;

        // Command lists are created in recording state, so close it
        _commandList.Get()->Close();
        _isRecording = false;
    }

    public void Begin()
    {
        if (_isRecording)
        {
            throw new InvalidOperationException("Command buffer is already recording");
        }

        _allocator.Get()->Reset();
        _commandList.Get()->Reset(_allocator.Get(), null);
        _isRecording = true;
    }

    public void End()
    {
        if (!_isRecording)
        {
            throw new InvalidOperationException("Command buffer is not recording");
        }

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

    public void SetPipelineState(IPipelineStateController pipelineState)
    {
        // TODO: Implement pipeline state setting
        throw new NotImplementedException();
    }

    public void DrawMesh(Mesh mesh, Material material)
    {
        // Bind the bindless material (sets up root signature, pipeline state, and descriptor heaps)
        var shaderPipeline = _stateController.GetShaderPipeline(material.Shader);
        if (shaderPipeline is not D3D12ShaderPipeline d3d12Pipeline)
        {
            throw new InvalidOperationException("Shader pipeline is not compiled or invalid");
        }

        // Set root signature and pipeline state
        _commandList.Get()->SetGraphicsRootSignature(d3d12Pipeline.rootSignature.Get());
        _commandList.Get()->SetPipelineState(d3d12Pipeline.pipelineState.Get());

        // Set descriptor heaps - CRUCIAL: Use the specialized bindless heap for SM 6.6
        var heaps = stackalloc ID3D12DescriptorHeap*[2];
        heaps[0] = _descriptorAllocator.GetBindlessHeap(); // Specialized bindless heap
        heaps[1] = d3d12Pipeline.samplerHeap.Get(); // Sampler heap from shader
        _commandList.Get()->SetDescriptorHeaps(2, heaps);

        // Bind constant buffers
        var rootParamIndex = 0u;
        foreach (var cbufferInfo in material.Shader.ConstantBuffers)
        {
            var cache = material.CBufferCaches[(int)cbufferInfo.RegisterSlot];
            _commandList.Get()->SetGraphicsRootConstantBufferView(rootParamIndex++, cache.GpuResource.GPUAddress);
        }

        // Bind sampler descriptor table (last root parameter)
        var samplerGpuHandle = d3d12Pipeline.samplerHeap.Get()->GetGPUDescriptorHandleForHeapStart();
        _commandList.Get()->SetGraphicsRootDescriptorTable(rootParamIndex, samplerGpuHandle);


        // For fully bindless rendering, we don't use the Input Assembler stage
        // Instead, we use instanced drawing where each "instance" represents a triangle
        // The shader will use SV_InstanceID to index into the index buffer and then into the vertex buffer
        _commandList.Get()->IASetPrimitiveTopology(PrimitiveTopology.TriangleList);

        // Draw without vertex/index buffers - use instanced drawing
        // Each instance represents a triangle (3 vertices)
        var triangleCount = mesh.IndexCount / 3;
        _commandList.Get()->DrawInstanced(3, triangleCount, 0, 0);
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

    private static ResourceStates ConvertResourceState(ResourceState state)
    {
        return state switch
        {
            ResourceState.Common or ResourceState.Present => ResourceStates.Common,
            ResourceState.VertexAndConstantBuffer => ResourceStates.VertexAndConstantBuffer,
            ResourceState.IndexBuffer => ResourceStates.IndexBuffer,
            ResourceState.RenderTarget => ResourceStates.RenderTarget,
            ResourceState.UnorderedAccess => ResourceStates.UnorderedAccess,
            ResourceState.DepthWrite => ResourceStates.DepthWrite,
            ResourceState.DepthRead => ResourceStates.DepthRead,
            ResourceState.PixelShaderResource => ResourceStates.PixelShaderResource,
            ResourceState.CopyDest => ResourceStates.CopyDest,
            ResourceState.CopySource => ResourceStates.CopySource,
            _ => throw new ArgumentException($"Unknown resource state: {state}")
        };
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _commandList.Dispose();
        _allocator.Dispose();
        _disposed = true;
    }
}
