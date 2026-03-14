using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;

namespace Ghost.Graphics.Core;

public readonly struct MaterialPaletteInfo
{
    public int MaterialCount
    {
        get;
        init;
    }
}

internal sealed class MaterialPaletteStore : IDisposable
{
    private struct Entry : IDisposable
    {
        public UnsafeList<Handle<Material>> materials;
        public int refCount;
        public ulong lookupHash;
        public int nextFree;

        public readonly bool IsAllocated => materials.IsCreated;
        public readonly bool IsActive => refCount > 0 && materials.IsCreated;

        public void Dispose()
        {
            materials.Dispose();
        }
    }

    private UnsafeList<Entry> _entries;
    private UnsafeHashMap<ulong, int> _lookup;
    private int _freeListHead;
    private bool _disposed;

    public MaterialPaletteStore(int initialCapacity = 16)
    {
        if (initialCapacity <= 0)
        {
            initialCapacity = 16;
        }

        _entries = new UnsafeList<Entry>(initialCapacity + 1, Allocator.Persistent);
        _lookup = new UnsafeHashMap<ulong, int>(initialCapacity * 2, Allocator.Persistent);
        _freeListHead = 0;

        _entries.Add(new Entry
        {
            materials = default,
            refCount = int.MaxValue,
            lookupHash = 0,
            nextFree = -1,
        });
    }

    ~MaterialPaletteStore()
    {
        Dispose();
    }

    private int AllocateEntry()
    {
        if (_freeListHead != 0)
        {
            int index = _freeListHead;
            ref var entry = ref _entries[index];
            _freeListHead = entry.nextFree;
            entry.nextFree = -1;
            return index;
        }

        int newIndex = _entries.Count;
        _entries.Add(default);
        return newIndex;
    }

    private static ulong ComputeLookupHash(ReadOnlySpan<Handle<Material>> materials, ulong seed)
    {
        const ulong offset = 14695981039346656037UL;

        ulong hash = offset ^ seed;
        hash = Mix(hash, (ulong)materials.Length);

        foreach (var material in materials)
        {
            hash = Mix(hash, (uint)material.ID);
            hash = Mix(hash, (uint)material.Generation);
        }

        return hash;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Mix(ulong hash, ulong value)
    {
        const ulong prime = 1099511628211UL;
        hash ^= value;
        hash *= prime;
        return hash;
    }

    public int InsertOrGet(ReadOnlySpan<Handle<Material>> materials)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (materials.Length == 0)
        {
            return 0;
        }

        ulong hash = ComputeLookupHash(materials, 0);
        while (_lookup.TryGetValue(hash, out var existingIndex))
        {
            ref var entry = ref _entries[existingIndex];
            if (entry.IsActive && materials.SequenceEqual(entry.materials.AsSpan()))
            {
                entry.refCount++;
                return existingIndex;
            }

            hash = ComputeLookupHash(materials, hash);
        }

        int index = AllocateEntry();
        ref var newEntry = ref _entries[index];
        newEntry.lookupHash = hash;
        newEntry.refCount = 1;
        newEntry.nextFree = -1;

        if (!newEntry.materials.IsCreated)
        {
            newEntry.materials = new UnsafeList<Handle<Material>>(materials.Length, Allocator.Persistent);
        }
        else
        {
            newEntry.materials.Clear();
        }

        for (int i = 0; i < materials.Length; i++)
        {
            newEntry.materials.Add(materials[i]);
        }
        _lookup.Add(hash, index);

        return index;
    }

    public bool IsValid(int paletteIndex)
    {
        if (paletteIndex == 0)
        {
            return true;
        }

        return paletteIndex > 0
            && paletteIndex < _entries.Count
            && _entries[paletteIndex].IsActive;
    }

    public MaterialPaletteInfo GetInfo(int paletteIndex)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!IsValid(paletteIndex))
        {
            return default;
        }

        if (paletteIndex == 0)
        {
            return default;
        }

        return new MaterialPaletteInfo
        {
            MaterialCount = _entries[paletteIndex].materials.Count,
        };
    }

    public Handle<Material> GetMaterial(int paletteIndex, int localMaterialIndex)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!IsValid(paletteIndex) || paletteIndex == 0)
        {
            return Handle<Material>.Invalid;
        }

        ref var entry = ref _entries[paletteIndex];
        if ((uint)localMaterialIndex >= (uint)entry.materials.Count)
        {
            return Handle<Material>.Invalid;
        }

        return entry.materials[localMaterialIndex];
    }

    public void Release(int paletteIndex)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (paletteIndex == 0 || !IsValid(paletteIndex))
        {
            return;
        }

        ref var entry = ref _entries[paletteIndex];
        entry.refCount--;
        if (entry.refCount > 0)
        {
            return;
        }

        _lookup.Remove(entry.lookupHash);
        entry.materials.Clear();
        entry.nextFree = _freeListHead;
        _freeListHead = paletteIndex;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        for (int i = 0; i < _entries.Count; i++)
        {
            _entries[i].Dispose();
        }

        _entries.Dispose();
        _lookup.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
