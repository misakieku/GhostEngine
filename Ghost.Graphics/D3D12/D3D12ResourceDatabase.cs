using Ghost.Core;
using Ghost.Graphics.Core;
using Ghost.Graphics.D3D12.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Collections;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.InteropServices;
using TerraFX.Interop.DirectX;

namespace Ghost.Graphics.D3D12;

internal class D3D12ResourceDatabase : IResourceDatabase
{
    internal unsafe record struct ResourceRecord
    {
        [StructLayout(LayoutKind.Explicit)]
        public struct ResourceUnion
        {
            [FieldOffset(0)]
            public UniquePtr<D3D12MA_Allocation> allocation;
            [FieldOffset(0)]
            public UniquePtr<ID3D12Resource> resource;

            public ResourceUnion(D3D12MA_Allocation* allocation)
            {
                this.allocation = allocation;
            }

            public ResourceUnion(ID3D12Resource* resource)
            {
                this.resource = resource;
            }
        }

        public ResourceDesc desc;
        public ResourceViewGroup viewGroup;
        public ResourceUnion resource;

        //public BarrierLayout layout;
        //public BarrierAccess access;
        //public BarrierSync sync;
        public ResourceBarrierData barrierData;

        public uint cpuFenceValue;
        public readonly bool isExternal;

        public readonly bool Allocated => isExternal ? resource.resource.Get() != null : resource.allocation.Get() != null;
        public readonly SharedPtr<ID3D12Resource> ResourcePtr => isExternal ? resource.resource.Get() : resource.allocation.Get()->GetResource();

        public ResourceRecord(D3D12MA_Allocation* allocation, uint cpuFenceValue, ResourceBarrierData barrierData, ResourceViewGroup resourceDescriptor, ResourceDesc desc)
        {
            this.resource = new ResourceUnion(allocation);
            this.isExternal = false;

            this.viewGroup = resourceDescriptor;
            this.cpuFenceValue = cpuFenceValue;
            this.barrierData = barrierData;
            this.desc = desc;
        }

        public ResourceRecord(ID3D12Resource* resource, ResourceBarrierData barrierData, ResourceViewGroup viewGroup)
        {
            this.resource = new ResourceUnion(resource);
            this.isExternal = true;

            this.viewGroup = viewGroup;
            this.cpuFenceValue = ~0u;
            this.barrierData = barrierData;
            this.desc = resource->GetDesc().ToResourceDesc();
        }

        public uint Release(D3D12DescriptorAllocator descriptorAllocator)
        {
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

            resource = default;
            viewGroup = default;

            return refCount;
        }
    }

    private readonly D3D12DescriptorAllocator _descriptorAllocator;

    private UnsafeSlotMap<ResourceRecord> _resources;
#if DEBUG || GHOST_EDITOR
    private readonly Dictionary<Handle<GPUResource>, string> _resourceName;
#endif

    private UnsafeHashMap<SamplerDesc, Identifier<Sampler>> _samplers;
    private UnsafeSlotMap<Mesh> _meshes;
    private UnsafeSlotMap<Material> _materials;
    private readonly DynamicArray<Shader> _shaders; // TODO: Use SlotMap?

    private bool _disposed;

    public D3D12ResourceDatabase(D3D12DescriptorAllocator descriptorAllocator)
    {
        _descriptorAllocator = descriptorAllocator;

        _resources = new UnsafeSlotMap<ResourceRecord>(64, Allocator.Persistent, AllocationOption.Clear);
#if DEBUG || GHOST_EDITOR
        _resourceName = new Dictionary<Handle<GPUResource>, string>(64);
#endif
        _samplers = new UnsafeHashMap<SamplerDesc, Identifier<Sampler>>(32, Allocator.Persistent);
        _meshes = new UnsafeSlotMap<Mesh>(64, Allocator.Persistent, AllocationOption.Clear);
        _materials = new UnsafeSlotMap<Material>(16, Allocator.Persistent, AllocationOption.Clear);
        _shaders = new DynamicArray<Shader>(16);
    }

    ~D3D12ResourceDatabase()
    {
        Dispose();
    }

    private void ReleaseResource<T>(ref T resource)
        where T : IResourceReleasable
    {
        resource.ReleaseResource(this);
        resource = default!;
    }

