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
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    private static void RecordError(string cmdName, Error status)
#else
    private void RecordError(string cmdName, Error status)
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
            heaps[0] = _descriptorAllocator.GetCbvSrvUavHeap(); // Bindless resource Heap
            heaps[1] = _descriptorAllocator.GetSamplerHeap();   // Bindless sampler Heap
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
        if (_lastError.Status != Error.None)
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
        if (_lastError.Status != Error.None)
        {
            return;
        }
#endif
        IncrementCommandCount();

        var d3d12Rect = new RECT((int)rect.Left, (int)rect.Top, (int)rect.Right, (int)rect.Bottom);
        _commandList.Get()->RSSetScissorRects(1, &d3d12Rect);
    }

    public void ResourceBarrier(params ReadOnlySpan<BarrierDesc> barrierDescs)
    {
        ThrowIfDisposed();
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
                    pGlobalBarriers[globalIndex++] = new D3D12_GLOBAL_BARRIER
                    {
                        SyncBefore = (D3D12_BARRIER_SYNC)desc.SyncBefore,
                        SyncAfter = (D3D12_BARRIER_SYNC)desc.SyncAfter,
                        AccessBefore = (D3D12_BARRIER_ACCESS)desc.AccessBefore,
                        AccessAfter = (D3D12_BARRIER_ACCESS)desc.AccessAfter
                    };
                    break;
                case BarrierType.Buffer:
                {
                    var r = _resourceDatabase.GetResourceRecord(desc.Resource);
                    if (r.IsFailure)
                    {
                        RecordError(nameof(ResourceBarrier), r.Error);
                        continue;
                    }

                    ref var record = ref r.Value;
                    var resource = record.ResourcePtr;
                    pBufferBarriers[bufferIndex++] = new D3D12_BUFFER_BARRIER
                    {
                        SyncBefore = (D3D12_BARRIER_SYNC)desc.SyncBefore,
                        SyncAfter = (D3D12_BARRIER_SYNC)desc.SyncAfter,
                        AccessBefore = (D3D12_BARRIER_ACCESS)desc.AccessBefore,
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
                        RecordError(nameof(ResourceBarrier), r.Error);
                        continue;
                    }

                    ref var record = ref r.Value;
                    var resource = record.ResourcePtr;
                    pTextureBarriers[textureIndex++] = new D3D12_TEXTURE_BARRIER
                    {
                        SyncBefore = (D3D12_BARRIER_SYNC)desc.SyncBefore,
                        SyncAfter = (D3D12_BARRIER_SYNC)desc.SyncAfter,
                        AccessBefore = (D3D12_BARRIER_ACCESS)desc.AccessBefore,
                        AccessAfter = (D3D12_BARRIER_ACCESS)desc.AccessAfter,
                        LayoutBefore = (D3D12_BARRIER_LAYOUT)desc.LayoutBefore,
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

        if (globalCount > 0)
        {
            groups[groupCount] = new D3D12_BARRIER_GROUP
            {
                Type = D3D12_BARRIER_TYPE.D3D12_BARRIER_TYPE_GLOBAL,
                NumBarriers = (uint)globalCount,
            };
            groups[groupCount].Anonymous.pGlobalBarriers = pGlobalBarriers;
            groupCount++;
        }

        if (bufferCount > 0)
        {
            groups[groupCount] = new D3D12_BARRIER_GROUP
            {
                Type = D3D12_BARRIER_TYPE.D3D12_BARRIER_TYPE_BUFFER,
                NumBarriers = (uint)bufferCount,
            };
            groups[groupCount].Anonymous.pBufferBarriers = pBufferBarriers;
            groupCount++;
        }

        if (textureCount > 0)
        {
            groups[groupCount] = new D3D12_BARRIER_GROUP
            {
                Type = D3D12_BARRIER_TYPE.D3D12_BARRIER_TYPE_TEXTURE,
                NumBarriers = (uint)textureCount,
            };
            groups[groupCount].Anonymous.pTextureBarriers = pTextureBarriers;
            groupCount++;
        }

        _commandList.Get()->Barrier(groupCount, groups);
    }

    public void SetRenderTargets(ReadOnlySpan<Handle<Texture>> renderTargets, Handle<Texture> depthTarget)
    {
        ThrowIfDisposed();
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

        _commandList.Get()->OMSetRenderTargets(rtvCount, pRtvHandles, FALSE, pDsvHandle);
    }

    public void ClearRenderTargetView(Handle<Texture> renderTarget, Color128 clearColor)
    {
        ThrowIfDisposed();
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
        var color = stackalloc float[4]
        {
            clearColor.r,
            clearColor.g,
            clearColor.b,
            clearColor.a
        };

        _commandList.Get()->ClearRenderTargetView(cpuHandle, color, 0, null);
    }

    public void ClearDepthStencilView(Handle<Texture> depthStencil, bool inlcudeDepth, bool includeStencil, float clearDepth = 1.0f, byte clearStencil = 0)
    {
        ThrowIfDisposed();
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

        _commandList.Get()->ClearDepthStencilView(cpuHandle,
            flag,
            clearDepth,
            clearStencil,
            0,
            null);
    }

    public void BeginRenderPass(ReadOnlySpan<PassRenderTargetDesc> rtDescs, PassDepthStencilDesc depthDesc, bool allowUAVWrites = false)
    {
        ThrowIfDisposed();
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
                _ => D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_PRESERVE
            };

            // Map depth store operation
            var depthStoreAccessType = depthDesc.DepthStoreOp switch
            {
                AttachmentStoreOp.Store => D3D12_RENDER_PASS_ENDING_ACCESS_TYPE_PRESERVE,
                AttachmentStoreOp.DontCare => D3D12_RENDER_PASS_ENDING_ACCESS_TYPE_DISCARD,
                _ => D3D12_RENDER_PASS_ENDING_ACCESS_TYPE_PRESERVE
            };

            // Map stencil load operation
            var stencilLoadAccessType = depthDesc.StencilLoadOp switch
            {
                AttachmentLoadOp.Load => D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_PRESERVE,
                AttachmentLoadOp.Clear => D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_CLEAR,
                AttachmentLoadOp.DontCare => D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_DISCARD,
                _ => D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_PRESERVE
            };

            // Map stencil store operation
            var stencilStoreAccessType = depthDesc.StencilStoreOp switch
            {
                AttachmentStoreOp.Store => D3D12_RENDER_PASS_ENDING_ACCESS_TYPE_PRESERVE,
                AttachmentStoreOp.DontCare => D3D12_RENDER_PASS_ENDING_ACCESS_TYPE_DISCARD,
                _ => D3D12_RENDER_PASS_ENDING_ACCESS_TYPE_PRESERVE
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

        _commandList.Get()->BeginRenderPass((uint)rtDescs.Length, pRtvDescs, pDsvDesc,
                allowUAVWrites ? D3D12_RENDER_PASS_FLAG_ALLOW_UAV_WRITES : D3D12_RENDER_PASS_FLAG_NONE);
    }

    public void EndRenderPass()
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != Error.None)
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
        if (_lastError.Status != Error.None)
        {
            return;
        }
#endif
        IncrementCommandCount();

        var d3d12Viewport = new D3D12_VIEWPORT(viewport.X, viewport.Y, viewport.Width, viewport.Height, viewport.MinDepth, viewport.MaxDepth);
        _commandList.Get()->RSSetViewports(1, &d3d12Viewport);
    }

    public void SetPipelineState(Key128<GraphicsPipeline> pipelineKey)
    {
        ThrowIfDisposed();
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

        _commandList.Get()->SetGraphicsRootSignature(_pipelineLibrary.DefaultRootSignature);
        _commandList.Get()->SetPipelineState(psor.Value);
    }

    public void SetConstantBufferView(uint slot, Handle<GraphicsBuffer> buffer)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != Error.None)
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

        _commandList.Get()->IASetVertexBuffers(slot, 1, &vbView);
    }

    public void SetIndexBuffer(Handle<GraphicsBuffer> buffer, IndexType type, ulong offset = 0)
    {
        ThrowIfDisposed();
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

        _commandList.Get()->IASetIndexBuffer(&ibView);
    }

    public void SetPrimitiveTopology(PrimitiveTopology topology)
    {
        ThrowIfDisposed();
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

        _commandList.Get()->IASetPrimitiveTopology(d3d12Topology);
    }

    public void SetGraphicsRoot32Constants(uint rootIndex, ReadOnlySpan<uint> constantBuffer, uint offsetIn32Bits = 0)
    {
        ThrowIfDisposed();
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
            _commandList.Get()->SetGraphicsRoot32BitConstants(rootIndex, (uint)constantBuffer.Length, pConstants, offsetIn32Bits);
        }
    }

    public void Draw(uint vertexCount, uint instanceCount = 1, uint startVertex = 0, uint startInstance = 0)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != Error.None)
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
        if (_lastError.Status != Error.None)
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
        if (_lastError.Status != Error.None)
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
        if (_lastError.Status != Error.None)
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

    public void DispatchGraph()
    {
        throw new NotImplementedException();
    }

    public void ExecuteIndirect(Handle<GraphicsBuffer> argumentBuffer, ulong argumentOffset, Handle<GraphicsBuffer> countBuffer, ulong countBufferOffset)
    {
        throw new NotImplementedException();

        ThrowIfDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != Error.None)
        {
            return;
        }
