using Ghost.Core;
using Ghost.Graphics.Data;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Collections;
using Misaki.HighPerformance.LowLevel.Collections;
using Win32.Graphics.D3D12MemoryAllocator;
using Win32.Graphics.Direct3D12;

namespace Ghost.Graphics.D3D12;

internal class D3D12ResourceDatabase : IResourceDatabase, IDisposable
{
    private struct Slot<T>
    {
        public T value;
        public bool isValid;
    }

    public struct ResourceInfo : IDisposable
    {
        public readonly Allocation allocation;
        public readonly uint cpuFenceValue;
        public ResourceState state;

        public readonly bool Allocated => allocation.IsNotNull;

        public ResourceInfo(in Allocation allocation, uint cpuFenceValue, ResourceState state)
        {
            this.allocation = allocation;
            this.cpuFenceValue = cpuFenceValue;
            this.state = state;
        }

        public readonly void Dispose()
        {
            if (allocation.IsNull)
            {
                return;
            }

            allocation.Release();
        }
    }

    private UnsafeSlotMap<ResourceInfo> _resources;

    // NOTE: We use a simple list for shaders since they are not frequently added/removed. This can save 4 bytes for each ecs component.
    private readonly DynamicArray<Slot<Mesh>> _meshDatas;
    private readonly DynamicArray<Slot<Shader>> _shaders;

    private readonly D3D12DescriptorAllocator _descriptorAllocator;

    private bool _disposed;

    public D3D12ResourceDatabase(D3D12DescriptorAllocator descriptorAllocator)
    {
        _resources = new(64, Misaki.HighPerformance.LowLevel.Buffer.Allocator.Persistent);

        _meshDatas = new(64);
        _shaders = new(16);

        _descriptorAllocator = descriptorAllocator;
    }

    ~D3D12ResourceDatabase()
    {
        Dispose();
    }

