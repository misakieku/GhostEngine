using Ghost.Core;
using Ghost.Graphics.D3D12.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using static TerraFX.Aliases.D3D_Alias;
using static TerraFX.Aliases.D3D12_Alias;
using static TerraFX.Aliases.DXGI_Alias;

namespace Ghost.Graphics.D3D12;

internal unsafe class D3D12CommandBuffer : D3D12Object<ID3D12GraphicsCommandList10>, ICommandBuffer
{
    private readonly D3D12PipelineLibrary _pipelineLibrary;
    private readonly D3D12ResourceDatabase _resourceDatabase;
    private readonly D3D12ResourceAllocator _resourceAllocator;
    private readonly D3D12DescriptorAllocator _descriptorAllocator;
    private readonly CommandBufferType _type;

    private CommandError _lastError;
    private ushort _commandCount;
    private bool _isRecording;

    public CommandBufferType Type => _type;
    public bool IsEmpty => _commandCount == 0;

    private static ID3D12GraphicsCommandList10* CreateCommandList(ID3D12Device14* device, D3D12_COMMAND_LIST_TYPE type)
    {
        ID3D12GraphicsCommandList10* pCommandList = default;
        ThrowIfFailed(device->CreateCommandList1(0u, type, D3D12_COMMAND_LIST_FLAG_NONE, __uuidof(pCommandList), (void**)&pCommandList));
        return pCommandList;
    }

    public D3D12CommandBuffer(
        D3D12RenderDevice device,
        D3D12PipelineLibrary pipelineLibrary,
        D3D12ResourceDatabase resourceDatabase,
        D3D12ResourceAllocator resourceAllocator,
        D3D12DescriptorAllocator descriptorAllocator,
        CommandBufferType type)
        : base(CreateCommandList(device.NativeObject, D3D12Utility.ToCommandListType(type)))
    {
        _type = type;

        _pipelineLibrary = pipelineLibrary;
        _resourceDatabase = resourceDatabase;
        _resourceAllocator = resourceAllocator;
        _descriptorAllocator = descriptorAllocator;

        _isRecording = false;
    }

    [Conditional("DEBUG")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfRecording()
    {
        if (_isRecording)
        {
            throw new InvalidOperationException("Command buffer is already recording");
        }
    }

    [Conditional("DEBUG")]
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
    private void RecordError(string cmdName, Error status)
    {
        Debug.Fail($"Command '{cmdName}' failed with error: {status}");

        _lastError = new CommandError
        {
            CommandName = cmdName,
            CommandIndex = _commandCount,
            Status = status
        };
    }

    public void Begin(ICommandAllocator allocator)
    {
        ThrowIfDisposed();
        ThrowIfRecording();

        if (allocator is not D3D12CommandAllocator d3d12Allocator)
        {
            throw new ArgumentException("Invalid command allocator type", nameof(allocator));
        }

        ThrowIfFailed(pNativeObject->Reset(d3d12Allocator.NativeObject, null));

        if (Type == CommandBufferType.Graphics || Type == CommandBufferType.Compute)
        {
            // Set descriptor heaps for bindless resources and samplers

            var heaps = stackalloc ID3D12DescriptorHeap*[2];
            heaps[0] = _descriptorAllocator.GetCbvSrvUavHeap(); // Bindless resource Heap
            heaps[1] = _descriptorAllocator.GetSamplerHeap();   // Bindless sampler Heap
            pNativeObject->SetDescriptorHeaps(2, heaps);
        }

        _commandCount = 0;
        _isRecording = true;
    }

    public Result End()
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();

        pNativeObject->Close();
        _isRecording = false;

#if !DEBUG
        if (_lastError.Status != Error.None)
        {
            return Result.Failure($"Command buffer ended with errors at {_lastError.CommandIndex}, command '{_lastError.CommandName}': {_lastError.Status}");
        }
#endif

        return Result.Success();
    }

    public void SetScissorRect(ScissorRectDesc rect)
    {
        AssertNotDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != Error.None)
        {
            return;
        }
#endif
        IncrementCommandCount();

