using Ghost.Core;
using Ghost.Graphics.Core;
using Ghost.Graphics.D3D12.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Collections;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;
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
        public ResourceUnion resourceUnion;
        public ResourceState state;
        public uint cpuFenceValue;
        public readonly bool isExternal;

        public readonly bool Allocated => isExternal ? resourceUnion.resource.Get() != null : resourceUnion.allocation.Get() != null;
        public readonly SharedPtr<ID3D12Resource> ResourcePtr => isExternal ? resourceUnion.resource.Get() : resourceUnion.allocation.Get()->GetResource();

        public ResourceRecord(D3D12MA_Allocation* allocation, uint cpuFenceValue, ResourceState state, ResourceViewGroup resourceDescriptor, ResourceDesc desc)
        {
            this.resourceUnion = new ResourceUnion(allocation);
            this.isExternal = false;

            this.viewGroup = resourceDescriptor;
            this.cpuFenceValue = cpuFenceValue;
            this.state = state;
            this.desc = desc;
        }

        public ResourceRecord(ID3D12Resource* resource, ResourceState state, ResourceViewGroup viewGroup)
        {
            this.resourceUnion = new ResourceUnion(resource);
            this.isExternal = true;

            this.viewGroup = viewGroup;
            this.cpuFenceValue = ~0u;
            this.state = state;
            this.desc = ResourceDesc.FromD3D12(resource->GetDesc());
        }

        public uint Release(D3D12DescriptorAllocator descriptorAllocator)
        {
            var refCount = 0u;
            if (Allocated)
            {
                if (isExternal)
                {
                    refCount = resourceUnion.resource.Get()->Release();
                }
                else
                {
                    refCount = resourceUnion.allocation.Get()->Release();
                }
            }

            descriptorAllocator.Release(viewGroup);

            resourceUnion = default;
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
    private readonly DynamicArray<Shader?> _shaders; // NOTE: We use a simple list since shader is not frequently added/removed. This can save 4 bytes for each ecs component.
    private readonly Dictionary<ShaderPassKey, ShaderPass> _shaderPasses; // NOTE: The reason we use Dictionary here is that ShaderPassKey is a presistence identifier across multiple application sessions.

    private int _lastSamplerId;
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
        _shaders = new DynamicArray<Shader?>(16);
        _shaderPasses = new Dictionary<ShaderPassKey, ShaderPass>(16);

        _lastSamplerId = -1;
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

    public unsafe Handle<GPUResource> ImportExternalResource(ID3D12Resource* pResource, ResourceState initialState, ResourceViewGroup viewGroup, string? name = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var id = _resources.Add(new ResourceRecord(pResource, initialState, viewGroup), out var generation);
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

    public unsafe Handle<GPUResource> AddResource(D3D12MA_Allocation* allocation, uint cpuFenceValue, ResourceState initialState, ResourceViewGroup resourceDescriptor, ResourceDesc desc, string? name = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var id = _resources.Add(new ResourceRecord(allocation, cpuFenceValue, initialState, resourceDescriptor, desc), out var generation);
        var handle = new Handle<GPUResource>(id, generation);

#if DEBUG || GHOST_EDITOR
        if (!string.IsNullOrEmpty(name))
        {
            allocation->SetName(name);
            _resourceName[handle] = name;
        }
#endif

        return handle;
    }

    public bool HasResource(Handle<GPUResource> handle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _resources.Contains(handle.id, handle.generation);
    }

    public RefResult<ResourceRecord, ResultStatus> GetResourceRecord(Handle<GPUResource> handle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        ref var info = ref _resources.GetElementReferenceAt(handle.id, handle.generation, out var exist);
        if (!exist)
        {
            return Result.CreateRef(ref Unsafe.NullRef<ResourceRecord>(), ResultStatus.NotFound);
        }

        return Result.CreateRef(ref info, ResultStatus.Success);
    }

    public ref ResourceRecord GetResourceRecord(Handle<GPUResource> handle, out bool exist)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return ref _resources.GetElementReferenceAt(handle.id, handle.generation, out exist);
    }

    public SharedPtr<ID3D12Resource> GetResource(Handle<GPUResource> handle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var r = GetResourceRecord(handle);
        if (r.Status != ResultStatus.Success)
        {
            return null;
        }

        return r.Value.ResourcePtr;
    }

    public Result<ResourceState, ResultStatus> GetResourceState(Handle<GPUResource> handle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var r = GetResourceRecord(handle);
        if (r.Status != ResultStatus.Success)
        {
            return Result.Create(ResourceState.Common, r.Status);
        }

        return Result.Create(r.Value.state, ResultStatus.Success);
    }

    public void SetResourceState(Handle<GPUResource> handle, ResourceState state)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        ref var info = ref _resources.GetElementReferenceAt(handle.id, handle.generation, out var exist);
        if (!exist || !info.Allocated)
        {
            throw new KeyNotFoundException($"Resource with handle {handle} not found.");
        }

        info.state = state;
    }

    public Result<ResourceDesc, ResultStatus> GetResourceDescription(Handle<GPUResource> handle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var r = GetResourceRecord(handle);

        if (r.Status != ResultStatus.Success)
        {
            return Result.Create(default(ResourceDesc), r.Status);
        }

        return Result.Create(r.Value.desc, ResultStatus.Success);
    }

    public Result<uint, ResultStatus> GetBindlessIndex(Handle<GPUResource> handle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        ref var info = ref GetResourceRecord(handle, out var exist);
        if (!exist || !info.Allocated)
        {
            return Result.Create(0u, ResultStatus.NotFound);
        }

        return Result.Create((uint)info.viewGroup.srv.value, ResultStatus.Success);
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

        ref var info = ref _resources.GetElementReferenceAt(handle.id, handle.generation, out var exist);
        if (!exist || !info.Allocated)
        {
            return;
        }

        info.Release(_descriptorAllocator);
        //System.Diagnostics.Debug.Assert(info.Release(_descriptorAllocator) == 0);
#if DEBUG || GHOST_EDITOR
        _resourceName.Remove(handle, out var name);
#endif

        _resources.Remove(handle.id, handle.generation);
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
        return _meshes.Contains(handle.id, handle.generation);
    }

    public ref Mesh GetMeshReference(Handle<Mesh> handle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        ref var mesh = ref _meshes.GetElementReferenceAt(handle.id, handle.generation, out var exist);
        if (!exist)
        {
            throw new ArgumentOutOfRangeException(nameof(handle), $"Mesh {handle} is invalid.");
        }

        return ref mesh;
    }

    public void ReleaseMesh(Handle<Mesh> handle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        ref var mesh = ref _meshes.GetElementReferenceAt(handle.id, handle.generation, out var exist);
        if (!exist)
        {
            return;
        }

        ReleaseResource(ref mesh);
        _meshes.Remove(handle.id, handle.generation);
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
        return _materials.Contains(handle.id, handle.generation);
    }

    public ref Material GetMaterialReference(Handle<Material> handle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        ref var material = ref _materials.GetElementReferenceAt(handle.id, handle.generation, out var exist);
        if (!exist)
        {
            throw new ArgumentOutOfRangeException(nameof(handle), $"Material handle {handle} is invalid.");
        }

        return ref material;
    }

    public void ReleaseMaterial(Handle<Material> handle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        ref var material = ref _materials.GetElementReferenceAt(handle.id, handle.generation, out var exist);
        if (!exist)
        {
            return;
        }

        ReleaseResource(ref material);
        _materials.Remove(handle.id, handle.generation);
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
        return id.value >= 0 && id.value < _shaders.Count && _shaders[id.value] != null;
    }

    public Shader GetShaderReference(Identifier<Shader> id)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!HasShader(id))
        {
            throw new ArgumentOutOfRangeException(nameof(id), $"Shader id {id} is invalid.");
        }

        var shader = _shaders[id.value]!;
        return shader;
    }

    public void ReleaseShader(Identifier<Shader> id)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!HasShader(id))
        {
            return;
        }

        ref var shader = ref _shaders[id.value]!;
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

        // SDL are reference type, it will be managed by GC, so we don't throw exception here.
        for (var i = 0; i < _shaders.Count; i++)
        {
            ref var shader = ref _shaders[i];
            if (shader == null)
            {
                continue;
            }

            ReleaseResource(ref shader);
        }

        // Same for shader pass.
        foreach (var kv in _shaderPasses)
        {
            var pass = kv.Value;
            ReleaseResource(ref pass);
        }

        _resources.Dispose();
        _samplers.Dispose();
        _meshes.Dispose();
        _materials.Dispose();

        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
