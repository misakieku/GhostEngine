using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Graphics.Core;
using Ghost.Graphics.D3D12.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Runtime.CompilerServices;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using static TerraFX.Aliases.D3D_Alias;
using static TerraFX.Aliases.D3D12_Alias;
using static TerraFX.Aliases.DXGI_Alias;

namespace Ghost.Graphics.D3D12;

internal unsafe class D3D12CommandBuffer : ICommandBuffer
{
    private UniquePtr<ID3D12GraphicsCommandList10> _commandList;
    private UniquePtr<ID3D12CommandAllocator> _allocator;

    private readonly D3D12PipelineLibrary _pipelineLibrary;
    private readonly D3D12ResourceDatabase _resourceDatabase;
    private readonly D3D12ResourceAllocator _resourceAllocator;
    private readonly D3D12DescriptorAllocator _descriptorAllocator;
    private readonly CommandBufferType _type;

    private ushort _commandCount;
    private bool _isRecording;
    private bool _disposed;

    public SharedPtr<ID3D12GraphicsCommandList10> NativeCommandList => _commandList.Get();

    public CommandBufferType Type => _type;
    public bool IsEmpty => _commandCount == 0;

    public string Name
    {
        get => field;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            _commandList.Get()->SetName(value);
        }
    } = string.Empty;

    public D3D12CommandBuffer(
        D3D12RenderDevice device,
        D3D12PipelineLibrary stateController,
        D3D12ResourceDatabase resourceDatabase,
        D3D12ResourceAllocator resourceAllocator,
        D3D12DescriptorAllocator descriptorAllocator,
        CommandBufferType type)
    {
        _type = type;

        ID3D12CommandAllocator* pAllocator = default;
        ID3D12GraphicsCommandList10* pCommandList = default;
        var commandListType = ConvertCommandBufferType(type);

        device.NativeDevice.Get()->CreateCommandAllocator(commandListType, __uuidof(pAllocator), (void**)&pAllocator);
        device.NativeDevice.Get()->CreateCommandList1(0u, commandListType, D3D12_COMMAND_LIST_FLAG_NONE, __uuidof(pCommandList), (void**)&pCommandList);

        _allocator.Attach(pAllocator);
        _commandList.Attach(pCommandList);

        _pipelineLibrary = stateController;
        _resourceDatabase = resourceDatabase;
        _resourceAllocator = resourceAllocator;
        _descriptorAllocator = descriptorAllocator;

        _isRecording = false;
    }

    ~D3D12CommandBuffer()
    {
        Dispose();
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfRecording()
    {
        if (_isRecording)
        {
            throw new InvalidOperationException("Command buffer is already recording");
        }
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
        ThrowIfRecording();

        ResetCommandList();

        if (Type == CommandBufferType.Graphics || Type == CommandBufferType.Compute)
        {
            SetBindlessHeap();
        }

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

        var d3d12Rect = new RECT((int)rect.Left, (int)rect.Top, (int)rect.Right, (int)rect.Bottom);
        _commandList.Get()->RSSetScissorRects(1, &d3d12Rect);
    }

    public void ResourceBarrier(ReadOnlySpan<BarrierDesc> barrierDescs)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
        IncrementCommandCount();

        var count = 0u;
        var pBarriers = stackalloc D3D12_RESOURCE_BARRIER[barrierDescs.Length];

        for (int i = 0; i < barrierDescs.Length; i++)
        {
            var desc = barrierDescs[i];
            if (desc.StateBefore == desc.StateAfter)
            {
                continue;
            }

            if (!desc.Resource.IsValid)
            {
                throw new ArgumentException($"Barrier resource at index {i} is not a valid resource handle");
            }

            ref var resourceRecord = ref _resourceDatabase.GetResourceRecord(desc.Resource.AsResource());
            if (resourceRecord.state != desc.StateBefore)
            {
                throw new InvalidOperationException($"Resource state mismatch: expected {desc.StateBefore}, actual {resourceRecord.state}");
            }

            var barrier = D3D12_RESOURCE_BARRIER.InitTransition(resourceRecord.ResourcePtr,
                desc.StateBefore.ToD3D12States(), desc.StateAfter.ToD3D12States());

            pBarriers[count] = barrier;
            count++;

            // Update the resource state in the database
            resourceRecord.state = desc.StateAfter;
        }

        _commandList.Get()->ResourceBarrier(count, pBarriers);
    }

    public void ResourceBarrier(Handle<GPUResource> resource, ResourceState stateBefore, ResourceState stateAfter)
    {
        if (stateBefore == stateAfter)
        {
            return;
        }

        ref var resourceRecord = ref _resourceDatabase.GetResourceRecord(resource);
        if (resourceRecord.state != stateBefore)
        {
            throw new InvalidOperationException($"Resource state mismatch: expected {stateBefore}, actual {resourceRecord.state}");
        }

        var barrier = D3D12_RESOURCE_BARRIER.InitTransition(resourceRecord.ResourcePtr,
            stateBefore.ToD3D12States(), stateAfter.ToD3D12States());

        _commandList.Get()->ResourceBarrier(1, &barrier);
        resourceRecord.state = stateAfter;
    }

    public void ResourceBarrier(Handle<GPUResource> resource, ResourceState stateAfter)
    {
        var stateBefore = _resourceDatabase.GetResourceState(resource);
        ResourceBarrier(resource, stateBefore, stateAfter);
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

            var descriptor = _resourceDatabase.GetResourceRecord(handle.AsResource()).viewGroup;
            pRtvHandles[i] = _descriptorAllocator.GetCpuHandle(descriptor.rtv);
        }

        var pDsvHandle = stackalloc D3D12_CPU_DESCRIPTOR_HANDLE[depthTarget.IsValid ? 1 : 0];
        if (pDsvHandle != null)
        {
            pDsvHandle[0] = _descriptorAllocator.GetCpuHandle(_resourceDatabase.GetResourceRecord(depthTarget.AsResource()).viewGroup.dsv);
        }

        _commandList.Get()->OMSetRenderTargets((uint)renderTargets.Length, pRtvHandles, FALSE, pDsvHandle);
    }

    public void BeginRenderPass(ReadOnlySpan<PassRenderTargetDesc> rtDescs, PassDepthStencilDesc depthDesc, bool allowUAVWrites = false)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
        IncrementCommandCount();

        var pRtvDescs = stackalloc D3D12_RENDER_PASS_RENDER_TARGET_DESC[rtDescs.Length];
        for (var i = 0; i < rtDescs.Length; i++)
        {
            var rtDesc = rtDescs[i];
            if (!rtDesc.Texture.IsValid)
            {
                throw new ArgumentException($"Render target at index {i} is not a valid texture handle");
            }

            var resourceInfo = _resourceDatabase.GetResourceRecord(rtDesc.Texture.AsResource());
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
                            Format = resourceInfo.desc.TextureDescription.Format.ToDXGIFormat(),
                        }
                    }
                },
                EndingAccess = new D3D12_RENDER_PASS_ENDING_ACCESS
                {
                    Type = D3D12_RENDER_PASS_ENDING_ACCESS_TYPE_PRESERVE
                }
            };

            desc.BeginningAccess.Clear.ClearValue.Color[0] = rtDesc.ClearColor.r;
            desc.BeginningAccess.Clear.ClearValue.Color[1] = rtDesc.ClearColor.g;
            desc.BeginningAccess.Clear.ClearValue.Color[2] = rtDesc.ClearColor.b;
            desc.BeginningAccess.Clear.ClearValue.Color[3] = rtDesc.ClearColor.a;

            pRtvDescs[i] = desc;
        }

        var pDsvDesc = stackalloc D3D12_RENDER_PASS_DEPTH_STENCIL_DESC[depthDesc.Texture.IsValid ? 1 : 0];
        if (pDsvDesc != null)
        {
            var resourceInfo = _resourceDatabase.GetResourceRecord(depthDesc.Texture.AsResource());
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
                            Format = resourceInfo.desc.TextureDescription.Format.ToDXGIFormat(),
                            DepthStencil = new D3D12_DEPTH_STENCIL_VALUE
                            {
                                Depth = depthDesc.ClearDepth,
                                Stencil = depthDesc.ClearStencil
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

        var d3d12Viewport = new D3D12_VIEWPORT(viewport.X, viewport.Y, viewport.Width, viewport.Height, viewport.MinDepth, viewport.MaxDepth);
        _commandList.Get()->RSSetViewports(1, &d3d12Viewport);
    }

    public void SetPipelineState(GraphicsPipelineKey pipelineKey)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
        IncrementCommandCount();

        var psor = _pipelineLibrary.GetGraphicsPSO(pipelineKey);
        if (psor.Status != ResultStatus.Success)
        {
#if DEBUG || GHOST_EDITOR
            Logger.LogError($"Failed to get graphics pipeline state object for key {pipelineKey}: {psor.Status}");
#endif
            return;
        }

        _commandList.Get()->SetGraphicsRootSignature(_pipelineLibrary.DefaultRootSignature);
        _commandList.Get()->SetPipelineState(psor.Value);
    }

    public void SetConstantBufferView(uint slot, Handle<GraphicsBuffer> buffer)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
        IncrementCommandCount();

        var resource = _resourceDatabase.GetResource(buffer.AsResource());
        _commandList.Get()->SetGraphicsRootConstantBufferView(slot, resource.Get()->GetGPUVirtualAddress());
    }

    public void SetVertexBuffer(uint slot, Handle<GraphicsBuffer> buffer, ulong offset = 0)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
        IncrementCommandCount();

        var resource = _resourceDatabase.GetResource(buffer.AsResource());
        var vbView = new D3D12_VERTEX_BUFFER_VIEW
        {
            BufferLocation = resource.Get()->GetGPUVirtualAddress() + offset,
            SizeInBytes = (uint)(resource.Get()->GetDesc().Width - offset),
            StrideInBytes = _resourceDatabase.GetResourceDescription(buffer.AsResource()).BufferDescription.Stride
        };

        _commandList.Get()->IASetVertexBuffers(slot, 1, &vbView);
    }

    public void SetIndexBuffer(Handle<GraphicsBuffer> buffer, IndexType type, ulong offset = 0)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
        IncrementCommandCount();

        var resource = _resourceDatabase.GetResource(buffer.AsResource());
        var ibView = new D3D12_INDEX_BUFFER_VIEW
        {
            BufferLocation = resource.Get()->GetGPUVirtualAddress() + offset,
            SizeInBytes = (uint)(resource.Get()->GetDesc().Width - offset),
            Format = type == IndexType.UInt16 ? DXGI_FORMAT_R16_UINT : DXGI_FORMAT_R32_UINT
        };

        _commandList.Get()->IASetIndexBuffer(&ibView);
    }

    public void SetPrimitiveTopology(PrimitiveTopology topology)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
        IncrementCommandCount();

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

        // _device.Get()->DispatchRays();
    }

    public void UploadBuffer<T>(Handle<GraphicsBuffer> buffer, ReadOnlySpan<T> data)
        where T : unmanaged
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
        IncrementCommandCount();

        var sizeInBytes = (uint)(data.Length * sizeof(T));

        var uploadHandle = _resourceAllocator.CreateUploadBuffer(sizeInBytes);
        var uploadResource = _resourceDatabase.GetResource(uploadHandle.AsResource());

        void* pMappedData;
        uploadResource.Get()->Map(0, null, &pMappedData);
        fixed (T* pData = data)
        {
            MemoryUtility.MemCpy(pData, pMappedData, sizeInBytes);
        }
        uploadResource.Get()->Unmap(0, null);

        var pResource = _resourceDatabase.GetResource(buffer.AsResource());

        _commandList.Get()->CopyBufferRegion(pResource, 0, uploadResource, 0, sizeInBytes);
        // D3D12 transition resource to COPY_DEST when copying
        _resourceDatabase.SetResourceState(buffer.AsResource(), ResourceState.CopyDest);
    }

    public void UploadTexture(Handle<Texture> texture, ReadOnlySpan<SubResourceData> subresources)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
        IncrementCommandCount();

        var resource = _resourceDatabase.GetResource(texture.AsResource());

        var resourceDesc = resource.Get()->GetDesc();
        var requiredSize = GetRequiredIntermediateSize(resource, 0, (uint)subresources.Length);

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
            resource,
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

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_isRecording)
        {
            throw new InvalidOperationException("Command buffer is still recording");
        }

        _commandList.Dispose();
        _allocator.Dispose();
        _commandCount = 0;

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}