        var d3d12Rect = new RECT((int)rect.Left, (int)rect.Top, (int)rect.Right, (int)rect.Bottom);
        pNativeObject->RSSetScissorRects(1, &d3d12Rect);
    }

    public void Barrier(params ReadOnlySpan<BarrierDesc> barrierDescs)
    {
        AssertNotDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != Error.None)
        {
            return;
        }
#endif
        IncrementCommandCount();

        if (barrierDescs.IsEmpty)
        {
            return;
        }

        var globalCount = 0;
        var bufferCount = 0;
        var textureCount = 0;

        for (var i = 0; i < barrierDescs.Length; i++)
        {
            switch (barrierDescs[i].Type)
            {
                case BarrierType.Global: globalCount++; break;
                case BarrierType.Buffer: bufferCount++; break;
                case BarrierType.Texture: textureCount++; break;
            }
        }

        var pGlobalBarriers = stackalloc D3D12_GLOBAL_BARRIER[globalCount];
        var pBufferBarriers = stackalloc D3D12_BUFFER_BARRIER[bufferCount];
        var pTextureBarriers = stackalloc D3D12_TEXTURE_BARRIER[textureCount];

        var globalIndex = 0;
        var bufferIndex = 0;
        var textureIndex = 0;

        for (var i = 0; i < barrierDescs.Length; i++)
        {
            var desc = barrierDescs[i];
            switch (desc.Type)
            {
                case BarrierType.Global:
                    if (desc.SyncAfter == _resourceDatabase.globalBarrier.sync && desc.AccessAfter == _resourceDatabase.globalBarrier.access)
                    {
                        continue;
                    }

                    pGlobalBarriers[globalIndex++] = new D3D12_GLOBAL_BARRIER
                    {
                        SyncBefore = (D3D12_BARRIER_SYNC)_resourceDatabase.globalBarrier.sync,
                        SyncAfter = (D3D12_BARRIER_SYNC)desc.SyncAfter,
                        AccessBefore = (D3D12_BARRIER_ACCESS)_resourceDatabase.globalBarrier.access,
                        AccessAfter = (D3D12_BARRIER_ACCESS)desc.AccessAfter
                    };

                    _resourceDatabase.globalBarrier = new ResourceBarrierData(BarrierLayout.Undefined, desc.AccessAfter, desc.SyncAfter);
                    break;
                case BarrierType.Buffer:
                    {
                        var r = _resourceDatabase.GetResourceRecord(desc.Resource);
                        if (r.IsFailure)
                        {
                            RecordError(nameof(Barrier), r.Error);
                            continue;
                        }

                        ref var record = ref r.Value;
                        var accessBefore = desc.IsAliasing ? BarrierAccess.NoAccess : record.barrierData.access;

                        if (record.barrierData.sync == desc.SyncAfter && accessBefore == desc.AccessAfter)
                        {
                            continue;
                        }

                        var resource = record.ResourcePtr;
                        pBufferBarriers[bufferIndex++] = new D3D12_BUFFER_BARRIER
                        {
                            SyncBefore = (D3D12_BARRIER_SYNC)record.barrierData.sync,
                            SyncAfter = (D3D12_BARRIER_SYNC)desc.SyncAfter,
                            AccessBefore = (D3D12_BARRIER_ACCESS)accessBefore,
                            AccessAfter = (D3D12_BARRIER_ACCESS)desc.AccessAfter,
                            pResource = resource,
                            Offset = 0,
                            Size = ulong.MaxValue
                        };

                        record.barrierData = new ResourceBarrierData(BarrierLayout.Undefined, desc.AccessAfter, desc.SyncAfter);
                    }
                    break;
                case BarrierType.Texture:
                    {
                        var r = _resourceDatabase.GetResourceRecord(desc.Resource);
                        if (r.IsFailure)
                        {
                            RecordError(nameof(Barrier), r.Error);
                            continue;
                        }

                        ref var record = ref r.Value;
                        var accessBefore = desc.IsAliasing ? BarrierAccess.NoAccess : record.barrierData.access;
                        var layoutBefore = desc.IsAliasing ? BarrierLayout.Undefined : record.barrierData.layout;

                        if (record.barrierData.sync == desc.SyncAfter && accessBefore == desc.AccessAfter && layoutBefore == desc.LayoutAfter)
                        {
                            continue;
                        }

                        var resource = record.ResourcePtr;
                        pTextureBarriers[textureIndex++] = new D3D12_TEXTURE_BARRIER
                        {
                            SyncBefore = (D3D12_BARRIER_SYNC)record.barrierData.sync,
                            SyncAfter = (D3D12_BARRIER_SYNC)desc.SyncAfter,
                            AccessBefore = (D3D12_BARRIER_ACCESS)accessBefore,
                            AccessAfter = (D3D12_BARRIER_ACCESS)desc.AccessAfter,
                            LayoutBefore = (D3D12_BARRIER_LAYOUT)layoutBefore,
                            LayoutAfter = (D3D12_BARRIER_LAYOUT)desc.LayoutAfter,
                            pResource = resource,
                            Subresources = new D3D12_BARRIER_SUBRESOURCE_RANGE
                            {
                                IndexOrFirstMipLevel = desc.Subresources.IndexOrFirstMipLevel,
                                NumMipLevels = desc.Subresources.NumMipLevels,
                                FirstArraySlice = desc.Subresources.FirstArraySlice,
                                NumArraySlices = desc.Subresources.NumArraySlices
                            },
                            Flags = desc.Discard ? D3D12_TEXTURE_BARRIER_FLAGS.D3D12_TEXTURE_BARRIER_FLAG_DISCARD : D3D12_TEXTURE_BARRIER_FLAGS.D3D12_TEXTURE_BARRIER_FLAG_NONE
                        };

                        record.barrierData = new ResourceBarrierData(desc.LayoutAfter, desc.AccessAfter, desc.SyncAfter);
                    }
                    break;
            }
        }

        var groups = stackalloc D3D12_BARRIER_GROUP[3];
        var groupCount = 0u;

        if (globalIndex > 0)
        {
            groups[groupCount] = new D3D12_BARRIER_GROUP
            {
                Type = D3D12_BARRIER_TYPE.D3D12_BARRIER_TYPE_GLOBAL,
                NumBarriers = (uint)globalIndex,
            };
            groups[groupCount].Anonymous.pGlobalBarriers = pGlobalBarriers;
            groupCount++;
        }

        if (bufferIndex > 0)
        {
            groups[groupCount] = new D3D12_BARRIER_GROUP
            {
                Type = D3D12_BARRIER_TYPE.D3D12_BARRIER_TYPE_BUFFER,
                NumBarriers = (uint)bufferIndex,
            };
            groups[groupCount].Anonymous.pBufferBarriers = pBufferBarriers;
            groupCount++;
        }

        if (textureIndex > 0)
        {
            groups[groupCount] = new D3D12_BARRIER_GROUP
            {
                Type = D3D12_BARRIER_TYPE.D3D12_BARRIER_TYPE_TEXTURE,
                NumBarriers = (uint)textureIndex,
            };
            groups[groupCount].Anonymous.pTextureBarriers = pTextureBarriers;
            groupCount++;
        }

        if (groupCount == 0)
        {
            return;
        }

        pNativeObject->Barrier(groupCount, groups);
    }

    public void SetRenderTargets(ReadOnlySpan<Handle<GPUTexture>> renderTargets, Handle<GPUTexture> depthTarget)
    {
        AssertNotDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != Error.None)
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
                RecordError(nameof(SetRenderTargets), Error.InvalidArgument);
                continue;
            }

            var recordResult = _resourceDatabase.GetResourceRecord(handle.AsResource());
            if (recordResult.Error != Error.None)
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
            if (recordResult.Error != Error.None)
            {
                RecordError(nameof(SetRenderTargets), recordResult.Error);
                return;
            }

            var viewGroup = recordResult.Value.viewGroup;
            pDsvHandle[0] = _descriptorAllocator.GetCpuHandle(viewGroup.dsv);
        }

        pNativeObject->OMSetRenderTargets(rtvCount, pRtvHandles, FALSE, pDsvHandle);
    }

    public void ClearRenderTargetView(Handle<GPUTexture> renderTarget, Color128 clearColor)
    {
        AssertNotDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != Error.None)
        {
            return;
        }
