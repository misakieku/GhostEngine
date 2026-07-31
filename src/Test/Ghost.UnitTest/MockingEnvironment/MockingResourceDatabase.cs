using Ghost.Core;
using Ghost.Graphics.RHI;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.UnitTest.MockingEnvironment;

internal unsafe class MockingResourceDatabase : IResourceDatabase
{
    internal struct MockResourceRecord()
    {
        public ResourceDesc desc;
        public ResourceBarrierData barrierData;
        public string? name;
        public int refCount = 1;
        public bool isShared;
        public void* mappedData;
    }

    private readonly Dictionary<ulong, MockResourceRecord> _resources = new(128);
    private readonly Dictionary<Identifier<Sampler>, SamplerDesc> _samplers = new(128);
    private int _nextToken = 0;
    private int _samplerToken = 0;

    private static ulong GetKey(Handle<GPUResource> handle) => ((ulong)handle.Generation << 32) | (uint)handle.ID;

    public Handle<GPUResource> AddMockResource(ResourceDesc desc, ResourceBarrierData barrierData, string? name)
    {
        var id = Interlocked.Increment(ref _nextToken);
        var generation = 1;
        var handle = new Handle<GPUResource>(id, generation);

        _resources.TryAdd(GetKey(handle), new MockResourceRecord
        {
            desc = desc,
            barrierData = barrierData,
            name = name
        });

        return handle;
    }

    public Identifier<Sampler> AddSampler(scoped in SamplerDesc desc, int id)
    {
        var newId = new Identifier<Sampler>(id);
        _samplers.TryAdd(newId, desc);
        return newId;
    }

    public Handle<GPUResource> CreateShared(Handle<GPUResource> src)
    {
        ref var record = ref CollectionsMarshal.GetValueRefOrNullRef(_resources, GetKey(src));
        if (!Unsafe.IsNullRef(ref record))
        {
            record.refCount++;
            record.isShared = true;

            // To simulate sharing, we create a new handle mapping to the same conceptual resource.
            // For simplicity, we just clone the dict entry with a new ID
            var id = Interlocked.Increment(ref _nextToken);
            var generation = 1;
            var handle = new Handle<GPUResource>(id, generation);

            _resources.TryAdd(GetKey(handle), record);
            return handle;
        }

        return Handle<GPUResource>.Invalid;
    }

    public Handle<GPUResource> CreateEmpty()
    {
        var id = Interlocked.Increment(ref _nextToken);
        var generation = 1;
        var handle = new Handle<GPUResource>(id, generation);

        _resources.TryAdd(GetKey(handle), new MockResourceRecord());

        return handle;
    }

    public uint GetBindlessIndex(Handle<GPUResource> handle, BindlessAccess access = BindlessAccess.ShaderResource)
    {
        // Mock bindless index
        return (uint)handle.ID;
    }

    public ulong GetIntermediateResourceSize(Handle<GPUResource> resource, uint firstSubResource, uint numSubResources)
    {
        return 0; // For testing, we can return 0 because we don't actually allocate memory.
    }

    public Result<ResourceBarrierData, Error> GetResourceBarrierData(Handle<GPUResource> handle)
    {
        ref var record = ref CollectionsMarshal.GetValueRefOrNullRef(_resources, GetKey(handle));
        if (!Unsafe.IsNullRef(ref record))
            return record.barrierData;
        return Error.NotFound;
    }

    public Result<ResourceDesc, Error> GetResourceDescription(Handle<GPUResource> handle)
    {
        ref var record = ref CollectionsMarshal.GetValueRefOrNullRef(_resources, GetKey(handle));
        if (!Unsafe.IsNullRef(ref record))
            return record.desc;
        return Error.NotFound;
    }

    public string? GetResourceName(Handle<GPUResource> handle)
    {
        ref var record = ref CollectionsMarshal.GetValueRefOrNullRef(_resources, GetKey(handle));
        if (!Unsafe.IsNullRef(ref record))
            return record.name;
        return null;
    }

    public bool HasResource(Handle<GPUResource> handle)
    {
        return _resources.ContainsKey(GetKey(handle));
    }

    public void* MapResource(Handle<GPUResource> handle, uint subResource, ResourceRange? readRange)
    {
        ref var record = ref CollectionsMarshal.GetValueRefOrNullRef(_resources, GetKey(handle));
        if (!Unsafe.IsNullRef(ref record))
        {
            return null;
        }

        if (record.mappedData == null)
        {
            var size = record.desc.Type == ResourceType.Buffer ? Math.Max(1UL, record.desc.BufferDescriptor.Size) : 1UL;
            record.mappedData = NativeMemory.Alloc((nuint)size);
        }

        return record.mappedData;
    }

    public void ReleaseResource(Handle<GPUResource> handle)
    {
        ReleaseResourceImmediately(handle); // Simplified for testing
    }

    public void ReleaseResourceImmediately(Handle<GPUResource> handle)
    {
        ref var record = ref CollectionsMarshal.GetValueRefOrNullRef(_resources, GetKey(handle));
        if (!Unsafe.IsNullRef(ref record))
        {
            record.refCount--;
            if (record.refCount <= 0)
            {
                if (record.mappedData != null)
                {
                    NativeMemory.Free(record.mappedData);
                    record.mappedData = null;
                }

                _resources.Remove(GetKey(handle), out _);
            }
        }
    }

    public void ReleaseSampler(Identifier<Sampler> id)
    {
        _samplers.Remove(id, out _);
    }

    public Handle<GPUResource> Replace(Handle<GPUResource> dst, Handle<GPUResource> src)
    {
        if (_resources.TryGetValue(GetKey(dst), out var recordDst) &&
            _resources.TryGetValue(GetKey(src), out var recordSrc))
        {
            _resources[GetKey(dst)] = recordSrc;
            _resources[GetKey(src)] = recordDst;
        }

        ReleaseResource(src);
        return dst;
    }

    public Error SetResourceBarrierData(Handle<GPUResource> handle, ResourceBarrierData data)
    {
        ref var record = ref CollectionsMarshal.GetValueRefOrNullRef(_resources, GetKey(handle));
        if (!Unsafe.IsNullRef(ref record))
        {
            record.barrierData = data;

            return Error.None;
        }

        return Error.NotFound;
    }

    public Error Swap(Handle<GPUResource> handleA, Handle<GPUResource> handleB)
    {
        if (_resources.TryGetValue(GetKey(handleA), out var recordA) &&
            _resources.TryGetValue(GetKey(handleB), out var recordB))
        {
            _resources[GetKey(handleA)] = recordB;
            _resources[GetKey(handleB)] = recordA;
            return Error.None;
        }
        return Error.NotFound;
    }

    public bool TryGetSampler(scoped in SamplerDesc desc, out Identifier<Sampler> id)
    {
        foreach (var kvp in _samplers)
        {
            // Simple generic mock check
            id = kvp.Key;
            return true;
        }
        id = default;
        return false;
    }

    public Error UnmapResource(Handle<GPUResource> handle, uint subResource, ResourceRange? writtenRange)
    {
        return Error.None;
    }

    public void Reset()
    {
        foreach (var kvp in _resources)
        {
            if (kvp.Value.mappedData != null)
            {
                NativeMemory.Free(kvp.Value.mappedData);
            }
        }

        _resources.Clear();
        _samplers.Clear();
    }

    public void Dispose()
    {
        foreach (var record in _resources.Values)
        {
            if (record.mappedData != null)
            {
                NativeMemory.Free(record.mappedData);
            }
        }

        _resources.Clear();
        _samplers.Clear();
    }
}
