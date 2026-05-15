using Ghost.Core;
using Ghost.Graphics.Core;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;

namespace Ghost.Graphics.Services;

public readonly struct MaterialPalette
{
    public readonly ReadOnlyView<Handle<Material>> materials;

    public unsafe MaterialPalette(Handle<Material>* pMaterials, int count)
    {
        materials = new ReadOnlyView<Handle<Material>>(pMaterials, count);
    }

    public MaterialPalette(ReadOnlyView<Handle<Material>> materials)
    {
        this.materials = materials;
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
        /// <summary> First element index in _materialIndices for this palette. </summary>
        public int indicesOffset;

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

    // Deferred-release queue: slots are not reclaimed until the GPU has finished
    // reading them (same pattern as ResourceManager.ResourceReturnEntry).
    private struct PendingFreeSlot
    {
        public int slotIndex;
        public ulong releaseFrame;
    }

    private UnsafeQueue<PendingFreeSlot> _pendingFreeSlots;
    private ulong _currentFrame;

    // ── CPU-side GPU buffer mirrors ──────────────────────────────────────────
    // Index 0 is always reserved (palette 0 = no palette / empty), so both
    // lists are pre-seeded with one sentinel entry each.

    /// <summary> One uint per palette entry: base offset into <see cref="_materialIndices"/>. </summary>
    private UnsafeList<uint> _paletteOffsets;

    /// <summary> Packed bindless CBuffer descriptor indices for all palettes, contiguous. </summary>
    private UnsafeList<uint> _materialIndices;

    // Dirty ranges for incremental GPU upload.
    private int _dirtyOffsetStart;
    private int _dirtyOffsetEnd;
    private int _dirtyIndicesStart;
    private int _dirtyIndicesEnd;
    private bool _gpuDirty;

    /// <summary>
    /// Returns true if any palette data has changed since the last <see cref="ClearDirty"/> call.
    /// </summary>
    public bool IsGpuDirty => _gpuDirty;

    /// <summary>
    /// Returns the CPU-side palette offset array (one uint per palette slot).
    /// Only valid to read on the render thread after main-thread work is complete.
    /// </summary>
    public ReadOnlySpan<uint> PaletteOffsets => _paletteOffsets.AsSpan();

    /// <summary>
    /// Returns the CPU-side packed material bindless index array.
    /// Entries are 0 until <see cref="ResolveMaterialIndices"/> is called.
    /// </summary>
    public ReadOnlySpan<uint> MaterialIndices => _materialIndices.AsSpan();

    public MaterialPaletteStore(int initialCapacity = 16)
    {
        if (initialCapacity <= 0)
        {
            initialCapacity = 16;
        }

        _entries = new UnsafeList<Entry>(initialCapacity + 1, AllocationHandle.Persistent);
        _lookup = new UnsafeHashMap<ulong, int>(initialCapacity * 2, AllocationHandle.Persistent);
        _freeListHead = 0;

        _paletteOffsets = new UnsafeList<uint>(initialCapacity + 1, AllocationHandle.Persistent);
        _materialIndices = new UnsafeList<uint>(initialCapacity * 4, AllocationHandle.Persistent);
        _pendingFreeSlots = new UnsafeQueue<PendingFreeSlot>(16, AllocationHandle.Persistent);

        // Slot 0 is reserved (empty palette). Seed both lists so indices stay in sync.
        _paletteOffsets.Add(0); // palette 0 offset = 0
        _materialIndices.Add(0); // placeholder, never read for palette 0

        _dirtyOffsetStart = int.MaxValue;
        _dirtyOffsetEnd = 0;
        _dirtyIndicesStart = int.MaxValue;
        _dirtyIndicesEnd = 0;
        _gpuDirty = false;
    }

    ~MaterialPaletteStore()
    {
        Dispose();
    }

    private int AllocateEntry()
    {
        if (_freeListHead != 0)
        {
            var index = _freeListHead;
            ref var entry = ref _entries[index];
            _freeListHead = entry.nextFree;
            entry.nextFree = -1;
            return index;
        }

        var newIndex = _entries.Count;
        _entries.Add(default);
        return newIndex;
    }

    private static ulong ComputeLookupHash(ReadOnlySpan<Handle<Material>> materials, ulong seed)
    {
        const ulong offset = 14695981039346656037UL;

        var hash = offset ^ seed;
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

        var hash = ComputeLookupHash(materials, 0);
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

        var index = AllocateEntry();
        ref var newEntry = ref _entries[index];
        newEntry.lookupHash = hash;
        newEntry.refCount = 1;
        newEntry.nextFree = -1;

        if (!newEntry.materials.IsCreated)
        {
            newEntry.materials = new UnsafeList<Handle<Material>>(materials.Length, AllocationHandle.Persistent);
        }
        else
        {
            newEntry.materials.Clear();
        }

        for (var i = 0; i < materials.Length; i++)
        {
            newEntry.materials.Add(materials[i]);
        }
        _lookup.Add(hash, index);

        // Record where in _materialIndices this palette's slots begin.
        newEntry.indicesOffset = _materialIndices.Count;

        // Ensure _paletteOffsets is large enough (index may be a recycled slot).
        while (_paletteOffsets.Count <= index)
        {
            _paletteOffsets.Add(0);
        }

        _paletteOffsets[index] = (uint)newEntry.indicesOffset;

        // Append placeholder indices (0 = unresolved). ResolveMaterialIndices
        // will overwrite these with real bindless indices before GPU upload.
        for (var i = 0; i < materials.Length; i++)
        {
            _materialIndices.Add(0);
        }

        // Mark dirty ranges.
        _dirtyOffsetStart = Math.Min(_dirtyOffsetStart, index);
        _dirtyOffsetEnd = Math.Max(_dirtyOffsetEnd, index + 1);
        _dirtyIndicesStart = Math.Min(_dirtyIndicesStart, newEntry.indicesOffset);
        _dirtyIndicesEnd = Math.Max(_dirtyIndicesEnd, newEntry.indicesOffset + materials.Length);
        _gpuDirty = true;

        return index;
    }

    public bool IsValid(Identifier<MaterialPalette> paletteID)
    {
        var paletteIndex = paletteID.Value;
        return paletteIndex > 0
            && paletteIndex < _entries.Count
            && _entries[paletteIndex].IsActive;
    }

    public MaterialPalette GetInfo(Identifier<MaterialPalette> paletteID)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!IsValid(paletteID))
        {
            return default;
        }

        if (paletteID == 0)
        {
            return default;
        }

        return new MaterialPalette(_entries[paletteID].materials.AsReadOnly());
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

    public void Release(Identifier<MaterialPalette> paletteID)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (paletteID == 0 || !IsValid(paletteID))
        {
            return;
        }

        ref var entry = ref _entries[paletteID];
        entry.refCount--;
        if (entry.refCount > 0)
        {
            return;
        }

        // Remove from CPU lookup immediately — the slot is logically dead.
        _lookup.Remove(entry.lookupHash);
        entry.materials.Clear();

        // Do NOT push to _freeListHead yet. The GPU may still be in-flight reading
        // _paletteOffsets[slot] from previous frames. Queue for deferred reclaim.
        _pendingFreeSlots.Enqueue(new PendingFreeSlot
        {
            slotIndex = paletteID,
            releaseFrame = _currentFrame,
        });
    }

    /// <summary>
    /// Advances the frame counter and reclaims slots whose GPU reads have completed.
    /// Must be called from <c>ResourceManager.EndFrame(completedFrame)</c> with the
    /// same <paramref name="completedFrame"/> that was used to drain the resource return queue.
    /// A slot is safe to reuse when <c>completedFrame &gt; releaseFrame</c>, meaning
    /// all GPU command buffers that could have read the old palette entry have retired.
    /// </summary>
    public void EndFrame(ulong currentFrame, ulong completedFrame)
    {
        _currentFrame = currentFrame;

        while (_pendingFreeSlots.TryPeek(out var pending) && pending.releaseFrame < completedFrame)
        {
            _pendingFreeSlots.Dequeue();
            ref var entry = ref _entries[pending.slotIndex];
            entry.nextFree = _freeListHead;
            _freeListHead = pending.slotIndex;
        }
    }

    // ── GPU upload support ───────────────────────────────────────────────────

    /// <summary>
    /// Gets the dirty sub-ranges that need to be uploaded to the GPU.
    /// </summary>
    public void GetDirtyRanges(
        out int offsetStart, out int offsetEnd,
        out int indicesStart, out int indicesEnd)
    {
        offsetStart = _gpuDirty ? _dirtyOffsetStart : 0;
        offsetEnd = _gpuDirty ? _dirtyOffsetEnd : 0;
        indicesStart = _gpuDirty ? _dirtyIndicesStart : 0;
        indicesEnd = _gpuDirty ? _dirtyIndicesEnd : 0;
    }

    /// <summary>
    /// Resolves every active palette's material handles to GPU bindless CBuffer indices.
    /// Must be called on the render thread before uploading to GPU.
    /// </summary>
    /// <param name="resolveIndex">
    /// Delegate that maps a <see cref="Handle{Material}"/> to its CBuffer bindless descriptor heap
    /// index. Typically <c>mat => resourceDatabase.GetBindlessIndex(material.CBuffer)</c>.
    /// </param>
    /// <param name="state"> The state object to pass to the resolveIndex delegate. </param>
    public void ResolveMaterialIndices(Func<Handle<Material>, object?, uint> resolveIndex, object? state)
    {
        if (!_gpuDirty)
        {
            return;
        }

        for (var i = 1; i < _entries.Count; i++)
        {
            ref var entry = ref _entries[i];
            if (!entry.IsActive)
            {
                continue;
            }

            var baseOffset = entry.indicesOffset;
            for (var slot = 0; slot < entry.materials.Count; slot++)
            {
                _materialIndices[baseOffset + slot] = resolveIndex(entry.materials[slot], state);
            }
        }
    }

    /// <summary>
    /// Clears the dirty flag after GPU upload is complete.
    /// </summary>
    public void ClearDirty()
    {
        _dirtyOffsetStart = int.MaxValue;
        _dirtyOffsetEnd = 0;
        _dirtyIndicesStart = int.MaxValue;
        _dirtyIndicesEnd = 0;
        _gpuDirty = false;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        for (var i = 0; i < _entries.Count; i++)
        {
            _entries[i].Dispose();
        }

        _entries.Dispose();
        _lookup.Dispose();
        _paletteOffsets.Dispose();
        _materialIndices.Dispose();
        _pendingFreeSlots.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
