using Ghost.Core;
using Ghost.Graphics.D3D12.Utilities;
using Ghost.Graphics.Data;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Utilities;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using static TerraFX.Aliases.D3D12_Alias;
using static TerraFX.Aliases.DXGI_Alias;
using static TerraFX.Aliases.D3D_Alias;

namespace Ghost.Graphics.D3D12;

/// <summary>
/// D3D12 implementation of command buffer interface
/// </summary>
internal unsafe class D3D12CommandBuffer : ICommandBuffer
{
    private ComPtr<ID3D12CommandAllocator> _allocator;
    private ComPtr<ID3D12GraphicsCommandList10> _commandList;

    private readonly D3D12PipelineLibrary _pipelineLibrary;
    private readonly D3D12ResourceDatabase _resourceDatabase;
    private readonly D3D12ResourceAllocator _resourceAllocator;
    private readonly D3D12DescriptorAllocator _descriptorAllocator;

    private readonly CommandBufferType _type;
    private ushort _commandCount;
    private bool _isRecording;
    private bool _disposed;

    public CommandBufferType Type => _type;

    public ID3D12GraphicsCommandList10* NativeCommandList => _commandList.Get();

    public bool IsEmpty => _commandCount == 0;

    public D3D12CommandBuffer(
        D3D12RenderDevice device,
        D3D12PipelineLibrary stateController,
        D3D12ResourceDatabase resourceDatabase,
        D3D12ResourceAllocator resourceAllocator,
        D3D12DescriptorAllocator descriptorAllocator,
        CommandBufferType type)
    {
        _type = type;
        var commandListType = ConvertCommandBufferType(type);

        device.NativeDevice->CreateCommandAllocator(commandListType, __uuidof<ID3D12CommandAllocator>(), _allocator.GetVoidAddressOf());
        device.NativeDevice->CreateCommandList1(0u, commandListType, D3D12_COMMAND_LIST_FLAG_NONE, __uuidof<ID3D12GraphicsCommandList10>(), _commandList.GetVoidAddressOf());

        _pipelineLibrary = stateController;
        _resourceDatabase = resourceDatabase;
        _resourceAllocator = resourceAllocator;
        _descriptorAllocator = descriptorAllocator;

        // Command lists are created in recording state, so close it
        _commandList.Get()->Close();
        _isRecording = false;
    }

    ~D3D12CommandBuffer()
    {
        Dispose();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private void ThrowIfNotRecording()
    {
        if (!_isRecording)
        {
            throw new InvalidOperationException("Command buffer is not recording");
        }
    }

    private void IncrementCommandCount()
    {
        _commandCount++;
    }

    public void Begin()
    {
        ThrowIfDisposed();

        if (_isRecording)
        {
            throw new InvalidOperationException("Command buffer is already recording");
        }

        _allocator.Get()->Reset();
        _commandList.Get()->Reset(_allocator.Get(), null);
        _commandCount = 0;
        _isRecording = true;
    }

    public void End()
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();

        _commandList.Get()->Close();
        _isRecording = false;
    }

    public void SetRenderTargets(ReadOnlySpan<Handle<Texture>> renderTargets, Handle<Texture> depthTarget)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
        IncrementCommandCount();

        var rtvHandles = stackalloc D3D12_CPU_DESCRIPTOR_HANDLE[renderTargets.Length];
        for (var i = 0; i < renderTargets.Length; i++)
        {
            var handle = renderTargets[i];
            if (!handle.IsValid)
            {
                throw new ArgumentException($"Render target at index {i} is not a valid texture handle");
            }

            var descriptor = _resourceDatabase.GetResourceInfo(handle.AsResource()).descriptor;
            rtvHandles[i] = _descriptorAllocator.GetCpuHandle(descriptor.rtv);
        }

        var dsvHandle = stackalloc D3D12_CPU_DESCRIPTOR_HANDLE[depthTarget.IsValid ? 1 : 0];
        if (dsvHandle != null)
        {
            *dsvHandle = _descriptorAllocator.GetCpuHandle(_resourceDatabase.GetResourceInfo(depthTarget.AsResource()).descriptor.dsv);
        }