#endif
        IncrementCommandCount();

        var recordResult = _resourceDatabase.GetResourceRecord(renderTarget.AsResource());
        if (recordResult.Error != Error.None)
        {
            RecordError(nameof(ClearRenderTargetView), recordResult.Error);
            return;
        }

        ref var record = ref recordResult.Value;
        var cpuHandle = _descriptorAllocator.GetCpuHandle(record.viewGroup.rtv);

        pNativeObject->ClearRenderTargetView(cpuHandle, (float*)&clearColor, 0, null);
    }

    public void ClearDepthStencilView(Handle<GPUTexture> depthStencil, bool inlcudeDepth, bool includeStencil, float clearDepth = 1.0f, byte clearStencil = 0)
    {
        AssertNotDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != Error.None)
        {
            return;
        }
#endif
        IncrementCommandCount();

        var recordResult = _resourceDatabase.GetResourceRecord(depthStencil.AsResource());
        if (recordResult.Error != Error.None)
        {
            RecordError(nameof(ClearDepthStencilView), recordResult.Error);
            return;
        }

        ref var record = ref recordResult.Value;
        var cpuHandle = _descriptorAllocator.GetCpuHandle(record.viewGroup.dsv);
        var flag = (inlcudeDepth ? D3D12_CLEAR_FLAG_DEPTH : 0) | (includeStencil ? D3D12_CLEAR_FLAG_STENCIL : 0);

        pNativeObject->ClearDepthStencilView(cpuHandle,
            flag,
            clearDepth,
            clearStencil,
            0,
            null);
    }

    public void BeginRenderPass(ReadOnlySpan<PassRenderTargetDesc> rtDescs, PassDepthStencilDesc depthDesc, bool allowUAVWrites = false)
    {
        AssertNotDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != Error.None)
        {
            return;
        }
