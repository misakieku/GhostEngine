using Ghost.Core;
using Ghost.Graphics.D3D12.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Ghost.Graphics.D3D12;

internal unsafe class D3D12ResourceDatabase : IResourceDatabase
{
    internal unsafe record struct ResourceRecord
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

        public ResourceBarrierData barrierData;

        public bool isExternal;
        public bool isShared;

        public readonly bool Allocated => isExternal ? resource.resource.Get() != null : resource.allocation.Get() != null;
        public readonly SharedPtr<ID3D12Resource> ResourcePtr => isExternal ? resource.resource.Get() : resource.allocation.Get()->GetResource();

        public ResourceRecord(D3D12MA_Allocation* allocation, ResourceBarrierData barrierData, ResourceViewGroup viewGroup, ResourceDesc desc)
        {
            this.resource = new __resource_union(allocation);
            this.isExternal = false;
            this.isShared = false;

            this.viewGroup = viewGroup;
            this.barrierData = barrierData;
            this.desc = desc;
        }

        public ResourceRecord(ID3D12Resource* resource, ResourceBarrierData barrierData, ResourceViewGroup viewGroup, ResourceDesc desc)
        {
            this.resource = new __resource_union(resource);
            this.isExternal = true;
            this.isShared = false;

            this.viewGroup = viewGroup;
            this.barrierData = barrierData;
            this.desc = desc;
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

    private readonly D3D12DescriptorAllocator _descriptorAllocator;

    private UnsafeSlotMap<ResourceRecord> _resources;
    private UnsafeHashMap<SamplerDesc, Identifier<Sampler>> _samplers;
#if DEBUG || GHOST_EDITOR
    private readonly Dictionary<Handle<GPUResource>, string> _resourceName;
#endif

    private UnsafeQueue<ReleaseEntry> _releaseQueue;

    private int _writeLock;

    private ulong _cpuFrame;
    private bool _disposed;

    public ResourceBarrierData globalBarrier;

    public D3D12ResourceDatabase(D3D12DescriptorAllocator descriptorAllocator)
    {
        _descriptorAllocator = descriptorAllocator;

        _resources = new UnsafeSlotMap<ResourceRecord>(64, AllocationHandle.Persistent, AllocationOption.Clear);
        _samplers = new UnsafeHashMap<SamplerDesc, Identifier<Sampler>>(32, AllocationHandle.Persistent);
#if DEBUG || GHOST_EDITOR
        _resourceName = new Dictionary<Handle<GPUResource>, string>(64);
#endif

        _releaseQueue = new UnsafeQueue<ReleaseEntry>(32, AllocationHandle.Persistent);
    }

    ~D3D12ResourceDatabase()
    {
        Dispose();
    }

    internal Handle<GPUResource> ImportExternalResource(ID3D12Resource* pResource, ResourceBarrierData initialBarrierData, ResourceViewGroup viewGroup, ResourceDesc desc, string? name = null)
    {
        Logger.DebugAssert(!_disposed);

        if (pResource == null)
        {
#if DEBUG
            Debugger.Break();
#endif
            return Handle<GPUResource>.Invalid;
        }

        var spinner = new SpinWait();
        while (Interlocked.CompareExchange(ref _writeLock, 1, 0) != 0)
        {
            spinner.SpinOnce();
        }

        try
        {
            var id = _resources.Add(new ResourceRecord(pResource, initialBarrierData, viewGroup, desc), out var generation);
            var handle = new Handle<GPUResource>(id, generation);

#if DEBUG || GHOST_EDITOR
            if (!string.IsNullOrEmpty(name))
            {
                pResource->SetName(name);
                _resourceName[handle] = name;
            }
#endif

            return handle;
        }
        finally
        {
            Interlocked.Exchange(ref _writeLock, 0);
        }
    }

    internal Handle<GPUResource> AddAllocation(D3D12MA_Allocation* allocation, ResourceBarrierData initialBarrierData, ResourceViewGroup resourceDescriptor, ResourceDesc desc, string? name = null)
    {
        Logger.DebugAssert(!_disposed);

        if (allocation == null)
        {
#if DEBUG
            Debugger.Break();
#endif
            return Handle<GPUResource>.Invalid;
        }

        var spinner = new SpinWait();
        while (Interlocked.CompareExchange(ref _writeLock, 1, 0) != 0)
        {
            spinner.SpinOnce();
        }

        try
        {
            var id = _resources.Add(new ResourceRecord(allocation, initialBarrierData, resourceDescriptor, desc), out var generation);
            var handle = new Handle<GPUResource>(id, generation);

#if DEBUG || GHOST_EDITOR
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
        finally
        {
            Interlocked.Exchange(ref _writeLock, 0);
        }
    }

    public void EnterParallelRead()
    {
        Logger.DebugAssert(!_disposed);
        Interlocked.Exchange(ref _writeLock, 1);
    }

    public void ExitParallelRead()
    {
        Logger.DebugAssert(!_disposed);
        Interlocked.Exchange(ref _writeLock, 0);
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

    public Result<ResourceBarrierData, Error> GetResourceBarrierData(Handle<GPUResource> handle)
    {
        var r = GetResourceRecord(handle);
        if (r.IsFailure)
        {
            return r.Error;
        }

        return r.Value.barrierData;
    }

    public Error SetResourceBarrierData(Handle<GPUResource> handle, ResourceBarrierData data)
    {
        var r = GetResourceRecord(handle);
        if (r.IsFailure)
        {
            return r.Error;
        }

        r.Value.barrierData = data;
        return Error.None;
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

    public uint GetBindlessIndex(Handle<GPUResource> handle, BindlessAccess access = BindlessAccess.ShaderResource)
    {
        var r = GetResourceRecord(handle);
        if (r.IsFailure || !r.Value.Allocated)
        {
            return uint.MaxValue;
        }

        return access switch
        {
            BindlessAccess.ShaderResource => (uint)r.Value.viewGroup.srv.Value,
            BindlessAccess.ConstantBuffer => (uint)r.Value.viewGroup.cbv.Value,
            BindlessAccess.UnorderedAccess => (uint)r.Value.viewGroup.uav.Value,
            _ => uint.MaxValue,
        };
    }

    public string? GetResourceName(Handle<GPUResource> handle)
    {
        Logger.DebugAssert(!_disposed);

#if DEBUG || GHOST_EDITOR
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

        if (!_resources.TryGetElementAt(handle.ID, handle.Generation, out var record))
        {
            return;
        }

        var entry = new ReleaseEntry(record, _cpuFrame);

        _releaseQueue.Enqueue(entry);
        _resources.Remove(handle.ID, handle.Generation);

#if DEBUG || GHOST_EDITOR
        _resourceName.Remove(handle, out var name);
#endif
    }

    public void ReleaseResourceImmediately(Handle<GPUResource> handle)
    {
        Logger.DebugAssert(!_disposed);

        ref var info = ref _resources.GetElementReferenceAt(handle.ID, handle.Generation, out var exist);
        if (!exist || !info.Allocated)
        {
            return;
        }

        info.Release(_descriptorAllocator);
        _resources.Remove(handle.ID, handle.Generation);
    }

    public Identifier<Sampler> AddSampler(ref readonly SamplerDesc desc, int id)
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

    public bool TryGetSampler(ref readonly SamplerDesc desc, out Identifier<Sampler> id)
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

        var temp = recordA;
        recordA = recordB;
        recordB = temp;

        return Error.None;
    }

    public Handle<GPUResource> CreateShared(Handle<GPUResource> src)
    {
        if (src.IsInvalid)
        {
            return Handle<GPUResource>.Invalid;
        }

        var spinner = new SpinWait();
        while (Interlocked.CompareExchange(ref _writeLock, 1, 0) != 0)
        {
            spinner.SpinOnce();
        }

        try
        {
            var (srcRecord, error) = GetResourceRecord(src);
            if (error.IsFailure)
            {
                return Handle<GPUResource>.Invalid;
            }

            var newRecord = srcRecord.Get();
            newRecord.isShared = true;

            var id = _resources.Add(newRecord, out var generation);
            return new Handle<GPUResource>(id, generation);
        }
        finally
        {
            Interlocked.Exchange(ref _writeLock, 0);
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
            record.Release(_descriptorAllocator);
        }

        _resources.Clear();
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

        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
