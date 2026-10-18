using Ghost.Engine.Volumes;
using Ghost.Entities;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.Components;

/// <summary>
/// Metadata describing an unmanaged component stored in a <see cref="RenderVolumeDataContainer"/>.
/// </summary>
public struct VolumeDataEntry
{
    public ushort typeId;
    public ushort offset;
    public ushort size;
}

/// <summary>
/// Unmanaged cleanup component that stores arbitrary volume component settings in a contiguous native buffer.
/// Survives entity destruction until disposed by <see cref="Ghost.Engine.Systems.RenderVolumeCleanupSystem"/>.
/// </summary>
public unsafe struct RenderVolumeDataContainer : ICleanupComponent, IDisposable
{
    private UnsafeList<byte> _dataBuffer;
    private UnsafeList<VolumeDataEntry> _entries;
    private bool _isCreated;

    public readonly bool IsCreated => _isCreated;
    public readonly int ComponentCount => _isCreated ? _entries.Count : 0;
    public readonly ReadOnlySpan<VolumeDataEntry> Entries => _isCreated ? _entries.AsSpan() : ReadOnlySpan<VolumeDataEntry>.Empty;

    public RenderVolumeDataContainer(int initialByteCapacity = 64)
        : this(initialByteCapacity, AllocationHandle.Persistent)
    {
    }

    public RenderVolumeDataContainer(int initialByteCapacity, AllocationHandle allocationHandle)
    {
        _dataBuffer = new UnsafeList<byte>(initialByteCapacity, allocationHandle);
        _entries = new UnsafeList<VolumeDataEntry>(4, allocationHandle);
        _isCreated = true;
    }

    public static RenderVolumeDataContainer Create()
    {
        return new RenderVolumeDataContainer(64, AllocationHandle.Persistent);
    }

    public static RenderVolumeDataContainer Create(AllocationHandle allocationHandle)
    {
        return new RenderVolumeDataContainer(64, allocationHandle);
    }

    private void EnsureInitialized()
    {
        if (!_isCreated)
        {
            _dataBuffer = new UnsafeList<byte>(64, AllocationHandle.Persistent);
            _entries = new UnsafeList<VolumeDataEntry>(4, AllocationHandle.Persistent);
            _isCreated = true;
        }
    }

    /// <summary>
    /// Adds or updates a volume component setting inside this container.
    /// </summary>
    /// <typeparam name="T">The volume component type.</typeparam>
    /// <param name="value">The component value to store.</param>
    public void Set<T>(in T value)
        where T : unmanaged, IVolumeComponent<T>
    {
        EnsureInitialized();
        VolumeBlenderTrampoline<T>.EnsureInitialized();

        var typeId = VolumeBlenderTrampoline<T>.TypeId;
        var size = VolumeBlenderTrampoline<T>.Size;
        var offset = -1;

        for (var i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].typeId == typeId)
            {
                offset = _entries[i].offset;
                break;
            }
        }

        if (offset == -1)
        {
            offset = _dataBuffer.Count;
            _dataBuffer.Resize(offset + size);
            _entries.Add(new VolumeDataEntry
            {
                typeId = typeId,
                offset = (ushort)offset,
                size = (ushort)size
            });
        }

        fixed (T* pValue = &value)
        {
            MemoryUtility.MemCpy((byte*)_dataBuffer.GetUnsafePtr() + offset, pValue, (nuint)size);
        }
    }

    /// <summary>
    /// Checks whether this container contains a setting for the specified component type.
    /// </summary>
    /// <typeparam name="T">The volume component type.</typeparam>
    /// <returns>True if the component exists in this container, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Has<T>()
        where T : unmanaged, IVolumeComponent<T>
    {
        return TryGetPtr(VolumeBlenderTrampoline<T>.TypeId, out _);
    }

    /// <summary>
    /// Attempts to retrieve a copy of the component data stored in this container.
    /// </summary>
    /// <typeparam name="T">The volume component type.</typeparam>
    /// <param name="value">The retrieved component data if found.</param>
    /// <returns>True if found, false otherwise.</returns>
    public readonly bool TryGet<T>(out T value)
        where T : unmanaged, IVolumeComponent<T>
    {
        if (!_isCreated)
        {
            value = default;
            return false;
        }

        var typeId = VolumeBlenderTrampoline<T>.TypeId;
        for (var i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].typeId == typeId)
            {
                ref readonly var entry = ref _entries[i];
                var ptr = (byte*)_dataBuffer.GetUnsafePtr() + entry.offset;
                value = *(T*)ptr;
                return true;
            }
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Attempts to retrieve a raw pointer to the component data stored in this container by type ID.
    /// </summary>
    /// <param name="typeId">The component type ID.</param>
    /// <param name="ptr">The unmanaged pointer to the component data.</param>
    /// <returns>True if found, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool TryGetPtr(ushort typeId, out void* ptr)
    {
        if (!_isCreated)
        {
            ptr = null;
            return false;
        }

        for (var i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].typeId == typeId)
            {
                ptr = (byte*)_dataBuffer.GetUnsafePtr() + _entries[i].offset;
                return true;
            }
        }

        ptr = null;
        return false;
    }

    /// <summary>
    /// Removes a volume component setting from this container if present.
    /// </summary>
    /// <typeparam name="T">The volume component type.</typeparam>
    /// <returns>True if removed, false if not present.</returns>
    public bool Remove<T>()
        where T : unmanaged, IVolumeComponent<T>
    {
        if (!_isCreated)
        {
            return false;
        }

        var typeId = VolumeBlenderTrampoline<T>.TypeId;
        for (var i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].typeId == typeId)
            {
                _entries.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Releases unmanaged buffer allocations.
    /// </summary>
    public void Dispose()
    {
        if (_isCreated)
        {
            _dataBuffer.Dispose();
            _entries.Dispose();
            _isCreated = false;
        }
    }
}
