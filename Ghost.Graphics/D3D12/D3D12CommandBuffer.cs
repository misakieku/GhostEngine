using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Graphics.Core;
using Ghost.Graphics.D3D12.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Diagnostics.CodeAnalysis;
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

    private readonly D3D12PipelineLibrary _pipelineLibrary;
    private readonly D3D12ResourceDatabase _resourceDatabase;
    private readonly D3D12ResourceAllocator _resourceAllocator;
    private readonly D3D12DescriptorAllocator _descriptorAllocator;
    private readonly CommandBufferType _type;

#if !DEBUG
    private CommandError _lastError;
#endif
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

        ID3D12GraphicsCommandList10* pCommandList = default;
        var commandListType = D3D12Utility.ToCommandListType(type);

        device.NativeDevice.Get()->CreateCommandList1(0u, commandListType, D3D12_COMMAND_LIST_FLAG_NONE, __uuidof(pCommandList), (void**)&pCommandList);

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if DEBUG
    [DoesNotReturn]
    private static void RecordError(string cmdName, ErrorStatus status)
#else
    private void RecordError(string cmdName, ErrorStatus status)
#endif
    {
#if DEBUG
        throw new InvalidOperationException($"Error at {cmdName} with {status}");
#else

        _lastError = new CommandError
        {
            CommandName = cmdName,
            CommandIndex = _commandCount,
            Status = status
        };
#endif
    }

    public void Begin(ICommandAllocator allocator)
    {
        ThrowIfDisposed();
        ThrowIfRecording();

        if (allocator is not D3D12CommandAllocator d3d12Allocator)
        {
            throw new ArgumentException("Invalid command allocator type", nameof(allocator));
        }

        ThrowIfFailed(_commandList.Get()->Reset(d3d12Allocator.NativeAllocator, null));

        if (Type == CommandBufferType.Graphics || Type == CommandBufferType.Compute)
        {
            // Set descriptor heaps for bindless resources and samplers

            var heaps = stackalloc ID3D12DescriptorHeap*[2];
            heaps[0] = _descriptorAllocator.GetCbvSrvUavHeap(); // Bindless resource heap
            heaps[1] = _descriptorAllocator.GetSamplerHeap();   // Bindless sampler heap
            _commandList.Get()->SetDescriptorHeaps(2, heaps);
        }

        _commandCount = 0;
        _isRecording = true;
    }

    public Result End()
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();

        _commandList.Get()->Close();
        _isRecording = false;

#if !DEBUG
        if (_lastError.Status != ErrorStatus.None)
        {
            return Result.Failure($"Command buffer ended with errors at {_lastError.CommandIndex}, command '{_lastError.CommandName}': {_lastError.Status}");
        }
#endif

        return Result.Success();
    }

    public void SetScissorRect(RectDesc rect)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != ErrorStatus.None)
        {
            return;
        }
#endif
        IncrementCommandCount();

        var d3d12Rect = new RECT((int)rect.Left, (int)rect.Top, (int)rect.Right, (int)rect.Bottom);
        _commandList.Get()->RSSetScissorRects(1, &d3d12Rect);
    }

    public void ResourceBarrier(ReadOnlySpan<BarrierDesc> barrierDescs)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != ErrorStatus.None)
        {
            return;
        }
#endif
        IncrementCommandCount();

        var count = 0u;
        var pBarriers = stackalloc D3D12_RESOURCE_BARRIER[barrierDescs.Length];

        for (var i = 0; i < barrierDescs.Length; i++)
        {
            var desc = barrierDescs[i];
            if (desc.StateBefore == desc.StateAfter)
            {
                continue;
            }

            if (!desc.Resource.IsValid)
            {
                RecordError(nameof(ResourceBarrier), ErrorStatus.InvalidArgument);
                continue;
            }

            var recordResult = _resourceDatabase.GetResourceRecord(desc.Resource);
            if (recordResult.Error != ErrorStatus.None)
            {
                RecordError(nameof(ResourceBarrier), recordResult.Error);
                continue;
            }

            ref var record = ref recordResult.Value;
            if (record.state != desc.StateBefore)
            {
                RecordError(nameof(ResourceBarrier), ErrorStatus.InvalidState);
                continue;
            }

            var barrier = D3D12_RESOURCE_BARRIER.InitTransition(record.ResourcePtr,
                desc.StateBefore.ToD3D12States(), desc.StateAfter.ToD3D12States());

            pBarriers[count] = barrier;
            count++;

            // Update the resource state in the database
            record.state = desc.StateAfter;
        }

        _commandList.Get()->ResourceBarrier(count, pBarriers);
    }

    public void ResourceBarrier(Handle<GPUResource> resource, ResourceState stateBefore, ResourceState stateAfter)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != ErrorStatus.None)
        {
            return;
        }
