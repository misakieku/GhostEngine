using Ghost.Core;
using Ghost.Core.Graphics;
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

internal unsafe class D3D12CommandBuffer : D3D12Object<ID3D12GraphicsCommandList7>, ICommandBuffer
{
    private readonly D3D12RenderDevice _device;
    private readonly D3D12PipelineLibrary _pipelineLibrary;
    private readonly D3D12ResourceDatabase _resourceDatabase;
    private readonly D3D12ResourceAllocator _resourceAllocator;
    private readonly D3D12DescriptorAllocator _descriptorAllocator;
    private readonly CommandBufferType _type;

    private CommandBufferState _state;

    public CommandBufferType Type => _type;
    public CommandBufferState State => _state;

    private static ID3D12GraphicsCommandList7* CreateCommandList(D3D12RenderDevice renderDevice, D3D12_COMMAND_LIST_TYPE type)
    {
        var device = renderDevice.NativeObject.Get();
        ID3D12GraphicsCommandList7* pCommandList = default;
        var hr = device->CreateCommandList1(0u, type, D3D12_COMMAND_LIST_FLAG_NONE, __uuidof(pCommandList), (void**)&pCommandList);
        if (hr.FAILED)
        {
            var removedReason = device->GetDeviceRemovedReason();
            Logger.Error($"CreateCommandList1 failed with hr={hr}, GetDeviceRemovedReason={removedReason}");
            renderDevice.DumpInfoQueueMessages();
            ID3D12GraphicsCommandList* pBaseList = default;
            ThrowIfFailed(device->CreateCommandList1(0u, type, D3D12_COMMAND_LIST_FLAG_NONE, __uuidof(pBaseList), (void**)&pBaseList));
            ThrowIfFailed(pBaseList->QueryInterface(__uuidof(pCommandList), (void**)&pCommandList));
            pBaseList->Release();
        }
        return pCommandList;
    }

    public D3D12CommandBuffer(
        D3D12RenderDevice device,
        D3D12PipelineLibrary pipelineLibrary,
        D3D12ResourceDatabase resourceDatabase,
        D3D12ResourceAllocator resourceAllocator,
        D3D12DescriptorAllocator descriptorAllocator,
        CommandBufferType type)
        : base(CreateCommandList(device, D3D12Utility.ToCommandListType(type)))
    {
        _device = device;
        _type = type;

        _pipelineLibrary = pipelineLibrary;
        _resourceDatabase = resourceDatabase;
        _resourceAllocator = resourceAllocator;
        _descriptorAllocator = descriptorAllocator;
    }

