using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;

namespace Ghost.Entities;

internal sealed unsafe class SharedComponentStore : IDisposable
{
    private struct EntryInfo
    {
        public int refCount;
        public int hashCode;
        public int version;
        public int nextFree; // free-list linkage (index)
    }

    private struct TypeStore : IDisposable
    {
        public int typeSize;
        public UnsafeList<byte> data;         // raw bytes, stride = TypeSize
        public UnsafeList<EntryInfo> infos;   // parallel to Data entries (Entry 0 reserved)
        public UnsafeHashMap<long, int> hashLookup; // (hashKey) -> entryIndex
        public int freeListHead; // head index, 0 means none (we'll use Infos[0].NextFree too if you prefer)
        public int versionCounter;

        public void Dispose()
        {
            data.Dispose();
            infos.Dispose();
            hashLookup.Dispose();
        }
    }

    private UnsafeHashMap<int, TypeStore> _perType; // componentTypeId -> TypeStore // TODO: UnsafeMultiHashMap
    private bool _disposed;

    public SharedComponentStore(int initialCapacity = 16)
    {
        _perType = new UnsafeHashMap<int, TypeStore>(initialCapacity, AllocationHandle.Persistent);
    }

    ~SharedComponentStore()
    {
        Dispose();
    }

    public int InsertOrGet(int componentTypeId, int typeSize, void* data, int hashCode)
    {
        // Reserve index 0 for "default"
        if (data == null)
        {
            return 0;
        }

        ref var store = ref GetOrCreateTypeStore(componentTypeId, typeSize);

        // Combine (typeId, hash) into a single key; collisions handled by memcmp below.
        var key = ((long)componentTypeId << 32) ^ (uint)hashCode;

        if (store.hashLookup.TryGetValue(key, out var existingIndex))
        {
            var existingPtr = (byte*)store.data.GetUnsafePtr() + (existingIndex * store.typeSize);
            if (new Span<byte>(existingPtr, store.typeSize).SequenceEqual(new Span<byte>(data, store.typeSize)))
            {
                ((EntryInfo*)store.infos.GetUnsafePtr())[existingIndex].refCount++;
                return existingIndex;
            }
            // If collision: fall through to insert (you may want a secondary structure).
        }

        var index = AllocateEntry(ref store);

        var dst = (byte*)store.data.GetUnsafePtr() + (index * store.typeSize);
        MemoryUtility.MemCpy(dst, data, (nuint)store.typeSize);

        store.infos[index] = new EntryInfo
        {
            refCount = 1,
            hashCode = hashCode,
            version = ++store.versionCounter,
            nextFree = -1
        };

        store.hashLookup[key] = index;
        return index;
    }

    public RefResult<T, Error> GetComponentData<T>(int componentTypeId, int index)
        where T : unmanaged
    {
        if (index == 0)
        {
            return Error.InvalidArgument;
        }

        ref var store = ref _perType.GetValueRef(componentTypeId, out var exist);
        if (!exist || index >= store.infos.Count)
        {
            return Error.NotFound;
        }

        var info = store.infos[index];
        if (info.refCount <= 0)
        {
            return Error.NotFound;
        }

        var dataPtr = (byte*)store.data.GetUnsafePtr() + (index * store.typeSize);
        return new RefResult<T, Error>(ref *(T*)dataPtr, Error.Success);
    }

    public Error AddRef(int componentTypeId, int index)
    {
        if (index == 0)
        {
            return Error.Success;
        }

        ref var store = ref _perType.GetValueRef(componentTypeId, out var exist);
        if (!exist)
        {
            return Error.NotFound;
        }

        store.infos[index].refCount++;

        return Error.Success;
    }

    public void Release(int componentTypeId, int index)
    {
        if (index == 0)
        {
            return;
        }

        ref var store = ref _perType.GetValueRef(componentTypeId, out var exist);
        if (!exist)
        {
            return;
        }

        ref var info = ref store.infos[index];
        info.refCount--;
        if (info.refCount > 0)
        {
            return;
        }

        // Remove from hash lookup (best-effort; collisions require more robust handling)
        var key = ((long)componentTypeId << 32) ^ (uint)info.hashCode;
        store.hashLookup.Remove(key);

        // Push to free-list
        info.nextFree = store.freeListHead;
        store.freeListHead = index;
    }

    public void* GetDataPtr(int componentTypeId, int index)
    {
        if (index == 0)
        {
            return null;
        }

        ref var store = ref _perType.GetValueRef(componentTypeId, out var exist);
        if (!exist)
        {
            return null;
        }

        return (byte*)store.data.GetUnsafePtr() + (index * store.typeSize);
    }

    private ref TypeStore GetOrCreateTypeStore(int componentTypeId, int typeSize)
    {
        ref var existing = ref _perType.GetValueRef(componentTypeId, out var exist);
        if (exist)
        {
            return ref existing;
        }

        var store = new TypeStore
        {
            typeSize = typeSize,
            data = new UnsafeList<byte>(typeSize * 16, AllocationHandle.Persistent),
            infos = new UnsafeList<EntryInfo>(16, AllocationHandle.Persistent),
            hashLookup = new UnsafeHashMap<long, int>(16, AllocationHandle.Persistent),
            freeListHead = 0,
            versionCounter = 0
        };

        // Create reserved default entry at index 0
        store.data.Resize(typeSize); // one element worth of bytes
        store.infos.Add(new EntryInfo { refCount = int.MaxValue, hashCode = 0, version = 0, nextFree = -1 });

        _perType.Add(componentTypeId, store);

        existing = ref _perType.GetValueRef(componentTypeId, out exist);
        Logger.DebugAssert(exist);

        return ref existing;
    }

    private static int AllocateEntry(ref TypeStore store)
    {
        if (store.freeListHead != 0)
        {
            var idx = store.freeListHead;
            store.freeListHead = store.infos[idx].nextFree;
            store.infos[idx].nextFree = -1;
            return idx;
        }

        var newIndex = store.infos.Count;
        store.infos.Add(default);

        var newByteCount = (newIndex + 1) * store.typeSize;
        store.data.Resize(newByteCount);

        return newIndex;
    }

    public void Dispose()
    {
        foreach (var kvp in _perType)
        {
            kvp.Value.Dispose();
        }

        _perType.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
