using Ghost.Engine.Volumes;
using Ghost.Entities;
using Misaki.HighPerformance.LowLevel.Buffer;
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
    private UnmanagedTypeMap _storage;

    public readonly bool IsCreated => _storage.IsCreated;
    public readonly int ComponentCount => _storage.ComponentCount;
    public readonly ReadOnlySpan<VolumeDataEntry> Entries => _storage.Entries;

    public RenderVolumeDataContainer(int initialByteCapacity = 64)
        : this(initialByteCapacity, AllocationHandle.Persistent)
    {
    }

    public RenderVolumeDataContainer(int initialByteCapacity, AllocationHandle allocationHandle)
    {
        _storage = new UnmanagedTypeMap(initialByteCapacity, allocationHandle);
    }

    public static RenderVolumeDataContainer Create()
    {
        return new RenderVolumeDataContainer(64, AllocationHandle.Persistent);
    }

    public static RenderVolumeDataContainer Create(AllocationHandle allocationHandle)
    {
        return new RenderVolumeDataContainer(64, allocationHandle);
    }

    /// <summary>
    /// Adds or updates a volume component setting inside this container.
    /// </summary>
    /// <typeparam name="T">The volume component type.</typeparam>
    /// <param name="value">The component value to store.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set<T>(in T value)
        where T : unmanaged, IVolumeComponent<T>
    {
        VolumeBlenderTrampoline<T>.EnsureInitialized();
        _storage.Set(VolumeBlenderTrampoline<T>.TypeId, in value);
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
        return _storage.Has(VolumeBlenderTrampoline<T>.TypeId);
    }

    /// <summary>
    /// Attempts to retrieve a copy of the component data stored in this container.
    /// </summary>
    /// <typeparam name="T">The volume component type.</typeparam>
    /// <param name="value">The retrieved component data if found.</param>
    /// <returns>True if found, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool TryGet<T>(out T value)
        where T : unmanaged, IVolumeComponent<T>
    {
        return _storage.TryGet(VolumeBlenderTrampoline<T>.TypeId, out value);
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
        return _storage.TryGetPtr(typeId, out ptr);
    }

    /// <summary>
    /// Removes a volume component setting from this container if present.
    /// </summary>
    /// <typeparam name="T">The volume component type.</typeparam>
    /// <returns>True if removed, false if not present.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove<T>()
        where T : unmanaged, IVolumeComponent<T>
    {
        return _storage.Remove(VolumeBlenderTrampoline<T>.TypeId);
    }

    /// <summary>
    /// Releases unmanaged buffer allocations.
    /// </summary>
    public void Dispose()
    {
        _storage.Dispose();
    }
}
