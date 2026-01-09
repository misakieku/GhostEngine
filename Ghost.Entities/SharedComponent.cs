#if false // FIX: API update in Misaki.HighPerformance.LowLevel.Collections require me to disable this for now.
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;

namespace Ghost.Entities;

public interface ISharedComponent
{
}

internal unsafe sealed class SharedComponentStore : IDisposable
{
    private struct EntryInfo
    {
        public int RefCount;
        public int HashCode;
        public int Version;
        public int NextFree; // free-list linkage (index)
    }

    private struct TypeStore : IDisposable
    {
        public int TypeSize;
        public UnsafeList<byte> Data;         // raw bytes, stride = TypeSize
        public UnsafeList<EntryInfo> Infos;   // parallel to Data entries (Entry 0 reserved)
        public UnsafeHashMap<long, int> HashLookup; // (hashKey) -> entryIndex
        public int FreeListHead; // head index, 0 means none (we'll use Infos[0].NextFree too if you prefer)
        public int VersionCounter;

        public void Dispose()
        {
            Data.Dispose();
            Infos.Dispose();
            HashLookup.Dispose();
        }
    }

    private readonly UnsafeHashMap<int, TypeStore> _perType; // componentTypeId -> TypeStore

    public SharedComponentStore(int initialCapacity = 16)
    {
        _perType = new UnsafeHashMap<int, TypeStore>(initialCapacity, Allocator.Persistent);
    }

    public void Dispose()
    {
        foreach (var kvp in _perType)
        {
            kvp.Value.Dispose();
        }

        _perType.Dispose();
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

        if (store.HashLookup.TryGetValue(key, out var existingIndex))
        {
            var existingPtr = (byte*)store.Data.GetUnsafePtr() + (existingIndex * store.TypeSize);
            if (new Span<byte>(existingPtr, store.TypeSize).SequenceEqual(new Span<byte>(data, store.TypeSize)))
            {
                ((EntryInfo*)store.Infos.GetUnsafePtr())[existingIndex].RefCount++;
                return existingIndex;
            }
            // If collision: fall through to insert (you may want a secondary structure).
        }

        int index = AllocateEntry(ref store);

        var dst = (byte*)store.Data.GetUnsafePtr() + (index * store.TypeSize);
        MemoryUtility.MemCpy(dst, data, (nuint)store.TypeSize);

        store.Infos[index] = new EntryInfo
        {
            RefCount = 1,
            HashCode = hashCode,
            Version = ++store.VersionCounter,
            NextFree = -1
        };

        store.HashLookup[key] = index;
        return index;
    }

    public void AddRef(int componentTypeId, int index)
    {
        if (index == 0) return;
        ref var store = ref _perType[componentTypeId];
        store.Infos[index].RefCount++;
    }

    public void Release(int componentTypeId, int index)
    {
        if (index == 0) return;
        ref var store = ref _perType.GetValueByKey(componentTypeId);

        ref var info = ref store.Infos.Ptr[index];
        info.RefCount--;
        if (info.RefCount > 0) return;

        // Remove from hash lookup (best-effort; collisions require more robust handling)
        long key = ((long)componentTypeId << 32) ^ (uint)info.HashCode;
        store.HashLookup.Remove(key);

        // Push to free-list
        info.NextFree = store.FreeListHead;
        store.FreeListHead = index;
    }

    public void* GetDataPtr(int componentTypeId, int index)
    {
        if (index == 0) return null;
        ref var store = ref _perType.GetValueByKey(componentTypeId);
        return (byte*)store.Data.Ptr + (index * store.TypeSize);
    }

    private ref TypeStore GetOrCreateTypeStore(int componentTypeId, int typeSize)
    {
        if (_perType.TryGetValue(componentTypeId, out var existing))
        {
            // UnsafeHashMap returns by value in some implementations; you may need a different pattern here.
            // Adjust to your container API (e.g., TryGetValueRef).
        }

        var store = new TypeStore
        {
            TypeSize = typeSize,
            Data = new UnsafeList<byte>(typeSize * 16, Allocator.Persistent),
            Infos = new UnsafeList<EntryInfo>(16, Allocator.Persistent),
            HashLookup = new UnsafeHashMap<long, int>(16, Allocator.Persistent),
            FreeListHead = 0,
            VersionCounter = 0
        };

        // Create reserved default entry at index 0
        store.Data.Resize(typeSize); // one element worth of bytes
        store.Infos.Add(new EntryInfo { RefCount = int.MaxValue, HashCode = 0, Version = 0, NextFree = -1 });

        _perType.Add(componentTypeId, store);
        // NOTE: returning a ref requires a "get ref" API; adjust to your UnsafeHashMap capabilities.
        return ref _perType.GetValueByKey(componentTypeId);
    }

    private static int AllocateEntry(ref TypeStore store)
    {
        if (store.FreeListHead != 0)
        {
            int idx = store.FreeListHead;
            store.FreeListHead = store.Infos[idx].NextFree;
            store.Infos[idx].NextFree = -1;
            return idx;
        }

        int newIndex = store.Infos.Count;
        store.Infos.Add(default);

        int newByteCount = (newIndex + 1) * store.TypeSize;
        store.Data.Resize(newByteCount);

        return newIndex;
    }
}

#endif