#endif
        IncrementCommandCount();

        var pRtvDescs = stackalloc D3D12_RENDER_PASS_RENDER_TARGET_DESC[rtDescs.Length];
        for (var i = 0; i < rtDescs.Length; i++)
        {
            var rtDesc = rtDescs[i];
            if (rtDesc.Texture.IsInvalid)
            {
                RecordError(nameof(BeginRenderPass), Error.InvalidArgument);
                continue;
            }

            var recordResult = _resourceDatabase.GetResourceRecord(rtDesc.Texture.AsResource());
            if (recordResult.Error != Error.None)
            {
                RecordError(nameof(BeginRenderPass), recordResult.Error);
                continue;
            }

            ref var record = ref recordResult.Value;
            var cpuHandle = _descriptorAllocator.GetCpuHandle(record.viewGroup.rtv);
            var format = record.desc.TextureDescription.Format.ToDXGIFormat();
            var clearColor = rtDesc.ClearColor;

            // Map load operation
            var loadAccessType = rtDesc.LoadOp switch
            {
                AttachmentLoadOp.Load => D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_PRESERVE,
                AttachmentLoadOp.Clear => D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_CLEAR,
                AttachmentLoadOp.DontCare => D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_DISCARD,
                _ => D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_PRESERVE
            };

            // Map store operation
            var storeAccessType = rtDesc.StoreOp switch
            {
                AttachmentStoreOp.Store => D3D12_RENDER_PASS_ENDING_ACCESS_TYPE_PRESERVE,
                AttachmentStoreOp.DontCare => D3D12_RENDER_PASS_ENDING_ACCESS_TYPE_DISCARD,
                _ => D3D12_RENDER_PASS_ENDING_ACCESS_TYPE_PRESERVE
            };

            var desc = new D3D12_RENDER_PASS_RENDER_TARGET_DESC
            {
                cpuDescriptor = cpuHandle,
                BeginningAccess = new D3D12_RENDER_PASS_BEGINNING_ACCESS
                {
                    Type = loadAccessType,
                    Clear = loadAccessType == D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_CLEAR
                        ? new D3D12_RENDER_PASS_BEGINNING_ACCESS_CLEAR_PARAMETERS
                        {
                            ClearValue = new D3D12_CLEAR_VALUE(format, (float*)&clearColor)
                        }
                        : default
                },
                EndingAccess = new D3D12_RENDER_PASS_ENDING_ACCESS
                {
                    Type = storeAccessType
                }
            };

            pRtvDescs[i] = desc;
        }

        var pDsvDesc = stackalloc D3D12_RENDER_PASS_DEPTH_STENCIL_DESC[depthDesc.Texture.IsValid ? 1 : 0];
        if (pDsvDesc != null)
        {
            var recordResult = _resourceDatabase.GetResourceRecord(depthDesc.Texture.AsResource());
            if (recordResult.Error != Error.None)
            {
                RecordError(nameof(BeginRenderPass), recordResult.Error);
                return;
            }

            ref var record = ref recordResult.Value;
            var cpuHandle = _descriptorAllocator.GetCpuHandle(record.viewGroup.dsv);
            var format = record.desc.TextureDescription.Format.ToDXGIFormat();

            // Map depth load operation
            var depthLoadAccessType = depthDesc.DepthLoadOp switch
            {
                AttachmentLoadOp.Load => D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_PRESERVE,
                AttachmentLoadOp.Clear => D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_CLEAR,
                AttachmentLoadOp.DontCare => D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_DISCARD,
                AttachmentLoadOp.NoAccess => D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_NO_ACCESS,
                _ => D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_PRESERVE
            };

            // Map depth store operation
            var depthStoreAccessType = depthDesc.DepthStoreOp switch
            {
                AttachmentStoreOp.Store => D3D12_RENDER_PASS_ENDING_ACCESS_TYPE_PRESERVE,
                AttachmentStoreOp.DontCare => D3D12_RENDER_PASS_ENDING_ACCESS_TYPE_DISCARD,
                AttachmentStoreOp.NoAccess => D3D12_RENDER_PASS_ENDING_ACCESS_TYPE_NO_ACCESS,
                _ => D3D12_RENDER_PASS_ENDING_ACCESS_TYPE_NO_ACCESS
            };

            // Map stencil load operation
            var stencilLoadAccessType = depthDesc.StencilLoadOp switch
            {
                AttachmentLoadOp.Load => D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_PRESERVE,
                AttachmentLoadOp.Clear => D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_CLEAR,
                AttachmentLoadOp.DontCare => D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_DISCARD,
                AttachmentLoadOp.NoAccess => D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_NO_ACCESS,
                _ => D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_PRESERVE
            };

            // Map stencil store operation
            var stencilStoreAccessType = depthDesc.StencilStoreOp switch
            {
                AttachmentStoreOp.Store => D3D12_RENDER_PASS_ENDING_ACCESS_TYPE_PRESERVE,
                AttachmentStoreOp.DontCare => D3D12_RENDER_PASS_ENDING_ACCESS_TYPE_DISCARD,
                AttachmentStoreOp.NoAccess => D3D12_RENDER_PASS_ENDING_ACCESS_TYPE_NO_ACCESS,
                _ => D3D12_RENDER_PASS_ENDING_ACCESS_TYPE_NO_ACCESS
            };

            var desc = new D3D12_RENDER_PASS_DEPTH_STENCIL_DESC
            {
                cpuDescriptor = cpuHandle,
                DepthBeginningAccess = new D3D12_RENDER_PASS_BEGINNING_ACCESS
                {
                    Type = depthLoadAccessType,
                    Clear = depthLoadAccessType == D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_CLEAR
                        ? new D3D12_RENDER_PASS_BEGINNING_ACCESS_CLEAR_PARAMETERS
                        {
                            ClearValue = new D3D12_CLEAR_VALUE(format, depthDesc.ClearDepth, depthDesc.ClearStencil)
                        }
                        : default
                },
                DepthEndingAccess = new D3D12_RENDER_PASS_ENDING_ACCESS
                {
                    Type = depthStoreAccessType
                },
                StencilBeginningAccess = new D3D12_RENDER_PASS_BEGINNING_ACCESS
                {
                    Type = stencilLoadAccessType,
                    Clear = stencilLoadAccessType == D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_CLEAR
                        ? new D3D12_RENDER_PASS_BEGINNING_ACCESS_CLEAR_PARAMETERS
                        {
                            ClearValue = new D3D12_CLEAR_VALUE(format, depthDesc.ClearDepth, depthDesc.ClearStencil)
                        }
                        : default
                },
                StencilEndingAccess = new D3D12_RENDER_PASS_ENDING_ACCESS
                {
                    Type = stencilStoreAccessType
                }
            };

            pDsvDesc[0] = desc;
        }

        pNativeObject->BeginRenderPass((uint)rtDescs.Length, pRtvDescs, pDsvDesc,
                allowUAVWrites ? D3D12_RENDER_PASS_FLAG_ALLOW_UAV_WRITES : D3D12_RENDER_PASS_FLAG_NONE);
    }

    public void EndRenderPass()
    {
        AssertNotDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != Error.None)
        {
            return;
        }
