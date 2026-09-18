using Ghost.Core;
using Ghost.Graphics.D3D12.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop.DirectX;
using static TerraFX.Aliases.D3D12_Alias;
using static TerraFX.Aliases.DXGI_Alias;

namespace Ghost.Graphics.D3D12;

internal unsafe class D3D12ResourceDatabase : IResourceDatabase
{
    internal struct ResourceRecord
    {
        [StructLayout(LayoutKind.Explicit)]
        public struct __resource_union
        {
            [FieldOffset(0)]
            public UniquePtr<D3D12MA_Allocation> allocation;
            [FieldOffset(0)]
            public UniquePtr<ID3D12Resource> resource;

            public __resource_union(D3D12MA_Allocation* allocation)
            {
                this.allocation = allocation;
            }

            public __resource_union(ID3D12Resource* resource)
            {
                this.resource = resource;
            }
        }

        public ResourceDesc desc;
        public ResourceViewGroup viewGroup;
        public __resource_union resource;
        public UnsafeArray<ResourceViewGroup> subResourceView;

        public bool isExternal;
        public bool isShared;

        public readonly bool Allocated => isExternal ? resource.resource.Get() != null : resource.allocation.Get() != null;
        public readonly SharedPtr<ID3D12Resource> ResourcePtr => isExternal ? resource.resource.Get() : resource.allocation.Get()->GetResource();

        public ResourceRecord(D3D12MA_Allocation* allocation, ResourceViewGroup viewGroup, ResourceDesc desc, UnsafeArray<ResourceViewGroup> subResourceView = default)
        {
            this.resource = new __resource_union(allocation);
            this.isExternal = false;
            this.isShared = false;

            this.viewGroup = viewGroup;
            this.desc = desc;
            this.subResourceView = subResourceView;
        }

        public ResourceRecord(ID3D12Resource* resource, ResourceViewGroup viewGroup, ResourceDesc desc, UnsafeArray<ResourceViewGroup> subResourceView = default)
        {
            this.resource = new __resource_union(resource);
            this.isExternal = true;
            this.isShared = false;

            this.viewGroup = viewGroup;
            this.desc = desc;
            this.subResourceView = subResourceView;
        }
        public readonly uint Release(D3D12DescriptorAllocator descriptorAllocator)
        {
            if (isShared)
            {
                return 0;
            }

            var refCount = 0u;
            if (Allocated)
            {
                if (isExternal)
                {
                    refCount = resource.resource.Get()->Release();
                }
                else
                {
                    refCount = resource.allocation.Get()->Release();
                }
            }

            descriptorAllocator.Release(viewGroup);

            if (subResourceView.IsCreated)
            {
                for (var i = 1; i < subResourceView.Length; i++)
                {
                    descriptorAllocator.Release(subResourceView[i]);
                }

                subResourceView.Dispose();
            }

            return refCount;
        }
    }

    private readonly struct ReleaseEntry
    {
        public readonly ResourceRecord record;
        public readonly ulong fenceValue;

        public ReleaseEntry(ResourceRecord record, ulong fenceValue)
        {
            this.record = record;
            this.fenceValue = fenceValue;
        }
    }
    private readonly struct PendingSwapEntry
    {
        public readonly Handle<GPUResource> a;
        public readonly Handle<GPUResource> b;
        public readonly bool isReplace;
        public readonly ulong cpuFrame;

        public PendingSwapEntry(Handle<GPUResource> a, Handle<GPUResource> b, bool isReplace, ulong cpuFrame)
        {
            this.a = a;
            this.b = b;
            this.isReplace = isReplace;
            this.cpuFrame = cpuFrame;
        }
    }

    private readonly D3D12RenderDevice _device;
    private readonly D3D12DescriptorAllocator _descriptorAllocator;

    private UnsafeSlotMap<ResourceRecord> _resources;
    private UnsafeHashMap<SamplerDesc, Identifier<Sampler>> _samplers;
#if GHOST_SAFETY_CHECKS
    private readonly Dictionary<Handle<GPUResource>, string> _resourceName;
#endif

