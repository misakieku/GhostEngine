using Ghost.Core;
using Ghost.Graphics.D3D12.Utilities;
using Ghost.Graphics.Data;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Collections;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.InteropServices;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Ghost.Graphics.D3D12;

internal class D3D12ResourceDatabase : IResourceDatabase, IDisposable
{
    internal unsafe struct ResourceInfo
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
        public ResourceViewGroup descriptor;
        public ResourceUnion resourceUnion;
        public uint cpuFenceValue;
        public ResourceState state;
        public readonly bool isExternal;

        public readonly bool Allocated => isExternal ? resourceUnion.resource.Get() != null : resourceUnion.allocation.Get()->IsNotNull;
        public readonly ID3D12Resource* ResourcePtr => isExternal ? resourceUnion.resource.Get() : resourceUnion.allocation.Get()->GetResource();

        public ResourceInfo(ComPtr<D3D12MA_Allocation> allocation, uint cpuFenceValue, ResourceState state, ResourceViewGroup resourceDescriptor, ResourceDesc desc)
        {
            this.resourceUnion = new ResourceUnion(allocation);
            this.isExternal = false;

            this.descriptor = resourceDescriptor;
            this.cpuFenceValue = cpuFenceValue;
            this.state = state;
            this.desc = desc;
        }

        public ResourceInfo(ComPtr<ID3D12Resource> resource, ResourceState state)
        {
            this.resourceUnion = new ResourceUnion(resource);
            this.isExternal = true;

            this.descriptor = default;
            this.cpuFenceValue = ~0u;
            this.state = state;
            this.desc = ResourceDesc.FromD3D12(resource.Get()->GetDesc());
        }

        public uint Release()
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

                resourceUnion = default;
                descriptor = default;
            }

            return refCount;
        }
    }

    private struct Slot<T>
    {
        public T value;
        public bool occupied;
    }

    private UnsafeSlotMap<ResourceInfo> _resources;
#if DEBUG || GHOST_EDITOR
    private readonly Dictionary<ResourceInfo, string> _resourceName;
#endif

    private readonly UnsafeSlotMap<Mesh> _meshes;
    private readonly UnsafeSlotMap<Material> _materials;

    // NOTE: We use a simple list since shader is not frequently added/removed. This can save 4 bytes for each ecs component.
    private readonly DynamicArray<Slot<Shader>> _shaders;

    private readonly D3D12DescriptorAllocator _descriptorAllocator;

    private bool _disposed;

    public D3D12ResourceDatabase(D3D12DescriptorAllocator descriptorAllocator)
    {
        _resources = new(64, Misaki.HighPerformance.LowLevel.Buffer.Allocator.Persistent);
#if DEBUG || GHOST_EDITOR
        _resourceName = new(64);
#endif

        _meshes = new(64, Misaki.HighPerformance.LowLevel.Buffer.Allocator.Persistent);
        _materials = new(16, Misaki.HighPerformance.LowLevel.Buffer.Allocator.Persistent);
        _shaders = new(16);

        _descriptorAllocator = descriptorAllocator;
    }

    ~D3D12ResourceDatabase()
    {
        Dispose();
    }

    public Handle<GPUResource> ImportExternalResource<T>(T resource, ResourceState initialState)
        where T : unmanaged
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (resource is not ComPtr<ID3D12Resource> d3d12Resource)
        {
            throw new InvalidOperationException($"Expect ComPtr<ID3D12Resource> in D3D12ResourceDatabase, but got {typeof(T)}.");
        }

        var id = _resources.Add(new ResourceInfo(d3d12Resource, initialState), out var generation);
        return new Handle<GPUResource>(id, generation);
    }

    public Handle<GPUResource> AddResource(ComPtr<D3D12MA_Allocation> allocation, uint cpuFenceValue, ResourceState initialState, ResourceViewGroup resourceDescriptor, ResourceDesc desc)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var id = _resources.Add(new ResourceInfo(allocation, cpuFenceValue, initialState, resourceDescriptor, desc), out var generation);
        return new Handle<GPUResource>(id, generation);
    }

    public ref ResourceInfo GetResourceInfo(Handle<GPUResource> handle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        ref var info = ref _resources.GetElementReferenceAt(handle.id, handle.generation, out var exist);
        if (!exist)
        {
            throw new KeyNotFoundException($"Resource with handle {handle} not found.");
        }

        return ref info;
    }

    public ref ResourceInfo GetResourceInfo(Handle<GPUResource> handle, out bool exist)
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
        ref var info = ref GetResourceInfo(handle, out var exist);
        if (!exist || !info.Allocated)
        {
            return -1;
        }

        return info.descriptor.srv.value;
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

        var refCount = info.Release();