    [Conditional("DEBUG")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AssertNotRecording()
    {
        Logger.Assert(!_state.IsRecording);
    }

    [Conditional("DEBUG")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AssertRecording()
    {
        Logger.Assert(_state.IsRecording);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void IncrementCommandCount()
    {
        _state.CommandCount++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RecordError(string cmdName, Error error)
    {
        Logger.Error($"Command '{cmdName}' failed with error: {error}");

        _state.ErrorCommandName = cmdName;
        _state.Error = error;
    }

    public void Begin(ICommandAllocator allocator)
    {
        ThrowIfDisposed();
        AssertNotRecording();

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

        _state.CommandCount = 0;
        _state.IsRecording = true;
    }

    public Result End()
    {
        ThrowIfDisposed();
        AssertRecording();

        var hr = pNativeObject->Close();
        if (hr.FAILED)
        {
            _device.DumpInfoQueueMessages();
            return Result.Failure($"Failed to close command list: {hr}");
        }

        _state.IsRecording = false;

        if (_state.Error.IsFailure)
        {
            return Result.Failure($"Command buffer ended with errors at command {_state.ErrorCommandName}: {_state.Error}");
        }

        return Result.Success();
    }

    public void SetScissorRect(ScissorRectDesc rect)
    {
        AssertNotDisposed();
        AssertRecording();
        IncrementCommandCount();

        var d3d12Rect = new RECT((int)rect.Left, (int)rect.Top, (int)rect.Right, (int)rect.Bottom);
        pNativeObject->RSSetScissorRects(1, &d3d12Rect);
    }

    internal static bool IsTextureBarrierLayoutCompatible(
        CommandBufferType commandBufferType,
        BarrierLayout layoutBefore,
        BarrierLayout layoutAfter)
    {
        static bool IsDirectOnly(BarrierLayout layout)
        {
            return layout is >= BarrierLayout.DirectQueueCommon and <= BarrierLayout.DirectQueueCopyDest
                or BarrierLayout.DirectQueueGenericReadComputeQueueAccessible;
        }

        static bool IsComputeOnly(BarrierLayout layout)
        {
            return layout is >= BarrierLayout.ComputeQueueCommon and <= BarrierLayout.ComputeQueueCopyDest;
        }

        return commandBufferType switch
        {
            CommandBufferType.Graphics => !IsComputeOnly(layoutBefore) && !IsComputeOnly(layoutAfter),
            CommandBufferType.Compute => !IsDirectOnly(layoutBefore)
                && !IsDirectOnly(layoutAfter)
                && layoutBefore is not BarrierLayout.RenderTarget
                and not BarrierLayout.DepthStencilRead
                and not BarrierLayout.DepthStencilWrite
                && layoutAfter is not BarrierLayout.RenderTarget
                and not BarrierLayout.DepthStencilRead
                and not BarrierLayout.DepthStencilWrite,
            _ => true
        };
    }

    private static D3D12_BARRIER_SYNC FilterSyncForQueue(CommandBufferType queueType, BarrierSync sync)
    {
        if (queueType == CommandBufferType.Compute)
        {
            const BarrierSync graphicsOnly = BarrierSync.VertexShading | BarrierSync.PixelShading | BarrierSync.DepthStencil | BarrierSync.RenderTarget | BarrierSync.IndexInput | BarrierSync.Resolve;
            if ((sync & graphicsOnly) != 0)
            {
                var filtered = sync & ~graphicsOnly;
                return (D3D12_BARRIER_SYNC)(filtered == BarrierSync.None ? BarrierSync.AllShading : filtered);
            }
        }
        return (D3D12_BARRIER_SYNC)sync;
    }

    private static bool IsHandoffValid(scoped in BarrierDesc desc)
    {
        return desc.Handoff switch
        {
            BarrierHandoffType.None => true,
            BarrierHandoffType.Release => desc.SyncAfter == BarrierSync.None && desc.AccessAfter == BarrierAccess.NoAccess,
            BarrierHandoffType.Acquire => desc.SyncBefore == BarrierSync.None && desc.AccessBefore == BarrierAccess.NoAccess,
            _ => false
        };
    }

    public void Barrier(params ReadOnlySpan<BarrierDesc> barrierDescs)
    {
        AssertNotDisposed();
        AssertRecording();
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
                        RecordError(nameof(Barrier), r.Error);
                        continue;
                    }

                    ref var record = ref r.Value;
                    if (!record.Allocated)
                    {
                        return;
                    }

                    var beforeSync = desc.SyncBefore;
                    var beforeAccess = desc.IsAliasing ? BarrierAccess.NoAccess : desc.AccessBefore;

                    if (!IsHandoffValid(in desc))
                    {
                        RecordError(nameof(Barrier), Error.InvalidArgument);
                        continue;
                    }

                    if (!desc.Force
                        && beforeSync == desc.SyncAfter
                        && beforeAccess == desc.AccessAfter)
                    {
                        continue;
                    }

                    var syncBefore = FilterSyncForQueue(_type, beforeSync);
                    var accessBefore = syncBefore == D3D12_BARRIER_SYNC.D3D12_BARRIER_SYNC_NONE
                        ? D3D12_BARRIER_ACCESS.D3D12_BARRIER_ACCESS_NO_ACCESS
                        : (D3D12_BARRIER_ACCESS)beforeAccess;

                    var syncAfter = (D3D12_BARRIER_SYNC)desc.SyncAfter;
                    var accessAfter = syncAfter == D3D12_BARRIER_SYNC.D3D12_BARRIER_SYNC_NONE
                        ? D3D12_BARRIER_ACCESS.D3D12_BARRIER_ACCESS_NO_ACCESS
                        : (D3D12_BARRIER_ACCESS)desc.AccessAfter;

                    var resource = record.ResourcePtr;
                    pBufferBarriers[bufferIndex++] = new D3D12_BUFFER_BARRIER
                    {
                        SyncBefore = syncBefore,
                        SyncAfter = syncAfter,
                        AccessBefore = accessBefore,
                        AccessAfter = accessAfter,
                        pResource = resource,
                        Offset = desc.Offset,
                        Size = desc.Size
                    };
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
                    if (!record.Allocated)
                    {
                        return;
                    }

                    var beforeLayout = desc.IsAliasing ? BarrierLayout.Undefined : desc.LayoutBefore;
                    var beforeAccess = desc.IsAliasing ? BarrierAccess.NoAccess : desc.AccessBefore;
                    var beforeSync = desc.SyncBefore;

                    if (!IsHandoffValid(in desc)
                        || !IsTextureBarrierLayoutCompatible(_type, beforeLayout, desc.LayoutAfter))
                    {
                        RecordError(nameof(Barrier), Error.InvalidArgument);
                        continue;
                    }

                    if (!desc.Force
                        && beforeSync == desc.SyncAfter
                        && beforeAccess == desc.AccessAfter
                        && beforeLayout == desc.LayoutAfter)
                    {
                        continue;
                    }

                    var syncBefore = FilterSyncForQueue(_type, beforeSync);
                    var accessBefore = syncBefore == D3D12_BARRIER_SYNC.D3D12_BARRIER_SYNC_NONE
                        ? D3D12_BARRIER_ACCESS.D3D12_BARRIER_ACCESS_NO_ACCESS
                        : (D3D12_BARRIER_ACCESS)beforeAccess;

                    var syncAfter = (D3D12_BARRIER_SYNC)desc.SyncAfter;
                    var accessAfter = syncAfter == D3D12_BARRIER_SYNC.D3D12_BARRIER_SYNC_NONE
                        ? D3D12_BARRIER_ACCESS.D3D12_BARRIER_ACCESS_NO_ACCESS
                        : (D3D12_BARRIER_ACCESS)desc.AccessAfter;

                    var discard = desc.Discard || beforeLayout == BarrierLayout.Undefined;
                    var flags = D3D12_TEXTURE_BARRIER_FLAGS.D3D12_TEXTURE_BARRIER_FLAG_NONE;
                    if (discard)
                    {
                        flags |= D3D12_TEXTURE_BARRIER_FLAGS.D3D12_TEXTURE_BARRIER_FLAG_DISCARD;
                    }

                    var subres = desc.Subresources;
                    var subresourceRange = new D3D12_BARRIER_SUBRESOURCE_RANGE
                    {
                        IndexOrFirstMipLevel = (subres.IndexOrFirstMipLevel == 0 && subres.NumMipLevels == 0 && subres.NumArraySlices == 0)
                            ? 0xFFFFFFFF
                            : subres.IndexOrFirstMipLevel,
                        NumMipLevels = subres.NumMipLevels,
                        FirstArraySlice = subres.FirstArraySlice,
                        NumArraySlices = subres.NumArraySlices,
                        FirstPlane = subres.FirstPlane,
                        NumPlanes = subres.NumPlanes
                    };

                    var resource = record.ResourcePtr;
                    pTextureBarriers[textureIndex++] = new D3D12_TEXTURE_BARRIER
                    {
                        SyncBefore = syncBefore,
                        SyncAfter = syncAfter,
                        AccessBefore = accessBefore,
                        AccessAfter = accessAfter,
                        LayoutBefore = (D3D12_BARRIER_LAYOUT)beforeLayout,
                        LayoutAfter = (D3D12_BARRIER_LAYOUT)desc.LayoutAfter,
                        pResource = resource,
                        Subresources = subresourceRange,
                        Flags = flags
                    };
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

    public void InsertFullPipelineBarrier()
    {
        AssertNotDisposed();
        AssertRecording();
        IncrementCommandCount();

        var globalBarrier = new D3D12_GLOBAL_BARRIER
        {
            SyncBefore = D3D12_BARRIER_SYNC_ALL,
            SyncAfter = D3D12_BARRIER_SYNC_ALL,
            AccessBefore = D3D12_BARRIER_ACCESS_COMMON,
            AccessAfter = D3D12_BARRIER_ACCESS_COMMON
        };

        var barrierGroup = new D3D12_BARRIER_GROUP
        {
            Type = D3D12_BARRIER_TYPE_GLOBAL,
            NumBarriers = 1,
            pGlobalBarriers = &globalBarrier
        };

        pNativeObject->Barrier(1, &barrierGroup);
    }

    public void SetRenderTargets(ReadOnlySpan<Handle<GPUTexture>> renderTargets, Handle<GPUTexture> depthTarget)
    {
        AssertNotDisposed();
        AssertRecording();
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

            ref var record = ref recordResult.Value;
            if (!record.Allocated)
            {
                return;
            }

            var viewGroup = record.viewGroup;
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

            ref var record = ref recordResult.Value;
            if (!record.Allocated)
            {
                return;
            }

            var viewGroup = record.viewGroup;
            pDsvHandle[0] = _descriptorAllocator.GetCpuHandle(viewGroup.dsv);
        }

        pNativeObject->OMSetRenderTargets(rtvCount, pRtvHandles, FALSE, pDsvHandle);
    }

    public void ClearRenderTargetView(Handle<GPUTexture> renderTarget, Color128 clearColor)
    {
        AssertNotDisposed();
        AssertRecording();
        IncrementCommandCount();

        var recordResult = _resourceDatabase.GetResourceRecord(renderTarget.AsResource());
        if (recordResult.Error != Error.None)
        {
            RecordError(nameof(ClearRenderTargetView), recordResult.Error);
            return;
        }

        ref var record = ref recordResult.Value;
        if (!record.Allocated)
        {
            return;
        }

        var cpuHandle = _descriptorAllocator.GetCpuHandle(record.viewGroup.rtv);

        pNativeObject->ClearRenderTargetView(cpuHandle, (float*)&clearColor, 0, null);
    }

    public void ClearDepthStencilView(Handle<GPUTexture> depthStencil, bool inlcudeDepth, bool includeStencil, float clearDepth = 1.0f, byte clearStencil = 0)
    {
        AssertNotDisposed();
        AssertRecording();
        IncrementCommandCount();

        var recordResult = _resourceDatabase.GetResourceRecord(depthStencil.AsResource());
        if (recordResult.Error != Error.None)
        {
            RecordError(nameof(ClearDepthStencilView), recordResult.Error);
            return;
        }

        ref var record = ref recordResult.Value;
        if (!record.Allocated)
        {
            return;
        }

        var cpuHandle = _descriptorAllocator.GetCpuHandle(record.viewGroup.dsv);
        var flag = (inlcudeDepth ? D3D12_CLEAR_FLAG_DEPTH : 0) | (includeStencil ? D3D12_CLEAR_FLAG_STENCIL : 0);

        pNativeObject->ClearDepthStencilView(cpuHandle,
            flag,
            clearDepth,
            clearStencil,
            0,
            null);
    }

    public void BeginRenderPass(ReadOnlySpan<PassRenderTargetDesc> rtDescs, ref readonly PassDepthStencilDesc depthDesc, bool allowUAVWrites = false)
    {
        AssertNotDisposed();
        AssertRecording();
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
            if (!record.Allocated)
            {
                return;
            }

            var cpuHandle = _descriptorAllocator.GetCpuHandle(record.viewGroup.rtv);
            var format = record.desc.TextureDescriptor.Format.ToDXGIFormat();
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

        var hasStencil = false;
        var pDsvDesc = stackalloc D3D12_RENDER_PASS_DEPTH_STENCIL_DESC[depthDesc.Texture.IsValid ? 1 : 0];
        if (depthDesc.Texture.IsValid)
        {
            var recordResult = _resourceDatabase.GetResourceRecord(depthDesc.Texture.AsResource());
            if (recordResult.Error != Error.None)
            {
                RecordError(nameof(BeginRenderPass), recordResult.Error);
                return;
            }

            ref var record = ref recordResult.Value;
            if (!record.Allocated)
            {
                return;
            }

            var cpuHandle = _descriptorAllocator.GetCpuHandle(record.viewGroup.dsv);
            var format = record.desc.TextureDescriptor.Format.ToDXGIFormat();
            hasStencil = format is DXGI_FORMAT_D24_UNORM_S8_UINT or DXGI_FORMAT_D32_FLOAT_S8X24_UINT;

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
            var stencilLoadAccessType = !hasStencil
                ? D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_NO_ACCESS
                : depthDesc.StencilLoadOp switch
                {
                    AttachmentLoadOp.Load => D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_PRESERVE,
                    AttachmentLoadOp.Clear => D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_CLEAR,
                    AttachmentLoadOp.DontCare => D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_DISCARD,
                    AttachmentLoadOp.NoAccess => D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_NO_ACCESS,
                    _ => D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_PRESERVE
                };

            // Map stencil store operation
            var stencilStoreAccessType = !hasStencil
                ? D3D12_RENDER_PASS_ENDING_ACCESS_TYPE_NO_ACCESS
                : depthDesc.StencilStoreOp switch
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

        var passFlags = allowUAVWrites ? D3D12_RENDER_PASS_FLAG_ALLOW_UAV_WRITES : D3D12_RENDER_PASS_FLAG_NONE;
        if (depthDesc.Texture.IsValid)
        {
            if (depthDesc.DepthStoreOp == AttachmentStoreOp.NoAccess)
            {
                passFlags |= D3D12_RENDER_PASS_FLAG_BIND_READ_ONLY_DEPTH;
            }
            if (hasStencil && depthDesc.StencilStoreOp == AttachmentStoreOp.NoAccess)
            {
                passFlags |= D3D12_RENDER_PASS_FLAG_BIND_READ_ONLY_STENCIL;
            }
        }

        pNativeObject->BeginRenderPass((uint)rtDescs.Length, pRtvDescs, depthDesc.Texture.IsValid ? pDsvDesc : null, passFlags);
    }

    public void EndRenderPass()
    {
        AssertNotDisposed();
        AssertRecording();
        IncrementCommandCount();

        pNativeObject->EndRenderPass();
    }

    public void SetViewport(ViewportDesc viewport)
    {
        AssertNotDisposed();
        AssertRecording();
        IncrementCommandCount();

        var d3d12Viewport = new D3D12_VIEWPORT(viewport.X, viewport.Y, viewport.Width, viewport.Height, viewport.MinDepth, viewport.MaxDepth);
        pNativeObject->RSSetViewports(1, &d3d12Viewport);
    }

    public void SetPipelineState(Key128<PipelineState> pipelineKey)
    {
        AssertNotDisposed();
        AssertRecording();
        IncrementCommandCount();

        var psor = _pipelineLibrary.GetPipelineStateObject(pipelineKey);
        if (psor.Error != Error.None)
        {
            RecordError(nameof(SetPipelineState), psor.Error);
            return;
        }

        if (_type == CommandBufferType.Compute)
        {
            pNativeObject->SetComputeRootSignature(_pipelineLibrary.DefaultRootSignature);
        }
        else
        {
            pNativeObject->SetGraphicsRootSignature(_pipelineLibrary.DefaultRootSignature);
        }

        pNativeObject->SetPipelineState(psor.Value);
    }

    public void SetConstantBufferView(uint slot, Handle<GPUBuffer> buffer)
    {
        AssertNotDisposed();
        AssertRecording();
        IncrementCommandCount();

        var resource = _resourceDatabase.GetResource(buffer.AsResource());
        if (resource == null)
        {
            return;
        }

        pNativeObject->SetGraphicsRootConstantBufferView(slot, resource.Get()->GetGPUVirtualAddress());
    }

    public void SetVertexBuffer(uint slot, Handle<GPUBuffer> buffer, ulong offset = 0)
    {
        AssertNotDisposed();
        AssertRecording();
        IncrementCommandCount();

        var recordResult = _resourceDatabase.GetResourceRecord(buffer.AsResource());
        if (recordResult.Error != Error.None)
        {
            RecordError(nameof(BeginRenderPass), recordResult.Error);
            return;
        }

        ref var record = ref recordResult.Value;
        if (!record.Allocated)
        {
            return;
        }

        var vbView = new D3D12_VERTEX_BUFFER_VIEW
        {
            BufferLocation = record.ResourcePtr.Get()->GetGPUVirtualAddress() + offset,
            SizeInBytes = (uint)(record.ResourcePtr.Get()->GetDesc().Width - offset),
            StrideInBytes = record.desc.BufferDescriptor.Stride
        };

        pNativeObject->IASetVertexBuffers(slot, 1, &vbView);
    }

    public void SetIndexBuffer(Handle<GPUBuffer> buffer, IndexType type, ulong offset = 0)
    {
        AssertNotDisposed();
        AssertRecording();
        IncrementCommandCount();

        var resource = _resourceDatabase.GetResource(buffer.AsResource());
        if (resource == null)
        {
            return;
        }

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
        AssertRecording();
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
        AssertRecording();
        IncrementCommandCount();

        fixed (uint* pConstants = constantBuffer)
        {
            pNativeObject->SetGraphicsRoot32BitConstants(rootIndex, (uint)constantBuffer.Length, pConstants, offsetIn32Bits);
        }
    }

    public void SetComputeRoot32Constants(uint rootIndex, ReadOnlySpan<uint> constantBuffer, uint offsetIn32Bits = 0)
    {
        AssertNotDisposed();
        AssertRecording();
        IncrementCommandCount();

        fixed (uint* pConstants = constantBuffer)
        {
            pNativeObject->SetComputeRoot32BitConstants(rootIndex, (uint)constantBuffer.Length, pConstants, offsetIn32Bits);
        }
    }

    public void Draw(uint vertexCount, uint instanceCount = 1, uint startVertex = 0, uint startInstance = 0)
    {
        AssertNotDisposed();
        AssertRecording();
        IncrementCommandCount();

        pNativeObject->DrawInstanced(vertexCount, instanceCount, startVertex, startInstance);
    }

    public void SetProgram(scoped in SetProgramDesc desc)
    {
        // TODO
    }

    public void DrawIndexed(uint indexCount, uint instanceCount = 1, uint startIndex = 0, int baseVertex = 0, uint startInstance = 0)
    {
        AssertNotDisposed();
        AssertRecording();
        IncrementCommandCount();

        pNativeObject->DrawIndexedInstanced(indexCount, instanceCount, startIndex, baseVertex, startInstance);
    }

    public void DispatchCompute(uint threadGroupCountX, uint threadGroupCountY, uint threadGroupCountZ)
    {
        AssertNotDisposed();
        AssertRecording();
        IncrementCommandCount();

        pNativeObject->Dispatch(threadGroupCountX, threadGroupCountY, threadGroupCountZ);
    }

    public void DispatchMesh(uint threadGroupCountX, uint threadGroupCountY, uint threadGroupCountZ)
    {
        AssertNotDisposed();
        AssertRecording();
        IncrementCommandCount();

        pNativeObject->DispatchMesh(threadGroupCountX, threadGroupCountY, threadGroupCountZ);
    }

    public void DispatchRay()
    {
        throw new NotImplementedException();
    }

    public void DispatchGraph(scoped in DispatchGraphDesc desc)
    {
        throw new NotImplementedException();
    }

    public void ExecuteIndirect(ICommandSignature commandSignature, Handle<GPUBuffer> argumentBuffer, ulong argumentOffset, Handle<GPUBuffer> countBuffer, ulong countBufferOffset)
    {
        AssertNotDisposed();
        AssertRecording();
        IncrementCommandCount();

        Logger.DebugAssert(commandSignature is D3D12CommandSignature);

        var resource = _resourceDatabase.GetResource(argumentBuffer.AsResource());
        var countResource = _resourceDatabase.GetResource(countBuffer.AsResource());

        pNativeObject->ExecuteIndirect((ID3D12CommandSignature*)commandSignature.NativePointer, 0,
            resource, argumentOffset, countResource, countBufferOffset);
    }

    public void CopyBuffer(Handle<GPUBuffer> dst, Handle<GPUBuffer> src, ulong dstOffset = 0, ulong srcOffset = 0, ulong numBytes = 0)
    {
        AssertNotDisposed();
        AssertRecording();

        if (dst == src)
        {
            return;
        }

        IncrementCommandCount();

        var pDstResource = _resourceDatabase.GetResource(dst.AsResource());
        var pSrcResource = _resourceDatabase.GetResource(src.AsResource());
        if (pSrcResource == null || pDstResource == null)
        {
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
        AssertRecording();
        IncrementCommandCount();

        var d3d12Resource = _resourceDatabase.GetResource(resource);
        var d3d12Intermediate = _resourceDatabase.GetResource(intermediate);
        if (d3d12Intermediate == null || d3d12Resource == null)
        {
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
        AssertRecording();

        if (dst == src)
        {
            return;
        }

        IncrementCommandCount();

        var pDstResource = _resourceDatabase.GetResource(dst.AsResource());
        var pSrcResource = _resourceDatabase.GetResource(src.AsResource());
        if (pSrcResource == null || pDstResource == null)
        {
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