#endif
        IncrementCommandCount();

        if (stateBefore == stateAfter)
        {
            return;
        }

        var recordResult = _resourceDatabase.GetResourceRecord(resource);
        if (recordResult.Error != ErrorStatus.None)
        {
            RecordError(nameof(ResourceBarrier), recordResult.Error);
            return;
        }

        ref var record = ref recordResult.Value;
        var barrier = D3D12_RESOURCE_BARRIER.InitTransition(record.ResourcePtr,
            stateBefore.ToD3D12States(), stateAfter.ToD3D12States());

        _commandList.Get()->ResourceBarrier(1, &barrier);
        record.state = stateAfter;
    }

    public void ResourceBarrier(Handle<GPUResource> resource, ResourceState stateAfter)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != ErrorStatus.None)
        {
            return;
        }
#endif
        IncrementCommandCount();

        var recordResult = _resourceDatabase.GetResourceRecord(resource);
        if (recordResult.Error != ErrorStatus.None)
        {
            RecordError(nameof(ResourceBarrier), recordResult.Error);
            return;
        }

        ref var record = ref recordResult.Value;
        if (record.state == stateAfter)
        {
            return;
        }

        var barrier = D3D12_RESOURCE_BARRIER.InitTransition(record.ResourcePtr,
            record.state.ToD3D12States(), stateAfter.ToD3D12States());

        _commandList.Get()->ResourceBarrier(1, &barrier);
        record.state = stateAfter;
    }

    public void SetRenderTargets(ReadOnlySpan<Handle<Texture>> renderTargets, Handle<Texture> depthTarget)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != ErrorStatus.None)
        {
            return;
        }
#endif
        IncrementCommandCount();

        var pRtvHandles = stackalloc D3D12_CPU_DESCRIPTOR_HANDLE[renderTargets.Length];
        var rtvCount = 0u;
        for (var i = 0; i < renderTargets.Length; i++)
        {
            var handle = renderTargets[i];
            if (!handle.IsValid)
            {
                RecordError(nameof(SetRenderTargets), ErrorStatus.InvalidArgument);
                continue;
            }

            var recordResult = _resourceDatabase.GetResourceRecord(handle.AsResource());
            if (recordResult.Error != ErrorStatus.None)
            {
                RecordError(nameof(SetRenderTargets), recordResult.Error);
                continue;
            }

            var viewGroup = recordResult.Value.viewGroup;
            pRtvHandles[i] = _descriptorAllocator.GetCpuHandle(viewGroup.rtv);

            rtvCount++;
        }

        var pDsvHandle = stackalloc D3D12_CPU_DESCRIPTOR_HANDLE[depthTarget.IsValid ? 1 : 0];
        if (pDsvHandle != null)
        {
            var recordResult = _resourceDatabase.GetResourceRecord(depthTarget.AsResource());
            if (recordResult.Error != ErrorStatus.None)
            {
                RecordError(nameof(SetRenderTargets), recordResult.Error);
                return;
            }

            var viewGroup = recordResult.Value.viewGroup;
            pDsvHandle[0] = _descriptorAllocator.GetCpuHandle(viewGroup.dsv);
        }

        _commandList.Get()->OMSetRenderTargets(rtvCount, pRtvHandles, FALSE, pDsvHandle);
    }

    public void BeginRenderPass(ReadOnlySpan<PassRenderTargetDesc> rtDescs, PassDepthStencilDesc depthDesc, bool allowUAVWrites = false)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != ErrorStatus.None)
        {
            return;
        }