#if DEBUG || GHOST_EDITOR
        if (refCount > 0)
        {
            throw new GPUResourceLeakException(refCount, info.ResourcePtr, _resourceName[info]);
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
        ref var mesh = ref _meshes.GetElementReferenceAt(handle.id, handle.generation, out var exist);
        if (!exist)
        {
            throw new ArgumentOutOfRangeException(nameof(handle), $"Mesh {handle} is invalid.");
        }

        return ref mesh;
    }

    private void ReleaseMeshResources(ref readonly Mesh mesh)
    {
        mesh.ReleaseCpuResources();

        ref var vertexRef = ref GetResourceInfo(mesh.vertexBuffer.AsResource());
        ref var indexRef = ref GetResourceInfo(mesh.indexBuffer.AsResource());
        _descriptorAllocator.Release(vertexRef.descriptor);
        _descriptorAllocator.Release(indexRef.descriptor);

        ReleaseResource(mesh.vertexBuffer.AsResource());
        ReleaseResource(mesh.indexBuffer.AsResource());
    }

    public void ReleaseMesh(Handle<Mesh> handle)
    {
        ref var meshSlot = ref _meshes.GetElementReferenceAt(handle.id, handle.generation, out var exist);
        if (!exist)
        {
            return;
        }

        ReleaseMeshResources(ref meshSlot);
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
        ref var material = ref _materials.GetElementReferenceAt(handle.id, handle.generation, out var exist);
        if (!exist)
        {
            throw new ArgumentOutOfRangeException(nameof(handle), $"Material handle {handle} is invalid.");
        }

        return ref material;
    }

    public void ReleaseMaterial(Handle<Material> handle)
    {
        ref var material = ref _materials.GetElementReferenceAt(handle.id, handle.generation, out var exist);
        if (!exist)
        {
            return;
        }

        material.Dispose();
    }

    public Identifier<Shader> AddShader(ref readonly Shader shader)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var id = _shaders.Count;
        _shaders.Add(new Slot<Shader> { value = shader, occupied = true });
        return new Identifier<Shader>(id);
    }

    public bool HasShader(Identifier<Shader> id)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return id.value >= 0 && id.value < _shaders.Count && _shaders[id.value].occupied;
    }

    public ref Shader GetShaderReference(Identifier<Shader> id)
    {
        if (!HasShader(id))
        {
            throw new ArgumentOutOfRangeException(nameof(id), $"Shader id {id} is invalid.");
        }

        ref var shader = ref _shaders[id.value].value;
        return ref shader;
    }

    public void ReleaseShader(Identifier<Shader> handle)
    {
        if (!HasShader(handle))
        {
            return;
        }

        ref var shader = ref _shaders[handle.value].value;
        shader.Dispose();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

#if DEBUG || GHOST_EDITOR
        if (_resources.Count > 0)
        {
            throw new InvalidOperationException($"ResourceAllocator is being disposed with {_resources.Count} allocations still registered. Ensure all resources are released before disposing.");
        }

        if (_meshes.Count > 0)
        {
            throw new InvalidOperationException($"ResourceAllocator is being disposed with {_meshes.Count} meshes still registered. Ensure all meshes are released before disposing.");
        }

        if (_materials.Count > 0)
        {
            throw new InvalidOperationException($"ResourceAllocator is being disposed with {_materials.Count} materials still registered. Ensure all materials are released before disposing.");
        }

        if (_shaders.Count > 0)
        {
            throw new InvalidOperationException($"ResourceAllocator is being disposed with {_shaders.Count} shaders still registered. Ensure all shaders are released before disposing.");
        }
#endif
        _resources.Dispose();
        _meshes.Dispose();
        _materials.Dispose();

        _disposed = true;

        GC.SuppressFinalize(this);
    }
}