        _commandList.Get()->OMSetRenderTargets((uint)renderTargets.Length, rtvHandles, FALSE, dsvHandle);
    }

    public void BeginRenderPass(Handle<Texture> renderTarget, Handle<Texture> depthTarget, Color128 clearColor)
    {
        // TODO: Implement render pass begin
    }

    public void EndRenderPass()
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
        IncrementCommandCount();

        _commandList.Get()->EndRenderPass();
    }

    public void SetViewport(ViewportDesc viewport)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
        IncrementCommandCount();

        var d3d12Viewport = new D3D12_VIEWPORT(viewport.width, viewport.height, viewport.x, viewport.y, viewport.minDepth, viewport.maxDepth);
        _commandList.Get()->RSSetViewports(1, &d3d12Viewport);
    }

    public void SetScissorRect(RectDesc rect)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
        IncrementCommandCount();

        var d3d12Rect = new RECT((int)rect.left, (int)rect.top, (int)rect.right, (int)rect.bottom);
        _commandList.Get()->RSSetScissorRects(1, &d3d12Rect);
    }

    public void ResourceBarrier(Handle<GPUResource> resource, ResourceState before, ResourceState after)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
        IncrementCommandCount();

        var d3d12Resource = _resourceDatabase.GetResource(resource);
        var barrier = D3D12_RESOURCE_BARRIER.InitTransition(d3d12Resource,
            before.ToD3D12States(), after.ToD3D12States());

        _commandList.Get()->ResourceBarrier(1, &barrier);
    }

    public void SetRootSignature(IRootSignature rootSignature)
    {
        // TODO: Implement root signature setting
        throw new NotImplementedException();
    }

    public void SetPipelineState(IShaderPipeline pipelineState)
    {
        // TODO: Implement pipeline state setting
        throw new NotImplementedException();
    }

    public void SetVertexBuffer(uint slot, Handle<GraphicsBuffer> buffer, ulong offset = 0)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
        IncrementCommandCount();

        var pResource = _resourceDatabase.GetResource(buffer.AsResource());
        var vbView = new D3D12_VERTEX_BUFFER_VIEW
        {
            BufferLocation = pResource->GetGPUVirtualAddress() + offset,
            SizeInBytes = (uint)(pResource->GetDesc().Width - offset),
            StrideInBytes = _resourceDatabase.GetResourceDescription(buffer.AsResource()).bufferDescription.Stride
        };

        _commandList.Get()->IASetVertexBuffers(slot, 1, &vbView);
    }

    public void SetIndexBuffer(Handle<GraphicsBuffer> buffer, IndexType type, ulong offset = 0)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
        IncrementCommandCount();

        var pResource = _resourceDatabase.GetResource(buffer.AsResource());
        var ibView = new D3D12_INDEX_BUFFER_VIEW
        {
            BufferLocation = pResource->GetGPUVirtualAddress() + offset,
            SizeInBytes = (uint)(pResource->GetDesc().Width - offset),
            Format = type == IndexType.UInt16 ? DXGI_FORMAT_R16_UINT : DXGI_FORMAT_R32_UINT
        };

        _commandList.Get()->IASetIndexBuffer(&ibView);
    }

    public void Draw(uint vertexCount, uint instanceCount = 1, uint startVertex = 0, uint startInstance = 0)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
        IncrementCommandCount();

        _commandList.Get()->DrawInstanced(vertexCount, instanceCount, startVertex, startInstance);
    }

    public void DrawIndexed(uint indexCount, uint instanceCount = 1, uint startIndex = 0, int baseVertex = 0, uint startInstance = 0)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
        IncrementCommandCount();

        _commandList.Get()->DrawIndexedInstanced(indexCount, instanceCount, startIndex, baseVertex, startInstance);
    }

    // TODO: Batch draw calls by material to minimize state changes
    public void DrawMesh(Handle<Mesh> mesh, Handle<Material> material)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
        IncrementCommandCount();

        ref var meshRef = ref _resourceDatabase.GetMeshReference(mesh);
        ref var materialRef = ref _resourceDatabase.GetMaterialReference(material);
        ref var shaderRef = ref _resourceDatabase.GetShaderReference(materialRef.Shader);

        var shaderPipeline = _pipelineLibrary.GetShaderPipeline(materialRef.Shader);
        if (shaderPipeline is not D3D12ShaderPipeline d3d12Pipeline)
        {
            throw new InvalidOperationException("Shader pipeline is not compiled or invalid");
        }

        _commandList.Get()->SetPipelineState(d3d12Pipeline.pipelineState.Get());
        _commandList.Get()->SetGraphicsRootSignature(_pipelineLibrary.DefaultRootSignature);

        // Set descriptor heaps - CRUCIAL: Use the specialized bindless heap for SM 6.6
        var heaps = stackalloc ID3D12DescriptorHeap*[2];
        heaps[0] = _descriptorAllocator.GetCbvSrvUavHeap(); // Bindless resource heap
        heaps[1] = _descriptorAllocator.GetSamplerHeap();   // Bindless sampler heap
        _commandList.Get()->SetDescriptorHeaps(2, heaps);

        var rootParamIndex = 0u;
        foreach (var cbufferInfo in shaderRef.PerMaterialBufferInfo)
        {
            ref var cache = ref materialRef._materialPropertiesCache[(int)cbufferInfo.RegisterSlot];
            var resource = _resourceDatabase.GetResource(cache.GpuResource.AsResource());
            _commandList.Get()->SetGraphicsRootConstantBufferView(rootParamIndex++, resource->GetGPUVirtualAddress());
        }

        var samplerGpuHandle = _descriptorAllocator.GetSamplerHeap()->GetGPUDescriptorHandleForHeapStart();
        _commandList.Get()->SetGraphicsRootDescriptorTable(rootParamIndex, samplerGpuHandle);

        // For fully bindless rendering, we don't use the Input Assembler stage
        // Instead, we use instanced drawing where each "instance" represents a triangle
        // The shader will use SV_InstanceID to index into the index buffer and then into the vertex buffer
        _commandList.Get()->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST);

        // Draw without vertex/index buffers - use instanced drawing
        // Each instance represents a triangle (3 vertices)
        var triangleCount = (uint)meshRef.indices.Count / 3;
        _commandList.Get()->DrawInstanced(3, triangleCount, 0, 0);
    }

    public void Dispatch(uint threadGroupCountX, uint threadGroupCountY = 1, uint threadGroupCountZ = 1)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
        IncrementCommandCount();

        _commandList.Get()->Dispatch(threadGroupCountX, threadGroupCountY, threadGroupCountZ);
    }

    public void Upload<T>(Handle<GraphicsBuffer> buffer, ReadOnlySpan<T> data)
        where T : unmanaged
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
        IncrementCommandCount();

        var sizeInBytes = (uint)(data.Length * sizeof(T));

        var uploadHandle = _resourceAllocator.CreateUploadBuffer(sizeInBytes);
        var pUploadResource = _resourceDatabase.GetResource(uploadHandle.AsResource());

        void* pMappedData;
        pUploadResource->Map(0, null, &pMappedData);
        fixed (T* pData = data)
        {
            MemoryUtilities.MemCpy(pMappedData, pData, sizeInBytes);
        }
        pUploadResource->Unmap(0, null);

        var pResource = _resourceDatabase.GetResource(buffer.AsResource());

        _commandList.Get()->CopyBufferRegion(pResource, 0, pUploadResource, 0, sizeInBytes);
    }

    public void Upload(Handle<Texture> texture, params ReadOnlySpan<SubResourceData> subresources)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
        IncrementCommandCount();

        var pResource = _resourceDatabase.GetResource(texture.AsResource());

        var resourceDesc = pResource->GetDesc();
        var requiredSize = GetRequiredIntermediateSize(pResource, 0, (uint)subresources.Length);

        var uploadHandle = _resourceAllocator.CreateUploadBuffer(requiredSize);
        var pUploadResource = _resourceDatabase.GetResource(uploadHandle.AsResource());

        var d3d12Subresources = stackalloc D3D12_SUBRESOURCE_DATA[subresources.Length];
        for (var i = 0; i < subresources.Length; i++)
        {
            d3d12Subresources[i] = new D3D12_SUBRESOURCE_DATA
            {
                pData = subresources[i].pData,
                RowPitch = subresources[i].rowPitch,
                SlicePitch = subresources[i].slicePitch
            };
        }

        UpdateSubresources(
            (ID3D12GraphicsCommandList*)_commandList.Get(),
            pResource,
            pUploadResource,
            0,
            0,
            (uint)subresources.Length,
            d3d12Subresources);
    }

    public void CopyBuffer(Handle<GraphicsBuffer> dest, Handle<GraphicsBuffer> src, ulong destOffset = 0, ulong srcOffset = 0, ulong numBytes = 0)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
        IncrementCommandCount();

        var pDestResource = _resourceDatabase.GetResource(dest.AsResource());
        var pSrcResource = _resourceDatabase.GetResource(src.AsResource());
        if (pSrcResource == null || pDestResource == null)
        {
            throw new ArgumentException("Source or destination buffer is not valid");
        }

        if (numBytes == 0)
        {
            _commandList.Get()->CopyResource(pDestResource, pSrcResource);
        }
        else
        {
            _commandList.Get()->CopyBufferRegion(pDestResource, destOffset, pSrcResource, srcOffset, numBytes);
        }
    }

    private static D3D12_COMMAND_LIST_TYPE ConvertCommandBufferType(CommandBufferType type)
    {
        return type switch
        {
            CommandBufferType.Graphics => D3D12_COMMAND_LIST_TYPE_DIRECT,
            CommandBufferType.Compute => D3D12_COMMAND_LIST_TYPE_COMPUTE,
            CommandBufferType.Copy => D3D12_COMMAND_LIST_TYPE_COPY,
            _ => throw new ArgumentException($"Unknown command buffer type: {type}")
        };
    }

    public void Dispose()
    {
        if (_isRecording)
        {
            throw new InvalidOperationException("Command buffer is still recording");
        }

        if (_disposed)
        {
            return;
        }

        _commandList.Dispose();
        _allocator.Dispose();
        _isRecording = false;
        _commandCount = 0;
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