#endif
        IncrementCommandCount();

        pNativeObject->EndRenderPass();
    }

    public void SetViewport(ViewportDesc viewport)
    {
        AssertNotDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != Error.None)
        {
            return;
        }
#endif
        IncrementCommandCount();

        var d3d12Viewport = new D3D12_VIEWPORT(viewport.X, viewport.Y, viewport.Width, viewport.Height, viewport.MinDepth, viewport.MaxDepth);
        pNativeObject->RSSetViewports(1, &d3d12Viewport);
    }

    public void SetPipelineState(Key128<GraphicsPipeline> pipelineKey)
    {
        AssertNotDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != Error.None)
        {
            return;
        }
#endif
        IncrementCommandCount();

        var psor = _pipelineLibrary.GetGraphicsPSO(pipelineKey);
        if (psor.Error != Error.None)
        {
            RecordError(nameof(SetPipelineState), psor.Error);
            return;
        }

        pNativeObject->SetGraphicsRootSignature(_pipelineLibrary.DefaultRootSignature);
        pNativeObject->SetPipelineState(psor.Value);
    }

    public void SetConstantBufferView(uint slot, Handle<GPUBuffer> buffer)
    {
        AssertNotDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != Error.None)
        {
            return;
        }
#endif
        IncrementCommandCount();

        var resource = _resourceDatabase.GetResource(buffer.AsResource());
        pNativeObject->SetGraphicsRootConstantBufferView(slot, resource.Get()->GetGPUVirtualAddress());
    }

    public void SetVertexBuffer(uint slot, Handle<GPUBuffer> buffer, ulong offset = 0)
    {
        AssertNotDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != Error.None)
        {
            return;
        }