    private UnsafeQueue<ReleaseEntry> _releaseQueue;
    private UnsafeQueue<PendingSwapEntry> _pendingSwaps;

    private readonly Lock _writeLock;

    private ulong _cpuFrame;
    private bool _disposed;

    public D3D12ResourceDatabase(D3D12RenderDevice device, D3D12DescriptorAllocator descriptorAllocator)
    {
        _device = device;
        _descriptorAllocator = descriptorAllocator;

        _resources = new UnsafeSlotMap<ResourceRecord>(64, AllocationHandle.Persistent, AllocationOption.Clear);
        _samplers = new UnsafeHashMap<SamplerDesc, Identifier<Sampler>>(32, AllocationHandle.Persistent);
#if GHOST_SAFETY_CHECKS
        _resourceName = new Dictionary<Handle<GPUResource>, string>(64);
#endif

        _releaseQueue = new UnsafeQueue<ReleaseEntry>(32, AllocationHandle.Persistent);
        _pendingSwaps = new UnsafeQueue<PendingSwapEntry>(32, AllocationHandle.Persistent);
        _writeLock = new Lock();
    }

    ~D3D12ResourceDatabase()
    {
        Dispose();
    }

    internal Handle<GPUResource> ImportExternalResource(ID3D12Resource* pResource, ResourceViewGroup viewGroup, ResourceDesc desc, string? name = null, UnsafeArray<ResourceViewGroup> subResourceViews = default)
    {
        Logger.DebugAssert(!_disposed);

        if (pResource == null)
        {
#if DEBUG
            Debugger.Break();
#endif
            return Handle<GPUResource>.Invalid;
        }

        // It's fine here to use lock. System.Threading.Lock use LowLevelSpinWaiter internally before it escalates to a kernel lock, so it should be very cheap in the uncontended case.
        // And adding resources is not a very frequent operation, so we can afford the potential overhead here for the sake of simplicity and correctness.
        // We do not choose a concurrent collection here because we want maximum access speed for read operations.
        lock (_writeLock)
        {
            var id = _resources.Add(new ResourceRecord(pResource, viewGroup, desc, subResourceViews), out var generation);
            var handle = new Handle<GPUResource>(id, generation);

#if GHOST_SAFETY_CHECKS
            if (!string.IsNullOrEmpty(name))
            {
                pResource->SetName(name);
                _resourceName[handle] = name;
            }
#endif

            return handle;
        }
    }

    internal Handle<GPUResource> AddAllocation(D3D12MA_Allocation* allocation, ResourceViewGroup resourceDescriptor, ResourceDesc desc, string? name = null, UnsafeArray<ResourceViewGroup> subResourceViews = default)
    {
        Logger.DebugAssert(!_disposed);

        if (allocation == null)
        {
#if DEBUG
            Debugger.Break();
#endif
            return Handle<GPUResource>.Invalid;
        }

        lock (_writeLock)
        {
            var id = _resources.Add(new ResourceRecord(allocation, resourceDescriptor, desc, subResourceViews), out var generation);
            var handle = new Handle<GPUResource>(id, generation);

#if GHOST_SAFETY_CHECKS
            if (!string.IsNullOrEmpty(name))
            {
                allocation->SetName(name);
                var pResource = allocation->GetResource();
                if (pResource != null)
                {
                    pResource->SetName(name);
                }
                _resourceName[handle] = name;
            }
#endif

            return handle;
        }
    }

    public bool HasResource(Handle<GPUResource> handle)
    {
        Logger.DebugAssert(!_disposed);
        return _resources.Contains(handle.ID, handle.Generation);
    }

    public RefResult<ResourceRecord, Error> GetResourceRecord(Handle<GPUResource> handle)
    {
        Logger.DebugAssert(!_disposed);

        ref var info = ref _resources.GetElementReferenceAt(handle.ID, handle.Generation, out var exist);
        if (!exist)
        {
            return Error.NotFound;
        }

        return RefResult<ResourceRecord, Error>.Success(ref info);
    }