    public ResourceHandle AddResource(ref readonly Allocation allocation, uint cpuFenceValue, ResourceState initialState)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var id = _resources.Add(new ResourceInfo(allocation, cpuFenceValue, initialState), out var generation);
        return new ResourceHandle(id, generation);
    }

    public ref ResourceInfo GetResourceInfo(ResourceHandle handle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        ref var info = ref _resources.GetElementReferenceAt(handle.id, handle.generation, out var exist);
        if (!exist)
        {
            throw new KeyNotFoundException($"Resource with handle {handle} not found.");
        }

        return ref info;
    }

    public ref ResourceInfo GetResourceInfo(ResourceHandle handle, out bool exist)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return ref _resources.GetElementReferenceAt(handle.id, handle.generation, out exist);
    }

    public unsafe T* GetResource<T>(ResourceHandle handle)
        where T : unmanaged
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (typeof(T) != typeof(ID3D12Resource))
        {
            return null;
        }

        var info = GetResourceInfo(handle);
        if (!info.Allocated)
        {
            return null;
        }

        return (T*)info.allocation.Resource;
    }

    public unsafe ID3D12Resource* GetResource(ResourceHandle handle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var info = GetResourceInfo(handle);
        if (!info.Allocated)
        {
            return null;
        }

        return info.allocation.Resource;
    }

    public ResourceState GetResourceState(ResourceHandle handle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return GetResourceInfo(handle).state;
    }

    public void SetResourceState(ResourceHandle handle, ResourceState state)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        ref var info = ref _resources.GetElementReferenceAt(handle.id, handle.generation, out var exist);
        if (!exist || info.Allocated == false)
        {
            throw new KeyNotFoundException($"Resource with handle {handle} not found.");
        }

        info.state = state;
    }

    public void RemoveResource(ResourceHandle handle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        ref var info = ref _resources.GetElementReferenceAt(handle.id, handle.generation, out var exist);
        if (!exist || info.Allocated == false)
        {
            return;
        }

        info.Dispose();

        _resources.Remove(handle.id, handle.generation);
    }

    public Identifier<Mesh> AddMesh(ref readonly Mesh mesh)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var id = new Identifier<Mesh>(_meshDatas.Count);
        _meshDatas.Add(new()
        {
            value = mesh,
            isValid = true
        });

        return id;
    }

    public bool HasMesh(Identifier<Mesh> id)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return id.value >= 0 && id.value < _meshDatas.Count && _meshDatas[id.value].isValid;
    }

    public Mesh GetMesh(Identifier<Mesh> id)
    {
        if (!HasMesh(id))
        {
            throw new ArgumentOutOfRangeException(nameof(id), $"Mesh ID {id.value} is out of range.");
        }

        return _meshDatas[id.value].value;
    }

    public ref Mesh GetMeshReference(Identifier<Mesh> id)
    {
        if (!HasMesh(id))
        {
            throw new ArgumentOutOfRangeException(nameof(id), $"Mesh ID {id.value} is out of range.");
        }

        return ref _meshDatas[id.value].value;
    }

    public void RemoveMesh(Identifier<Mesh> id)
    {
        if (!HasMesh(id))
        {
            return;
        }

        ref var meshSlot = ref _meshDatas[id.value];
        if (!meshSlot.isValid)
        {
            return;
        }

        ref var mesh = ref meshSlot.value;
        mesh.ReleaseCpuResources();

        RemoveResource(mesh.vertexBuffer.ResourceHandle);
        RemoveResource(mesh.indexBuffer.ResourceHandle);

        _descriptorAllocator.Release(mesh.vertexBuffer.BindlessDescriptor);
        _descriptorAllocator.Release(mesh.indexBuffer.BindlessDescriptor);
    }

    public Identifier<Shader> AddShader(ref readonly Shader shader)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var id = new Identifier<Shader>(_shaders.Count);
        _shaders.Add(new()
        {
            value = shader,
            isValid = true
        });

        return id;
    }

    public bool HasShader(Identifier<Shader> id)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return id.value >= 0 && id.value < _shaders.Count && _shaders[id.value].isValid;
    }

    public Shader GetShader(Identifier<Shader> id)
    {
        if (!HasShader(id))
        {
            throw new ArgumentOutOfRangeException(nameof(id), $"Shader ID {id.value} is out of range.");
        }

        return _shaders[id.value].value;
    }

    public ref Shader GetShaderReference(Identifier<Shader> id)
    {
        if (!HasShader(id))
        {
            throw new ArgumentOutOfRangeException(nameof(id), $"Shader ID {id.value} is out of range.");
        }

        return ref _shaders[id.value].value;
    }

    public void RemoveShader(Identifier<Shader> id)
    {
        if (!HasShader(id))
        {
            return;
        }

        ref var shader = ref _shaders[id.value];

        shader.value.Dispose();
        shader.value = default;
        shader.isValid = false;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

#if DEBUG
        if (_resources.Count > 0)
        {
            throw new InvalidOperationException($"ResourceAllocator is being disposed with {_resources.Count} allocations still registered. Ensure all resources are released before disposing.");
        }

        if (_meshDatas.Count > 0)
        {
            throw new InvalidOperationException($"ResourceAllocator is being disposed with {_meshDatas.Count} meshes still registered. Ensure all meshes are released before disposing.");
        }

        if (_shaders.Count > 0)
        {
            throw new InvalidOperationException($"ResourceAllocator is being disposed with {_shaders.Count} shaders still registered. Ensure all shaders are released before disposing.");
        }
#endif

        _resources.Dispose();

        foreach (var mesh in _meshDatas)
        {
            if (!mesh.isValid)
            {
                continue;
            }

            mesh.value.ReleaseCpuResources();

            RemoveResource(mesh.value.vertexBuffer.ResourceHandle);
            RemoveResource(mesh.value.indexBuffer.ResourceHandle);

            _descriptorAllocator.Release(mesh.value.vertexBuffer.BindlessDescriptor);
            _descriptorAllocator.Release(mesh.value.indexBuffer.BindlessDescriptor);
        }

        foreach (var shader in _shaders)
        {
            if (!shader.isValid)
            {
                continue;
            }

            shader.value.Dispose();
        }

        _disposed = true;

        GC.SuppressFinalize(this);
    }
}