#endif
        IncrementCommandCount();

        var recordResult = _resourceDatabase.GetResourceRecord(buffer.AsResource());
        if (recordResult.Error != Error.None)
        {
            RecordError(nameof(BeginRenderPass), recordResult.Error);
            return;
        }

        ref var record = ref recordResult.Value;
        var vbView = new D3D12_VERTEX_BUFFER_VIEW
        {
            BufferLocation = record.ResourcePtr.Get()->GetGPUVirtualAddress() + offset,
            SizeInBytes = (uint)(record.ResourcePtr.Get()->GetDesc().Width - offset),
            StrideInBytes = record.desc.BufferDescription.Stride
        };

        pNativeObject->IASetVertexBuffers(slot, 1, &vbView);
    }

    public void SetIndexBuffer(Handle<GPUBuffer> buffer, IndexType type, ulong offset = 0)
    {
        AssertNotDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != Error.None)
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

        pNativeObject->IASetIndexBuffer(&ibView);
    }

    public void SetPrimitiveTopology(PrimitiveTopology topology)
    {
        AssertNotDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != Error.None)
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

        pNativeObject->IASetPrimitiveTopology(d3d12Topology);
    }

    public void SetGraphicsRoot32Constants(uint rootIndex, ReadOnlySpan<uint> constantBuffer, uint offsetIn32Bits = 0)
    {
        AssertNotDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != Error.None)
        {
            return;
        }