    public unsafe Handle<GPUResource> ImportExternalResource(ID3D12Resource* pResource, ResourceBarrierData initialBarrierData, ResourceViewGroup viewGroup, string? name = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (pResource == null)
        {
#if DEBUG
            System.Diagnostics.Debugger.Break();
#endif
            return Handle<GPUResource>.Invalid;
        }

        var id = _resources.Add(new ResourceRecord(pResource, initialBarrierData, viewGroup), out var generation);
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

    public unsafe Handle<GPUResource> AddAllocation(D3D12MA_Allocation* allocation, uint cpuFenceValue, ResourceBarrierData initialBarrierData, ResourceViewGroup resourceDescriptor, ResourceDesc desc, string? name = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (allocation == null)
        {
#if DEBUG
            System.Diagnostics.Debugger.Break();
#endif
            return Handle<GPUResource>.Invalid;
        }

        var id = _resources.Add(new ResourceRecord(allocation, cpuFenceValue, initialBarrierData, resourceDescriptor, desc), out var generation);
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

    public bool HasResource(Handle<GPUResource> handle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _resources.Contains(handle.ID, handle.Generation);
    }

    public RefResult<ResourceRecord, ErrorStatus> GetResourceRecord(Handle<GPUResource> handle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        ref var info = ref _resources.GetElementReferenceAt(handle.ID, handle.Generation, out var exist);
        if (!exist)
        {
            return ErrorStatus.NotFound;
        }

        return RefResult<ResourceRecord, ErrorStatus>.Success(ref info);
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

    public Result<ResourceBarrierData, ErrorStatus> GetResourceBarrierData(Handle<GPUResource> handle)
    {
        var r = GetResourceRecord(handle);
        if (r.IsFailure)
        {
            return r.Error;
        }

        return r.Value.barrierData;
    }

    public ErrorStatus SetResourceBarrierData(Handle<GPUResource> handle, ResourceBarrierData data)
    {
        var r = GetResourceRecord(handle);
        if (r.IsFailure)
        {
            return r.Error;
        }

        r.Value.barrierData = data;
        return ErrorStatus.None;
    }

    public Result<ResourceDesc, ErrorStatus> GetResourceDescription(Handle<GPUResource> handle)
    {
        var r = GetResourceRecord(handle);
        if (r.IsFailure)
        {
            return r.Error;
        }

        return r.Value.desc;
    }

    public uint GetBindlessIndex(Handle<GPUResource> handle)
    {
        var r = GetResourceRecord(handle);
        if (r.IsFailure || !r.Value.Allocated)
        {
            return ~0u;
        }

        return (uint)r.Value.viewGroup.srv.Value;
    }

    public string? GetResourceName(Handle<GPUResource> handle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

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
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!handle.IsValid)
        {
            return;
        }

        ref var info = ref _resources.GetElementReferenceAt(handle.ID, handle.Generation, out var exist);
        if (!exist || !info.Allocated)
        {
            return;
        }

        info.Release(_descriptorAllocator);
        //System.Diagnostics.Debug.Assert(info.Release(_descriptorAllocator) == 0);
#if DEBUG || GHOST_EDITOR
        _resourceName.Remove(handle, out var name);
#endif

        _resources.Remove(handle.ID, handle.Generation);
    }

    public Identifier<Sampler> CreateSampler(ref readonly SamplerDesc desc, int id)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

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
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _samplers.TryGetValue(desc, out id);
    }

    public Handle<Mesh> AddMesh(ref readonly Mesh mesh)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var id = _meshes.Add(mesh, out var generation);
        return new Handle<Mesh>(id, generation);
    }

    public bool HasMesh(Handle<Mesh> handle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _meshes.Contains(handle.ID, handle.Generation);
    }

    public RefResult<Mesh, ErrorStatus> GetMeshReference(Handle<Mesh> handle)
    {
        ref var mesh = ref _meshes.GetElementReferenceAt(handle.ID, handle.Generation, out var exist);
        if (!exist)
        {
            return ErrorStatus.NotFound;
        }

        return RefResult<Mesh, ErrorStatus>.Success(ref mesh);
    }

    public void ReleaseMesh(Handle<Mesh> handle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        ref var mesh = ref _meshes.GetElementReferenceAt(handle.ID, handle.Generation, out var exist);
        if (!exist)
        {
            return;
        }

        ReleaseResource(ref mesh);
        _meshes.Remove(handle.ID, handle.Generation);
    }

    public Handle<Material> AddMaterial(ref readonly Material material)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var id = _materials.Add(material, out var generation);
        return new Handle<Material>(id, generation);
    }

    public bool HasMaterial(Handle<Material> handle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _materials.Contains(handle.ID, handle.Generation);
    }

    public RefResult<Material, ErrorStatus> GetMaterialReference(Handle<Material> handle)
    {
        ref var material = ref _materials.GetElementReferenceAt(handle.ID, handle.Generation, out var exist);
        if (!exist)
        {
            return ErrorStatus.NotFound;
        }

        return RefResult<Material, ErrorStatus>.Success(ref material);
    }

    public void ReleaseMaterial(Handle<Material> handle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        ref var material = ref _materials.GetElementReferenceAt(handle.ID, handle.Generation, out var exist);
        if (!exist)
        {
            return;
        }

        ReleaseResource(ref material);
        _materials.Remove(handle.ID, handle.Generation);
    }

    public Identifier<Shader> AddShader(Shader shader)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var id = _shaders.Count;
        _shaders.Add(shader);
        return new Identifier<Shader>(id);
    }

    public bool HasShader(Identifier<Shader> id)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return id.Value >= 0 && id.Value < _shaders.Count;
    }

    public RefResult<Shader, ErrorStatus> GetShaderReference(Identifier<Shader> id)
    {
        if (!HasShader(id))
        {
            return ErrorStatus.NotFound;
        }

        return RefResult<Shader, ErrorStatus>.Success(ref _shaders[id.Value]);
    }

    public void ReleaseShader(Identifier<Shader> id)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!HasShader(id))
        {
            return;
        }

        ref var shader = ref _shaders[id.Value]!;
        ReleaseResource(ref shader);
    }

    public void Dispose()
    {
        static void ThrowMemoryLeakException(string resourceType, int count)
        {
            throw new MemoryLeakException($"ResourceAllocator is being disposed with {count} {resourceType} still registered. Ensure all resources are released before disposing.");
        }

        if (_disposed)
        {
            return;
        }

        if (_resources.Count > 0)
        {
            ThrowMemoryLeakException("GPU resources", _resources.Count);
        }

        if (_meshes.Count > 0)
        {
            ThrowMemoryLeakException("meshes", _meshes.Count);
        }

        if (_materials.Count > 0)
        {
            ThrowMemoryLeakException("materials", _materials.Count);
        }

        // DSL are reference space, it will be managed by GC, so we don't throw exception here.
        for (var i = 0; i < _shaders.Count; i++)
        {
            ref var shader = ref _shaders[i];
            ReleaseResource(ref shader);
        }

        _resources.Dispose();
        _samplers.Dispose();
        _meshes.Dispose();
        _materials.Dispose();

        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