#endif

        IncrementCommandCount();
        var resource = _resourceDatabase.GetResource(argumentBuffer.AsResource());
        var countResource = _resourceDatabase.GetResource(countBuffer.AsResource());
        _commandList.Get()->ExecuteIndirect(null, 0,
            resource, argumentOffset, countResource, countBufferOffset);

    }

    public void UploadBuffer<T>(Handle<GraphicsBuffer> buffer, params ReadOnlySpan<T> data)
        where T : unmanaged
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != Error.None)
        {
            return;
        }
#endif
        IncrementCommandCount();

        var sizeInBytes = (uint)(data.Length * sizeof(T));

        var uploadHandle = _resourceAllocator.CreateTempUploadBuffer(sizeInBytes, out var offset);
        var uploadResource = _resourceDatabase.GetResource(uploadHandle.AsResource());

        void* pMappedData;
        uploadResource.Get()->Map(0, null, &pMappedData);
        fixed (T* pData = data)
        {
            MemoryUtility.MemCpy((byte*)pMappedData + offset, pData, sizeInBytes);
        }
        uploadResource.Get()->Unmap(0, null);

        var pResource = _resourceDatabase.GetResource(buffer.AsResource());
        _commandList.Get()->CopyBufferRegion(pResource, 0, uploadResource, offset, sizeInBytes);
    }

    public void UploadTexture(Handle<Texture> texture, params ReadOnlySpan<SubResourceData> subresources)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != Error.None)
        {
            return;
        }
#endif
        IncrementCommandCount();

        var resource = _resourceDatabase.GetResource(texture.AsResource());

        var resourceDesc = resource.Get()->GetDesc();
        var requiredSize = GetRequiredIntermediateSize(resource, 0, (uint)subresources.Length);

        var uploadHandle = _resourceAllocator.CreateTempUploadBuffer(requiredSize, out var offset);
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
            offset,
            0,
            (uint)subresources.Length,
            d3d12Subresources);
    }

    public void CopyBuffer(Handle<GraphicsBuffer> dest, Handle<GraphicsBuffer> src, ulong destOffset = 0, ulong srcOffset = 0, ulong numBytes = 0)
    {
        ThrowIfDisposed();
        ThrowIfNotRecording();
#if !DEBUG
        if (_lastError.Status != Error.None)
        {
            return;
        }
#endif
        IncrementCommandCount();

        var pDestResource = _resourceDatabase.GetResource(dest.AsResource());
        var pSrcResource = _resourceDatabase.GetResource(src.AsResource());
        if (pSrcResource == null || pDestResource == null)
        {
            RecordError(nameof(CopyBuffer), Error.InvalidArgument);
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