#endif
        IncrementCommandCount();

        fixed (uint* pConstants = constantBuffer)
        {
            pNativeObject->SetGraphicsRoot32BitConstants(rootIndex, (uint)constantBuffer.Length, pConstants, offsetIn32Bits);
        }
    }

    public void Draw(uint vertexCount, uint instanceCount = 1, uint startVertex = 0, uint startInstance = 0)
    {
        AssertNotDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != Error.None)
        {
            return;
        }
#endif
        IncrementCommandCount();

        pNativeObject->DrawInstanced(vertexCount, instanceCount, startVertex, startInstance);
    }

    public void DrawIndexed(uint indexCount, uint instanceCount = 1, uint startIndex = 0, int baseVertex = 0, uint startInstance = 0)
    {
        AssertNotDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != Error.None)
        {
            return;
        }
#endif
        IncrementCommandCount();

        pNativeObject->DrawIndexedInstanced(indexCount, instanceCount, startIndex, baseVertex, startInstance);
    }

    public void DispatchCompute(uint threadGroupCountX, uint threadGroupCountY, uint threadGroupCountZ)
    {
        AssertNotDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != Error.None)
        {
            return;
        }
#endif
        IncrementCommandCount();

        pNativeObject->Dispatch(threadGroupCountX, threadGroupCountY, threadGroupCountZ);
    }

    public void DispatchMesh(uint threadGroupCountX, uint threadGroupCountY, uint threadGroupCountZ)
    {
        AssertNotDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != Error.None)
        {
            return;
        }
#endif
        IncrementCommandCount();

        pNativeObject->DispatchMesh(threadGroupCountX, threadGroupCountY, threadGroupCountZ);
    }

    public void DispatchRay()
    {
        throw new NotImplementedException();
    }

    public void DispatchGraph()
    {
        throw new NotImplementedException();
    }

    public void ExecuteIndirect(ICommandSignature commandSignature, Handle<GPUBuffer> argumentBuffer, ulong argumentOffset, Handle<GPUBuffer> countBuffer, ulong countBufferOffset)
    {
        AssertNotDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != Error.None)
        {
            return;
        }
#endif

        IncrementCommandCount();

        Debug.Assert(commandSignature is D3D12CommandSignature);

        var resource = _resourceDatabase.GetResource(argumentBuffer.AsResource());
        var countResource = _resourceDatabase.GetResource(countBuffer.AsResource());

        pNativeObject->ExecuteIndirect(((D3D12CommandSignature)commandSignature).NativeObject, 0,
            resource, argumentOffset, countResource, countBufferOffset);
    }

    public void CopyBuffer(Handle<GPUBuffer> dst, Handle<GPUBuffer> src, ulong dstOffset = 0, ulong srcOffset = 0, ulong numBytes = 0)
    {
        AssertNotDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != Error.None)
        {
            return;
        }
#endif

        if (dst == src)
        {
            return;
        }

        IncrementCommandCount();

        var pDstResource = _resourceDatabase.GetResource(dst.AsResource());
        var pSrcResource = _resourceDatabase.GetResource(src.AsResource());
        if (pSrcResource == null || pDstResource == null)
        {
            RecordError(nameof(CopyBuffer), Error.InvalidArgument);
            return;
        }

        if (numBytes == 0)
        {
            pNativeObject->CopyResource(pDstResource, pSrcResource);
        }
        else
        {
            pNativeObject->CopyBufferRegion(pDstResource, dstOffset, pSrcResource, srcOffset, numBytes);
        }
    }

    public void UpdateSubResources(Handle<GPUResource> resource, Handle<GPUResource> intermediate, params ReadOnlySpan<SubResourceData> subResources)
    {
        AssertNotDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != Error.None)
        {
            return;
        }
