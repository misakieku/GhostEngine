using Ghost.Engine.Components;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.Volumes;

/// <summary>
/// Contiguous, unmanaged heterogeneous storage for unmanaged component structs with guaranteed memory alignment.
/// </summary>
public unsafe struct UnmanagedTypeMap : IDisposable
{
    private UnsafeList<byte> _dataBuffer;
    private UnsafeList<VolumeDataEntry> _entries;
    private bool _isCreated;

    public readonly bool IsCreated => _isCreated;
    public readonly int ComponentCount => _isCreated ? _entries.Count : 0;
    public readonly ReadOnlySpan<VolumeDataEntry> Entries => _isCreated ? _entries.AsSpan() : ReadOnlySpan<VolumeDataEntry>.Empty;

    public UnmanagedTypeMap(int initialByteCapacity = 64)
        : this(initialByteCapacity, AllocationHandle.Persistent)
    {
    }

    public UnmanagedTypeMap(int initialByteCapacity, AllocationHandle allocationHandle)
    {
        _dataBuffer = new UnsafeList<byte>(initialByteCapacity, allocationHandle);
        _entries = new UnsafeList<VolumeDataEntry>(4, allocationHandle);
        _isCreated = true;
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
    /// Adds or updates an unmanaged component in the map with strict memory alignment.
    /// </summary>
    public void Set<T>(ushort typeId, in T value)
        where T : unmanaged
    {
        EnsureInitialized();

        var size = sizeof(T);
        var align = MemoryUtility.AlignOf<T>();

        for (var i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].typeId == typeId)
            {
                fixed (T* pValue = &value)
                {
                    MemoryUtility.MemCpy((byte*)_dataBuffer.GetUnsafePtr() + _entries[i].offset, pValue, (nuint)size);
                }
                return;
            }
        }

        var alignedOffset = (int)MemoryUtility.AlignUp((nuint)_dataBuffer.Count, align);
        var requiredCount = alignedOffset + size;
        if (_dataBuffer.Capacity < requiredCount)
        {
            _dataBuffer.Resize(requiredCount);
        }

        _dataBuffer.UnsafeSetCount(requiredCount);

        fixed (T* pValue = &value)
        {
            MemoryUtility.MemCpy((byte*)_dataBuffer.GetUnsafePtr() + alignedOffset, pValue, (nuint)size);
        }

        _entries.Add(new VolumeDataEntry
        {
            typeId = typeId,
            offset = (ushort)alignedOffset,
            size = (ushort)size
        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Has(ushort typeId)
    {
        return TryGetPtr(typeId, out _);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool TryGet<T>(ushort typeId, out T value)
        where T : unmanaged
    {
        if (TryGetPtr(typeId, out var ptr))
        {
            value = *(T*)ptr;
            return true;
        }

        value = default;
        return false;
    }

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

    public bool Remove(ushort typeId)
    {
        if (!_isCreated)
        {
            return false;
        }

        for (var i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].typeId == typeId)
            {
                _entries.RemoveAt(i);
                if (_entries.Count == 0)
                {
                    _dataBuffer.UnsafeSetCount(0);
                }
                return true;
            }
        }

        return false;
    }

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
