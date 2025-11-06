using Ghost.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Collections;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using Ghost.Graphics.Core;

namespace Ghost.Graphics.D3D12;

internal class D3D12ResourceDatabase : IResourceDatabase, IDisposable
{
    internal unsafe struct ResourceRecord
    {
        [StructLayout(LayoutKind.Explicit)]
        public struct ResourceUnion
        {
            [FieldOffset(0)]
            public ComPtr<D3D12MA_Allocation> allocation;
            [FieldOffset(0)]
            public ComPtr<ID3D12Resource> resource;

            public ResourceUnion(ComPtr<D3D12MA_Allocation> allocation)
            {
                this.allocation = allocation;
            }

            public ResourceUnion(ComPtr<ID3D12Resource> resource)
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
        public readonly ID3D12Resource* ResourcePtr => isExternal ? resourceUnion.resource.Get() : resourceUnion.allocation.Get()->GetResource();

        public ResourceRecord(ComPtr<D3D12MA_Allocation> allocation, uint cpuFenceValue, ResourceState state, ResourceViewGroup resourceDescriptor, ResourceDesc desc)
        {
            this.resourceUnion = new ResourceUnion(allocation);
            this.isExternal = false;

            this.viewGroup = resourceDescriptor;
            this.cpuFenceValue = cpuFenceValue;
            this.state = state;
            this.desc = desc;
        }

        public ResourceRecord(ComPtr<ID3D12Resource> resource, ResourceState state)
        {
            this.resourceUnion = new ResourceUnion(resource);
            this.isExternal = true;

            this.viewGroup = default;
            this.cpuFenceValue = ~0u;
            this.state = state;
            this.desc = ResourceDesc.FromD3D12(resource.Get()->GetDesc());
        }

        public uint Release(D3D12DescriptorAllocator descriptorAllocator)
        {
            var refCount = 0u;
            if (Allocated)
            {
                if (isExternal)
                {
                    refCount = resourceUnion.resource.Reset();
                }
                else
                {
                    refCount = resourceUnion.allocation.Reset();
                }

                resourceUnion = default;
                viewGroup = default;
            }

            descriptorAllocator.Release(viewGroup);

            return refCount;
        }
    }

    private readonly D3D12DescriptorAllocator _descriptorAllocator;

    private UnsafeSlotMap<ResourceRecord> _resources;
#if DEBUG || GHOST_EDITOR
    private readonly Dictionary<Handle<GPUResource>, string> _resourceName;
#endif

    private readonly UnsafeSlotMap<Mesh> _meshes;
    private readonly UnsafeSlotMap<Material> _materials;
    private readonly DynamicArray<Shader?> _shaders; // NOTE: We use a simple list since shader is not frequently added/removed. This can save 4 bytes for each ecs component.
    private readonly Dictionary<ShaderPassKey, ShaderPass> _shaderPasses; // NOTE: The reason we use Dictionary here is that ShaderPassKey is a presistence identifier across multiple application sessions.

    private bool _disposed;

    public D3D12ResourceDatabase(D3D12DescriptorAllocator descriptorAllocator)
    {
        _resources = new(64, Allocator.Persistent);
#if DEBUG || GHOST_EDITOR
        _resourceName = new(64);
#endif

        _meshes = new(64, Allocator.Persistent);
        _materials = new(16, Allocator.Persistent);
        _shaders = new(16);
        _shaderPasses = new(16);

        _descriptorAllocator = descriptorAllocator;
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

    public Handle<GPUResource> ImportExternalResource(ComPtr<ID3D12Resource> resource, ResourceState initialState, string? name = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var id = _resources.Add(new ResourceRecord(resource, initialState), out var generation);
        var handle = new Handle<GPUResource>(id, generation);

#if DEBUG || GHOST_EDITOR
        if (name != null)
        {
            _resourceName[handle] = name;
        }
#endif

        return handle;
    }

    public Handle<GPUResource> AddResource(ComPtr<D3D12MA_Allocation> allocation, uint cpuFenceValue, ResourceState initialState, ResourceViewGroup resourceDescriptor, ResourceDesc desc, string? name = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var id = _resources.Add(new ResourceRecord(allocation, cpuFenceValue, initialState, resourceDescriptor, desc), out var generation);
        var handle = new Handle<GPUResource>(id, generation);

#if DEBUG || GHOST_EDITOR
        if (name != null)
        {
            _resourceName[handle] = name;
        }
#endif

        return handle;
    }

    public ref ResourceRecord GetResourceInfo(Handle<GPUResource> handle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        ref var info = ref _resources.GetElementReferenceAt(handle.id, handle.generation, out var exist);
        if (!exist)
        {
            throw new KeyNotFoundException($"Resource with handle {handle} not found.");
        }

        return ref info;
    }

    public ref ResourceRecord GetResourceInfo(Handle<GPUResource> handle, out bool exist)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return ref _resources.GetElementReferenceAt(handle.id, handle.generation, out exist);
    }

    public unsafe ID3D12Resource* GetResource(Handle<GPUResource> handle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        ref var info = ref GetResourceInfo(handle);
        if (!info.Allocated)
        {
            return null;
        }

        return info.ResourcePtr;
    }

    public ResourceState GetResourceState(Handle<GPUResource> handle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return GetResourceInfo(handle).state;
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

    public ResourceDesc GetResourceDescription(Handle<GPUResource> handle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return GetResourceInfo(handle).desc;
    }

    public int GetBindlessIndex(Handle<GPUResource> handle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        ref var info = ref GetResourceInfo(handle, out var exist);
        if (!exist || !info.Allocated)
        {
            return -1;
        }

        return info.viewGroup.srv.value;
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

    public unsafe void ReleaseResource(Handle<GPUResource> handle)
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

        var refCount = info.Release(_descriptorAllocator);
#if DEBUG || GHOST_EDITOR
        _resourceName.Remove(handle, out var name);
        if (refCount > 0)
        {
            throw new GPUResourceLeakException(refCount, info.ResourcePtr, name ?? "Unknown Resource");
        }
#endif

        _resources.Remove(handle.id, handle.generation);
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
        return _meshes.Contain(handle.id, handle.generation);
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
        return _materials.Contain(handle.id, handle.generation);
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

    public void AddShaderPass(ShaderPassKey passKey, ShaderPass pass)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _shaderPasses.Add(passKey, pass);
    }

    public ShaderPass GetShaderPass(ShaderPassKey passKey)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_shaderPasses.TryGetValue(passKey, out var pass))
        {
            throw new KeyNotFoundException($"Shader pass '{passKey}' not found.");
        }

        return pass;
    }

    public void RemoveShaderPass(ShaderPassKey passKey)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_shaderPasses.Remove(passKey, out var pass))
        {
            ReleaseResource(ref pass);
        }
    }

    public void Dispose()
    {
        [Conditional("DEBUG"), Conditional("GHOST_EDITOR")]
        static void ThrowMemoryLeakException(string resourceType, int count)
        {
            throw new InvalidOperationException($"ResourceAllocator is being disposed with {count} {resourceType} still registered. Ensure all resources are released before disposing.");
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
        _meshes.Dispose();
        _materials.Dispose();

        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
