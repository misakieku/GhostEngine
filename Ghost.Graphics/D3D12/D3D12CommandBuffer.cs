using Ghost.Core;
using Ghost.Graphics.D3D12.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Runtime.CompilerServices;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using static TerraFX.Aliases.D3D12_Alias;
using static TerraFX.Aliases.D3D_Alias;
using static TerraFX.Aliases.DXGI_Alias;
using Ghost.Core.Graphics;
using Ghost.Graphics.Core;

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

    private string _name;
    private readonly CommandBufferType _type;
    private ushort _commandCount;
    private bool _isRecording;
    private bool _disposed;

    public ID3D12GraphicsCommandList10* NativeCommandList => _commandList.Get();
    public CommandBufferType Type => _type;
    public bool IsEmpty => _commandCount == 0;

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            _commandList.Get()->SetName(value);
        }
    }

    public D3D12CommandBuffer(
        D3D12RenderDevice device,
        D3D12PipelineLibrary stateController,
        D3D12ResourceDatabase resourceDatabase,
        D3D12ResourceAllocator resourceAllocator,
        D3D12DescriptorAllocator descriptorAllocator,
        CommandBufferType type)
    {
        _name = string.Empty;
        _type = type;

        ID3D12CommandAllocator* pAllocator = default;
        ID3D12GraphicsCommandList10* pCommandList = default;
        var commandListType = ConvertCommandBufferType(type);

        device.NativeDevice->CreateCommandAllocator(commandListType, __uuidof(pAllocator), (void**)&pAllocator);
        device.NativeDevice->CreateCommandList1(0u, commandListType, D3D12_COMMAND_LIST_FLAG_NONE, __uuidof(pCommandList), (void**)&pCommandList);

        _allocator.Attach(pAllocator);
        _commandList.Attach(pCommandList);

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfNotRecording()
    {
        if (!_isRecording)
        {
            throw new InvalidOperationException("Command buffer is not recording");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void IncrementCommandCount()
    {
        _commandCount++;
    }

    public void Begin()
    {
        void ResetCommandList()
        {
            ThrowIfFailed(_allocator.Get()->Reset());
            ThrowIfFailed(_commandList.Get()->Reset(_allocator.Get(), null));
        }

        void SetBindlessHeap()
        {
            var heaps = stackalloc ID3D12DescriptorHeap*[2];
            heaps[0] = _descriptorAllocator.GetCbvSrvUavHeap(); // Bindless resource heap
            heaps[1] = _descriptorAllocator.GetSamplerHeap();   // Bindless sampler heap
            _commandList.Get()->SetDescriptorHeaps(2, heaps);
        }

        ThrowIfDisposed();

        if (_isRecording)
        {
            throw new InvalidOperationException("Command buffer is already recording");
        }

        ResetCommandList();
        SetBindlessHeap();

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

    public void SetRenderTargets(ReadOnlySpan<Handle<Texture>> renderTargets, Handle<Texture> depthTarget)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
        IncrementCommandCount();

        var pRtvHandles = stackalloc D3D12_CPU_DESCRIPTOR_HANDLE[renderTargets.Length];
        for (var i = 0; i < renderTargets.Length; i++)
        {
            var handle = renderTargets[i];
            if (!handle.IsValid)
            {
                throw new ArgumentException($"Render target at index {i} is not a valid texture handle");
            }

            var descriptor = _resourceDatabase.GetResourceInfo(handle.AsResource()).viewGroup;
            pRtvHandles[i] = _descriptorAllocator.GetCpuHandle(descriptor.rtv);
        }

        var pDsvHandle = stackalloc D3D12_CPU_DESCRIPTOR_HANDLE[depthTarget.IsValid ? 1 : 0];
        if (pDsvHandle != null)
        {
            pDsvHandle[0] = _descriptorAllocator.GetCpuHandle(_resourceDatabase.GetResourceInfo(depthTarget.AsResource()).viewGroup.dsv);
        }

        _commandList.Get()->OMSetRenderTargets((uint)renderTargets.Length, pRtvHandles, FALSE, pDsvHandle);
    }

    public void BeginRenderPass(ReadOnlySpan<PassRenderTargetDesc> rtDescs, PassDepthStencilDesc depthDesc, bool allowUAVWrites = false)
    {
        // TODO: Implement render pass begin

        var pRtvDescs = stackalloc D3D12_RENDER_PASS_RENDER_TARGET_DESC[rtDescs.Length];
        for (var i = 0; i < rtDescs.Length; i++)
        {
            var rtDesc = rtDescs[i];
            if (!rtDesc.texture.IsValid)
            {
                throw new ArgumentException($"Render target at index {i} is not a valid texture handle");
            }

            var resourceInfo = _resourceDatabase.GetResourceInfo(rtDesc.texture.AsResource());
            var cpuHandle = _descriptorAllocator.GetCpuHandle(resourceInfo.viewGroup.rtv);

            var desc = new D3D12_RENDER_PASS_RENDER_TARGET_DESC
            {
                cpuDescriptor = cpuHandle,
                BeginningAccess = new D3D12_RENDER_PASS_BEGINNING_ACCESS
                {
                    Type = D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_CLEAR,
                    Clear = new D3D12_RENDER_PASS_BEGINNING_ACCESS_CLEAR_PARAMETERS
                    {
                        ClearValue = new D3D12_CLEAR_VALUE
                        {
                            Format = resourceInfo.desc.textureDescription.Format.ToDXGIFormat(),
                        }
                    }
                }
            };

            desc.BeginningAccess.Clear.ClearValue.Color[0] = rtDesc.clearColor.r;
            desc.BeginningAccess.Clear.ClearValue.Color[1] = rtDesc.clearColor.g;
            desc.BeginningAccess.Clear.ClearValue.Color[2] = rtDesc.clearColor.b;
            desc.BeginningAccess.Clear.ClearValue.Color[3] = rtDesc.clearColor.a;

            pRtvDescs[i] = desc;
        }

        var pDsvDesc = stackalloc D3D12_RENDER_PASS_DEPTH_STENCIL_DESC[depthDesc.texture.IsValid ? 1 : 0];
        if (pDsvDesc != null)
        {
            var resourceInfo = _resourceDatabase.GetResourceInfo(depthDesc.texture.AsResource());
            var cpuHandle = _descriptorAllocator.GetCpuHandle(resourceInfo.viewGroup.dsv);

            var desc = new D3D12_RENDER_PASS_DEPTH_STENCIL_DESC
            {
                cpuDescriptor = cpuHandle,
                DepthBeginningAccess = new D3D12_RENDER_PASS_BEGINNING_ACCESS
                {
                    Type = D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_CLEAR,
                    Clear = new D3D12_RENDER_PASS_BEGINNING_ACCESS_CLEAR_PARAMETERS
                    {
                        ClearValue = new D3D12_CLEAR_VALUE
                        {
                            Format = resourceInfo.desc.textureDescription.Format.ToDXGIFormat(),
                            DepthStencil = new D3D12_DEPTH_STENCIL_VALUE
                            {
                                Depth = depthDesc.clearDepth,
                                Stencil = depthDesc.clearStencil
                            }
                        }
                    }
                }
            };

            pDsvDesc[0] = desc;
        }

        _commandList.Get()->BeginRenderPass((uint)rtDescs.Length, pRtvDescs, pDsvDesc,
                allowUAVWrites ? D3D12_RENDER_PASS_FLAG_ALLOW_UAV_WRITES : D3D12_RENDER_PASS_FLAG_NONE);
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

    public void SetPipelineState(GraphicsPipelineKey pipelineKey)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
        IncrementCommandCount();

        var shaderPipeline = _pipelineLibrary.LoadGraphicsPSO(pipelineKey).GetValueOrThrow();
        _commandList.Get()->SetPipelineState(shaderPipeline.value);
    }

    public void SetConstantBufferView(uint slot, Handle<GraphicsBuffer> buffer)
    {
        var resource = _resourceDatabase.GetResource(buffer.AsResource());
        _commandList.Get()->SetGraphicsRootConstantBufferView(RootSignatureLayout.PER_MATERIAL_BUFFER_SLOT, resource->GetGPUVirtualAddress());
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

    public void SetPrimitiveTopology(PrimitiveTopology topology)
    {
        var d3d12Topology = topology switch
        {
            PrimitiveTopology.Point => D3D_PRIMITIVE_TOPOLOGY_POINTLIST,
            PrimitiveTopology.Line => D3D_PRIMITIVE_TOPOLOGY_LINELIST,
            PrimitiveTopology.Triangle => D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST,
            _ => D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST
        };

        _commandList.Get()->IASetPrimitiveTopology(d3d12Topology);
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

    public void DispatchCompute(uint threadGroupCountX, uint threadGroupCountY, uint threadGroupCountZ)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
        IncrementCommandCount();

        _commandList.Get()->Dispatch(threadGroupCountX, threadGroupCountY, threadGroupCountZ);
    }

    public void DispatchMesh(uint threadGroupCountX, uint threadGroupCountY, uint threadGroupCountZ)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
        IncrementCommandCount();

        _commandList.Get()->DispatchMesh(threadGroupCountX, threadGroupCountY, threadGroupCountZ);
    }

    public void DispatchRay()
    {
        throw new NotImplementedException();

        // ThrowIfDisposed();
        // ThrowIfNotRecording();
        // IncrementCommandCount();

        // _commandList.Get()->DispatchRays();
    }

    public void UploadBuffer<T>(Handle<GraphicsBuffer> buffer, ReadOnlySpan<T> data)
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
            MemoryUtility.MemCpy(pMappedData, pData, sizeInBytes);
        }
        pUploadResource->Unmap(0, null);

        var pResource = _resourceDatabase.GetResource(buffer.AsResource());

        _commandList.Get()->CopyBufferRegion(pResource, 0, pUploadResource, 0, sizeInBytes);
    }

    public void UploadTexture(Handle<Texture> texture, params ReadOnlySpan<SubResourceData> subresources)
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