#endif
        IncrementCommandCount();

        var pRtvDescs = stackalloc D3D12_RENDER_PASS_RENDER_TARGET_DESC[rtDescs.Length];
        for (var i = 0; i < rtDescs.Length; i++)
        {
            var rtDesc = rtDescs[i];
            if (!rtDesc.Texture.IsValid)
            {
                RecordError(nameof(BeginRenderPass), ErrorStatus.InvalidArgument);
                continue;
            }

            var recordResult = _resourceDatabase.GetResourceRecord(rtDesc.Texture.AsResource());
            if (recordResult.Error != ErrorStatus.None)
            {
                RecordError(nameof(BeginRenderPass), recordResult.Error);
                continue;
            }

            var record = recordResult.Value;
            var cpuHandle = _descriptorAllocator.GetCpuHandle(record.viewGroup.rtv);
            var format = record.desc.TextureDescription.Format.ToDXGIFormat();
            var clearColor = rtDesc.ClearColor;

            var desc = new D3D12_RENDER_PASS_RENDER_TARGET_DESC
            {
                cpuDescriptor = cpuHandle,
                BeginningAccess = new D3D12_RENDER_PASS_BEGINNING_ACCESS
                {
                    Type = D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_CLEAR,
                    Clear = new D3D12_RENDER_PASS_BEGINNING_ACCESS_CLEAR_PARAMETERS
                    {
                        ClearValue = new D3D12_CLEAR_VALUE(format, (float*)&clearColor)
                    }
                },
                EndingAccess = new D3D12_RENDER_PASS_ENDING_ACCESS
                {
                    Type = D3D12_RENDER_PASS_ENDING_ACCESS_TYPE_PRESERVE
                }
            };

            pRtvDescs[i] = desc;
        }

        var pDsvDesc = stackalloc D3D12_RENDER_PASS_DEPTH_STENCIL_DESC[depthDesc.Texture.IsValid ? 1 : 0];
        if (pDsvDesc != null)
        {
            var recordResult = _resourceDatabase.GetResourceRecord(depthDesc.Texture.AsResource());
            if (recordResult.Error != ErrorStatus.None)
            {
                RecordError(nameof(BeginRenderPass), recordResult.Error);
                return;
            }

            var record = recordResult.Value;
            var cpuHandle = _descriptorAllocator.GetCpuHandle(record.viewGroup.dsv);
            var format = record.desc.TextureDescription.Format.ToDXGIFormat();

            var desc = new D3D12_RENDER_PASS_DEPTH_STENCIL_DESC
            {
                cpuDescriptor = cpuHandle,
                DepthBeginningAccess = new D3D12_RENDER_PASS_BEGINNING_ACCESS
                {
                    Type = D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_CLEAR,
                    Clear = new D3D12_RENDER_PASS_BEGINNING_ACCESS_CLEAR_PARAMETERS
                    {
                        ClearValue = new D3D12_CLEAR_VALUE(format, depthDesc.ClearDepth, depthDesc.ClearStencil)
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
#if !DEBUG
        if (_lastError.Status != ErrorStatus.None)
        {
            return;
        }
#endif
        IncrementCommandCount();

        _commandList.Get()->EndRenderPass();
    }

    public void SetViewport(ViewportDesc viewport)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != ErrorStatus.None)
        {
            return;
        }
#endif
        IncrementCommandCount();

        var d3d12Viewport = new D3D12_VIEWPORT(viewport.X, viewport.Y, viewport.Width, viewport.Height, viewport.MinDepth, viewport.MaxDepth);
        _commandList.Get()->RSSetViewports(1, &d3d12Viewport);
    }

    public void SetPipelineState(GraphicsPipelineKey pipelineKey)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != ErrorStatus.None)
        {
            return;
        }
#endif
        IncrementCommandCount();

        var psor = _pipelineLibrary.GetGraphicsPSO(pipelineKey);
        if (psor.Error != ErrorStatus.None)
        {
            RecordError(nameof(SetPipelineState), psor.Error);
            return;
        }

        _commandList.Get()->SetGraphicsRootSignature(_pipelineLibrary.DefaultRootSignature);
        _commandList.Get()->SetPipelineState(psor.Value);
    }

    public void SetConstantBufferView(uint slot, Handle<GraphicsBuffer> buffer)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != ErrorStatus.None)
        {
            return;
        }