    public SharedPtr<ID3D12Resource> GetResource(Handle<GPUResource> handle)
    {
        var r = GetResourceRecord(handle);
        if (r.IsFailure)
        {
            return null;
        }

        return r.Value.ResourcePtr;
    }

    public Result<ResourceDesc, Error> GetResourceDescription(Handle<GPUResource> handle)
    {
        var r = GetResourceRecord(handle);
        if (r.IsFailure)
        {
            return r.Error;
        }

        return r.Value.desc;
    }

    public uint GetBindlessIndex(Handle<GPUResource> handle, BindlessAccess access = BindlessAccess.ShaderResource, uint subResource = IResourceDatabase.AllSubresources)
    {
        var r = GetResourceRecord(handle);
        if (r.IsFailure || !r.Value.Allocated)
        {
            return uint.MaxValue;
        }

        ref readonly var record = ref r.Value;

        if (subResource == IResourceDatabase.AllSubresources || !record.subResourceView.IsCreated)
        {
            return access switch
            {
                BindlessAccess.ShaderResource => (uint)record.viewGroup.srv.Value,
                BindlessAccess.ConstantBuffer => (uint)record.viewGroup.cbv.Value,
                BindlessAccess.UnorderedAccess => (uint)record.viewGroup.uav.Value,
                _ => uint.MaxValue,
            };
        }

        if (subResource < (uint)record.subResourceView.Length)
        {
            ref readonly var subView = ref record.subResourceView[(int)subResource];
            return access switch
            {
                BindlessAccess.ShaderResource => subView.srv.IsValid ? (uint)subView.srv.Value : (uint)record.viewGroup.srv.Value,
                BindlessAccess.ConstantBuffer => (uint)subView.cbv.Value,
                BindlessAccess.UnorderedAccess => subView.uav.IsValid ? (uint)subView.uav.Value : (uint)record.viewGroup.uav.Value,
                _ => uint.MaxValue,
            };
        }

        return uint.MaxValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetBindlessIndices(Handle<GPUResource> handle, ReadOnlySpan<uint> subResources, Span<uint> outIndices, BindlessAccess access = BindlessAccess.ShaderResource)
    {
        var r = GetResourceRecord(handle);
        if (r.IsFailure || !r.Value.Allocated)
        {
            outIndices.Fill(uint.MaxValue);
            return;
        }

        ref readonly var record = ref r.Value;
#if GHOST_SAFETY_CHECKS
        if (!record.subResourceView.IsCreated)
        {
            var name = _resourceName[handle];
            throw new InvalidOperationException($"Resource {name} does not have sub resource.");
        }
#endif

        var count = Math.Min(subResources.Length, outIndices.Length);
        for (var i = 0; i < count; i++)
        {
            var sub = subResources[i];
            if (sub == IResourceDatabase.AllSubresources)
            {
                outIndices[i] = access switch
                {
                    BindlessAccess.ShaderResource => (uint)record.viewGroup.srv.Value,
                    BindlessAccess.ConstantBuffer => (uint)record.viewGroup.cbv.Value,
                    BindlessAccess.UnorderedAccess => (uint)record.viewGroup.uav.Value,
                    _ => uint.MaxValue,
                };
            }
            else if (sub >= (uint)record.subResourceView.Length)
            {
                outIndices[i] = uint.MaxValue;
            }
            else
            {
                ref readonly var subView = ref record.subResourceView[(int)sub];
                outIndices[i] = access switch
                {
                    BindlessAccess.ShaderResource => subView.srv.IsValid ? (uint)subView.srv.Value : (uint)record.viewGroup.srv.Value,
                    BindlessAccess.ConstantBuffer => (uint)subView.cbv.Value,
                    BindlessAccess.UnorderedAccess => subView.uav.IsValid ? (uint)subView.uav.Value : (uint)record.viewGroup.uav.Value,
                    _ => uint.MaxValue,
                };
            }
        }
    }

    public uint AllocateRawBufferSRV(Handle<GPUBuffer> buffer, uint offsetInBytes, uint sizeInBytes)
    {
        var r = GetResourceRecord(buffer.AsResource());
        if (r.IsFailure || !r.Value.Allocated)
        {
            return uint.MaxValue;
        }

        var pResource = r.Value.ResourcePtr.Get();
        var srvDesc = new D3D12_SHADER_RESOURCE_VIEW_DESC
        {
            ViewDimension = D3D12_SRV_DIMENSION_BUFFER,
            Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING,
            Format = DXGI_FORMAT_R32_TYPELESS,
        };
        srvDesc.Buffer.FirstElement = offsetInBytes / 4u;
        srvDesc.Buffer.NumElements = (sizeInBytes + 3u) / 4u;
        srvDesc.Buffer.StructureByteStride = 0;
        srvDesc.Buffer.Flags = D3D12_BUFFER_SRV_FLAG_RAW;

        var descriptor = _descriptorAllocator.AllocateCbvSrvUav();
        var cpuHandle = _descriptorAllocator.GetCpuHandle(descriptor);
        _device.NativeObject.Get()->CreateShaderResourceView(pResource, &srvDesc, cpuHandle);

        return (uint)descriptor.Value;
    }

    public void ReleaseRawBufferSRV(uint descriptorIndex)
    {
        _descriptorAllocator.Release(new Identifier<CbvSrvUavDescriptor>((int)descriptorIndex));
    }

    public string? GetResourceName(Handle<GPUResource> handle)
    {
        Logger.DebugAssert(!_disposed);

#if GHOST_SAFETY_CHECKS
        if (_resourceName.TryGetValue(handle, out var name))
        {
            return name;
        }
#endif
        return null;
    }

    public void ReleaseResource(Handle<GPUResource> handle)
    {
        Logger.DebugAssert(!_disposed);

        lock (_writeLock)
        {
            if (!_resources.TryGetElementAt(handle.ID, handle.Generation, out var record))
            {
                return;
            }

            var entry = new ReleaseEntry(record, _cpuFrame);

            _releaseQueue.Enqueue(entry);
            _resources.Remove(handle.ID, handle.Generation);

#if GHOST_SAFETY_CHECKS
            _resourceName.Remove(handle, out _);
#endif
        }
    }

    public void ReleaseResourceImmediately(Handle<GPUResource> handle)
    {
        Logger.DebugAssert(!_disposed);

        lock (_writeLock)
        {
            ref var info = ref _resources.GetElementReferenceAt(handle.ID, handle.Generation, out var exist);
            if (!exist || !info.Allocated)
            {
                return;
            }

            info.Release(_descriptorAllocator);
            _resources.Remove(handle.ID, handle.Generation);
        }
    }

    public Identifier<Sampler> AddSampler(scoped in SamplerDesc desc, int id)
    {
        Logger.DebugAssert(!_disposed);

        if (_samplers.ContainsKey(desc))
        {
            throw new InvalidOperationException("Sampler already exists.");
        }

        var identifier = new Identifier<Sampler>(id);
        _samplers.Add(desc, identifier);

        return identifier;
    }

    public bool TryGetSampler(scoped in SamplerDesc desc, out Identifier<Sampler> id)
    {
        Logger.DebugAssert(!_disposed);
        return _samplers.TryGetValue(desc, out id);
    }

    public void ReleaseSampler(Identifier<Sampler> id)
    {
        Logger.DebugAssert(!_disposed);

        // NOTE: We almost never release samplers individually, because they are cheap and can be reused.
        // Ideally we would release all samplers at once when disposing the ResourceDatabase.
        _descriptorAllocator.Release(new Identifier<SamplerDescriptor>(id.Value));
    }

    public Error Swap(Handle<GPUResource> handleA, Handle<GPUResource> handleB)
    {
        ref var recordA = ref _resources.GetElementReferenceAt(handleA.ID, handleA.Generation, out var existA);
        ref var recordB = ref _resources.GetElementReferenceAt(handleB.ID, handleB.Generation, out var existB);
        if (!existA || !existB)
        {
            return Error.NotFound;
        }

        // ViewGroups and subResourceViews are pinned to their slots — save before swap
        var viewA = recordA.viewGroup;
        var viewB = recordB.viewGroup;
        var subA = recordA.subResourceView;
        var subB = recordB.subResourceView;

        var temp = recordA;
        recordA = recordB;
        recordB = temp;

        // Restore viewGroups and subResourceViews to their original slots
        recordA.viewGroup = viewA;
        recordB.viewGroup = viewB;
        recordA.subResourceView = subA;
        recordB.subResourceView = subB;

        recordA.viewGroup = D3D12Utility.CreateResourceDescriptor(_device, _descriptorAllocator, recordA.desc, recordA.ResourcePtr, viewA);
        recordB.viewGroup = D3D12Utility.CreateResourceDescriptor(_device, _descriptorAllocator, recordB.desc, recordB.ResourcePtr, viewB);

        if (subA.IsCreated && recordA.desc.Type == ResourceType.Texture)
        {
            ref readonly var descA = ref recordA.desc.TextureDescriptor;
            D3D12Utility.CreateSubresourceDescriptors(_device, _descriptorAllocator, descA, recordA.ResourcePtr, preallocated: subA);
        }

        if (subB.IsCreated && recordB.desc.Type == ResourceType.Texture)
        {
            ref readonly var descB = ref recordB.desc.TextureDescriptor;
            D3D12Utility.CreateSubresourceDescriptors(_device, _descriptorAllocator, descB, recordB.ResourcePtr, preallocated: subB);
        }

        return Error.None;
    }

    public Handle<GPUResource> Replace(Handle<GPUResource> dst, Handle<GPUResource> src)
    {
        ref var recordDst = ref _resources.GetElementReferenceAt(dst.ID, dst.Generation, out var existDst);
        ref var recordSrc = ref _resources.GetElementReferenceAt(src.ID, src.Generation, out var existSrc);
        if (!existDst || !existSrc)
        {
            return Handle<GPUResource>.Invalid;
        }

        var dstView = recordDst.viewGroup;
        var srcView = recordSrc.viewGroup;

        var temp = recordDst;
        recordDst = recordSrc;
        recordSrc = temp;

        if (dstView.srv.IsValid || dstView.uav.IsValid || dstView.cbv.IsValid || dstView.rtv.IsValid || dstView.dsv.IsValid)
        {
            recordDst.viewGroup = D3D12Utility.CreateResourceDescriptor(_device, _descriptorAllocator, recordDst.desc, recordDst.ResourcePtr, dstView);
            recordSrc.viewGroup = srcView;
        }
        else
        {
            recordDst.viewGroup = srcView;
            recordSrc.viewGroup = default;
        }

        ReleaseResource(src);

        return dst;
    }

    public void QueueReplace(Handle<GPUResource> dst, Handle<GPUResource> src)
    {
        Logger.DebugAssert(!_disposed);

        lock (_writeLock)
        {
            if (!_resources.TryGetElementAt(dst.ID, dst.Generation, out _) ||
                !_resources.TryGetElementAt(src.ID, src.Generation, out _))
            {
                return;
            }

            _pendingSwaps.Enqueue(new PendingSwapEntry(dst, src, true, _cpuFrame));
        }
    }

    public void QueueSwap(Handle<GPUResource> handleA, Handle<GPUResource> handleB)
    {
        Logger.DebugAssert(!_disposed);

        lock (_writeLock)
        {
            if (!_resources.TryGetElementAt(handleA.ID, handleA.Generation, out _) ||
                !_resources.TryGetElementAt(handleB.ID, handleB.Generation, out _))
            {
                return;
            }

            _pendingSwaps.Enqueue(new PendingSwapEntry(handleA, handleB, false, _cpuFrame));
        }
    }

    public Handle<GPUResource> CreateShared(Handle<GPUResource> src)
    {
        if (src.IsInvalid)
        {
            return Handle<GPUResource>.Invalid;
        }

        var (srcRecord, error) = GetResourceRecord(src);
        if (error.IsFailure)
        {
            return Handle<GPUResource>.Invalid;
        }

        lock (_writeLock)
        {
            var newRecord = srcRecord.Get();
            newRecord.isShared = true;

            var id = _resources.Add(newRecord, out var generation);
            return new Handle<GPUResource>(id, generation);
        }
    }

    public Handle<GPUResource> CreateEmpty()
    {
        lock (_writeLock)
        {
            var id = _resources.Add(default, out var generation);
            return new Handle<GPUResource>(id, generation);
        }
    }

    public void* MapResource(Handle<GPUResource> handle, uint subResource, ResourceRange? readRange)
    {
        var r = GetResourceRecord(handle);
        if (r.IsFailure)
        {
            return null;
        }

        var resource = r.Value.ResourcePtr;
        var rRange = readRange.HasValue ? new D3D12_RANGE { Begin = readRange.Value.Start, End = readRange.Value.End } : default;

        void* mappedData = null;
        resource.Get()->Map(subResource, readRange.HasValue ? &rRange : null, &mappedData);

        return mappedData;
    }

    public Error UnmapResource(Handle<GPUResource> handle, uint subResource, ResourceRange? writtenRange)
    {
        var r = GetResourceRecord(handle);
        if (r.IsFailure)
        {
            return r.Error;
        }

        var resource = r.Value.ResourcePtr;
        var wRange = writtenRange.HasValue ? new D3D12_RANGE { Begin = writtenRange.Value.Start, End = writtenRange.Value.End } : default;

        resource.Get()->Unmap(subResource, writtenRange.HasValue ? &wRange : null);

        return Error.None;
    }

    public ulong GetIntermediateResourceSize(Handle<GPUResource> resource, uint firstSubResource, uint numSubResources)
    {
        var r = GetResourceRecord(resource);
        if (r.IsFailure)
        {
            return 0;
        }

        return GetRequiredIntermediateSize(r.Value.ResourcePtr.Get(), firstSubResource, numSubResources);
    }

    internal void BeginFrame(ulong cpuFrame)
    {
        Logger.DebugAssert(!_disposed);
        _cpuFrame = cpuFrame;
    }

    internal void EndFrame(ulong gpuFrame)
    {
        Logger.DebugAssert(!_disposed);

        // Apply queued replace/swap ops whose frame has retired. Runs before the release pump below:
        // the ops only touch live records, and Replace funnels the displaced resource through the deferred
        // release path (tagged with the current frame) instead of destroying it under in-flight readers.
        // A dead handle at apply time (e.g. owner torn down) skips the swap.
        while (_pendingSwaps.TryPeek(out var pending) && pending.cpuFrame < gpuFrame)
        {
            _pendingSwaps.Dequeue();
            if (pending.isReplace)
            {
                Replace(pending.a, pending.b);
            }
            else
            {
                Swap(pending.a, pending.b);
            }
        }

        while (_releaseQueue.TryPeek(out var toRelease) && toRelease.fenceValue < gpuFrame)
        {
            _releaseQueue.Dequeue();
            toRelease.record.Release(_descriptorAllocator);
        }
    }

    internal void ReleaseAllResourcesImmediately()
    {
        Logger.DebugAssert(!_disposed);

        foreach (ref var entry in _releaseQueue)
        {
            entry.record.Release(_descriptorAllocator);
        }

        foreach (ref var record in _resources)
        {
#if DEBUG
            Debug.WriteLine($"[Resource Leak] Resource 0x{(nint)record.ResourcePtr.Get():X} is being released without proper disposal. This may indicate a resource leak.");
#endif
            record.Release(_descriptorAllocator);
        }

        _releaseQueue.Clear();
        _pendingSwaps.Clear();
        _resources.Clear();
#if GHOST_SAFETY_CHECKS
        _resourceName.Clear();
#endif
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _resources.Dispose();
        _samplers.Dispose();
        _releaseQueue.Dispose();
        _pendingSwaps.Dispose();

        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
