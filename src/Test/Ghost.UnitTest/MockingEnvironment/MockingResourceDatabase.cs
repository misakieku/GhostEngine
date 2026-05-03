using Ghost.Core;
using Ghost.Graphics.RHI;
using System.Collections.Concurrent;

namespace Ghost.UnitTest.MockingEnvironment;

internal unsafe class MockingResourceDatabase : IResourceDatabase
{
    internal class MockResourceRecord
    {
        public ResourceDesc desc;
        public ResourceBarrierData barrierData;
        public string? name;
        public int refCount = 1;
        public bool isShared;
    }

    private readonly ConcurrentDictionary<ulong, MockResourceRecord> _resources = new();
    private readonly ConcurrentDictionary<Identifier<Sampler>, SamplerDesc> _samplers = new();
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

    public Identifier<Sampler> AddSampler(ref readonly SamplerDesc desc, int id)
    {
        var newId = new Identifier<Sampler>(id);
        _samplers.TryAdd(newId, desc);
        return newId;
    }

    public Handle<GPUResource> CreateShared(Handle<GPUResource> src)
    {
        if (_resources.TryGetValue(GetKey(src), out var record))
        {
            lock (record)
            {
                record.refCount++;
                record.isShared = true;
            }

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
        if (_resources.TryGetValue(GetKey(handle), out var record))
            return record.barrierData;
        return Error.NotFound;
    }

    public Result<ResourceDesc, Error> GetResourceDescription(Handle<GPUResource> handle)
    {
        if (_resources.TryGetValue(GetKey(handle), out var record))
            return record.desc;
        return Error.NotFound;
    }

    public string? GetResourceName(Handle<GPUResource> handle)
    {
        if (_resources.TryGetValue(GetKey(handle), out var record))
            return record.name;
        return null;
    }

    public bool HasResource(Handle<GPUResource> handle)
    {
        return _resources.ContainsKey(GetKey(handle));
    }

    public void* MapResource(Handle<GPUResource> handle, uint subResource, ResourceRange? readRange)
    {
        // Real pointers are tricky in mocks unless native mem is allocated.
        // Usually unit tests don't do CPU readbacks directly on the raw pointer unless necessary.
        throw new NotSupportedException("MapResource is not supported in MockingResourceDatabase. Use a custom mechanism for tests.");
    }

    public void ReleaseResource(Handle<GPUResource> handle)
    {
        ReleaseResourceImmediately(handle); // Simplified for testing
    }

    public void ReleaseResourceImmediately(Handle<GPUResource> handle)
    {
        if (_resources.TryGetValue(GetKey(handle), out var record))
        {
            lock (record)
            {
                record.refCount--;
                if (record.refCount <= 0)
                {
                    _resources.TryRemove(GetKey(handle), out _);
                }
            }
        }
    }

    public void ReleaseSampler(Identifier<Sampler> id)
    {
        _samplers.TryRemove(id, out _);
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
        if (_resources.TryGetValue(GetKey(handle), out var record))
        {
            lock (record)
            {
                record.barrierData = data;
            }

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

    public bool TryGetSampler(ref readonly SamplerDesc desc, out Identifier<Sampler> id)
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

    public void Dispose()
    {
        _resources.Clear();
        _samplers.Clear();
    }
}