#endif

        IncrementCommandCount();

        var d3d12Resource = _resourceDatabase.GetResource(resource);
        var d3d12Intermediate = _resourceDatabase.GetResource(intermediate);
        if (d3d12Intermediate == null || d3d12Resource == null)
        {
            RecordError(nameof(UpdateSubResources), Error.InvalidArgument);
            return;
        }

        var d3d12Subresources = stackalloc D3D12_SUBRESOURCE_DATA[subResources.Length];
        for (var i = 0; i < subResources.Length; i++)
        {
            d3d12Subresources[i] = new D3D12_SUBRESOURCE_DATA
            {
                pData = subResources[i].pData,
                RowPitch = (nint)subResources[i].rowPitch,
                SlicePitch = (nint)subResources[i].slicePitch
            };
        }

        UpdateSubresources(
            (ID3D12GraphicsCommandList*)pNativeObject,
            d3d12Resource,
            d3d12Intermediate,
            0,
            0,
            (uint)subResources.Length,
            d3d12Subresources);
    }

    private D3D12_TEXTURE_COPY_LOCATION GetTextureCopyLocation(SharedPtr<ID3D12Resource> texture, TextureSubresource subres)
    {
        var flatIndex = subres.MipLevel + subres.ArrayLayer * texture.Get()->GetDesc().MipLevels;

        return new D3D12_TEXTURE_COPY_LOCATION
        {
            pResource = texture,
            Type = D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX,
            SubresourceIndex = flatIndex
        };
    }

    private bool AreTexturesIdentical(SharedPtr<ID3D12Resource> tex1, SharedPtr<ID3D12Resource> tex2)
    {
        var desc1 = tex1.Get()->GetDesc();
        var desc2 = tex2.Get()->GetDesc();
        return desc1.Width == desc2.Width
               && desc1.Height == desc2.Height
               && desc1.DepthOrArraySize == desc2.DepthOrArraySize
               && desc1.MipLevels == desc2.MipLevels
               && desc1.Format == desc2.Format
               && desc1.SampleDesc.Count == desc2.SampleDesc.Count;
    }

    public void CopyTexture(Handle<GPUTexture> dst, TextureRegion? dstRegion, Handle<GPUTexture> src, TextureRegion? srcRegion)
    {
        AssertNotDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != Error.None)
        {
            return;
        }
#endif
        if (dst == src)
        {
            return;
        }

        IncrementCommandCount();

        var pDstResource = _resourceDatabase.GetResource(dst.AsResource());
        var pSrcResource = _resourceDatabase.GetResource(src.AsResource());
        if (pSrcResource == null || pDstResource == null)
        {
            RecordError(nameof(CopyTexture), Error.InvalidArgument);
            return;
        }

        if (dstRegion == null || srcRegion == null)
        {
            if (!AreTexturesIdentical(pDstResource, pSrcResource))
            {
                RecordError(nameof(CopyTexture), Error.InvalidArgument);
                return;
            }

            pNativeObject->CopyResource(pDstResource, pSrcResource);
            return;
        }

        var dstRegionV = dstRegion.Value;
        var srcRegionV = srcRegion.Value;

        var dstLocation = GetTextureCopyLocation(pDstResource, dstRegionV.Subresource);
        var srcLocation = GetTextureCopyLocation(pSrcResource, srcRegionV.Subresource);
        var srcBoc = new D3D12_BOX
        {
            left = srcRegionV.X,
            top = srcRegionV.Y,
            front = srcRegionV.Z,
            right = srcRegionV.X + srcRegionV.Width,
            bottom = srcRegionV.Y + srcRegionV.Height,
            back = srcRegionV.Z + srcRegionV.Depth
        };

        pNativeObject->CopyTextureRegion(&dstLocation, dstRegionV.X, dstRegionV.Y, dstRegionV.Z, &srcLocation, &srcBoc);
    }
}