#endif
        IncrementCommandCount();

        var resource = _resourceDatabase.GetResource(buffer.AsResource());
        _commandList.Get()->SetGraphicsRootConstantBufferView(slot, resource.Get()->GetGPUVirtualAddress());
    }

    public void SetVertexBuffer(uint slot, Handle<GraphicsBuffer> buffer, ulong offset = 0)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != ErrorStatus.None)
        {
            return;
        }
#endif
        IncrementCommandCount();

        var recordResult = _resourceDatabase.GetResourceRecord(buffer.AsResource());
        if (recordResult.Error != ErrorStatus.None)
        {
            RecordError(nameof(BeginRenderPass), recordResult.Error);
            return;
        }

        var record = recordResult.Value;
        var vbView = new D3D12_VERTEX_BUFFER_VIEW
        {
            BufferLocation = record.ResourcePtr.Get()->GetGPUVirtualAddress() + offset,
            SizeInBytes = (uint)(record.ResourcePtr.Get()->GetDesc().Width - offset),
            StrideInBytes = record.desc.BufferDescription.Stride
        };

        _commandList.Get()->IASetVertexBuffers(slot, 1, &vbView);
    }

    public void SetIndexBuffer(Handle<GraphicsBuffer> buffer, IndexType type, ulong offset = 0)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != ErrorStatus.None)
        {
            return;
        }
#endif
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
#if !DEBUG
        if (_lastError.Status != ErrorStatus.None)
        {
            return;
        }
#endif
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
#if !DEBUG
        if (_lastError.Status != ErrorStatus.None)
        {
            return;
        }
#endif
        IncrementCommandCount();

        _commandList.Get()->DrawInstanced(vertexCount, instanceCount, startVertex, startInstance);
    }

    public void DrawIndexed(uint indexCount, uint instanceCount = 1, uint startIndex = 0, int baseVertex = 0, uint startInstance = 0)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != ErrorStatus.None)
        {
            return;
        }
#endif
        IncrementCommandCount();

        _commandList.Get()->DrawIndexedInstanced(indexCount, instanceCount, startIndex, baseVertex, startInstance);
    }

    public void DispatchCompute(uint threadGroupCountX, uint threadGroupCountY, uint threadGroupCountZ)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != ErrorStatus.None)
        {
            return;
        }
#endif
        IncrementCommandCount();

        _commandList.Get()->Dispatch(threadGroupCountX, threadGroupCountY, threadGroupCountZ);
    }

    public void DispatchMesh(uint threadGroupCountX, uint threadGroupCountY, uint threadGroupCountZ)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != ErrorStatus.None)
        {
            return;
        }
#endif
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
#if !DEBUG
        if (_lastError.Status != ErrorStatus.None)
        {
            return;
        }
#endif
        IncrementCommandCount();

        var sizeInBytes = (uint)(data.Length * sizeof(T));

        var uploadHandle = _resourceAllocator.CreateUploadBuffer(sizeInBytes);
        var uploadResource = _resourceDatabase.GetResource(uploadHandle.AsResource());

        void* pMappedData;
        uploadResource.Get()->Map(0, null, &pMappedData);
        fixed (T* pData = data)
        {
            MemoryUtility.MemCpy(pMappedData, pData, sizeInBytes);
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
#if !DEBUG
        if (_lastError.Status != ErrorStatus.None)
        {
            return;
        }
#endif
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
                RowPitch = (nint)subresources[i].rowPitch,
                SlicePitch = (nint)subresources[i].slicePitch
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
#if !DEBUG
        if (_lastError.Status != ErrorStatus.None)
        {
            return;
        }
#endif
        IncrementCommandCount();

        var pDestResource = _resourceDatabase.GetResource(dest.AsResource());
        var pSrcResource = _resourceDatabase.GetResource(src.AsResource());
        if (pSrcResource == null || pDestResource == null)
        {
            RecordError(nameof(CopyBuffer), ErrorStatus.InvalidArgument);
            return;
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
        _commandCount = 0;

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
