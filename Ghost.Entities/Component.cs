using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Runtime.CompilerServices;

namespace Ghost.Entities;

public interface IComponent
{
}

public interface IEnableableComponent : IComponent
{
}

internal struct ComponentInfo
{
    // public string stableName; // Do we actually need this?
    public Identifier<IComponent> id;
    public int size;
    public int alignment;
    public bool isEnableable;
}

/// <summary>
/// Represents an immutable set of component identifiers used to define a group of components within an entity or system.
/// </summary>
public struct ComponentSet : IDisposable, IEquatable<ComponentSet>
{
    private UnsafeArray<Identifier<IComponent>> _components;
    private int _hashCode;

    public readonly ReadOnlySpan<Identifier<IComponent>> Components => _components.AsSpan();

    public ComponentSet(AllocationHandle allocationHandle, params ReadOnlySpan<Identifier<IComponent>> components)
    {
        _components = new UnsafeArray<Identifier<IComponent>>(components.Length, allocationHandle);
        components.CopyTo(_components.AsSpan());

        _hashCode = -1;
    }

    public ComponentSet(Allocator allocator, params ReadOnlySpan<Identifier<IComponent>> components)
        : this(AllocationManager.GetAllocationHandle(allocator), components)
    {
    }

    public readonly bool Equals(ComponentSet other)
    {
        return _hashCode == other._hashCode;
    }

    public override int GetHashCode()
    {
        if (_hashCode == -1)
        {
            _hashCode = ComponentRegistry.GetHashCode(_components.AsSpan());
        }

        return _hashCode;
    }

    public override bool Equals(object? obj)
    {
        return obj is ComponentSet set && Equals(set);
    }

    public static bool operator ==(ComponentSet left, ComponentSet right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ComponentSet left, ComponentSet right)
    {
        return !(left == right);
    }

    public void Dispose()
    {
        _components.Dispose();
    }
}

/// <summary>
/// Provides a unique identifier for the specified unmanaged component type.
/// </summary>
/// <typeparam name="T">The component type for which to obtain an identifier. Must be unmanaged and implement <see cref="IComponent"/>.</typeparam>
public static class ComponentTypeID<T>
    where T : unmanaged, IComponent
{
    public static readonly Identifier<IComponent> Value = ComponentRegistry.GetOrRegisterComponentID<T>();
}

internal static class ComponentRegistry
{
    private static readonly List<ComponentInfo> s_registeredComponents = new();
    private static readonly Dictionary<IntPtr, int> s_typeHandleToID = new();
    private static readonly Dictionary<string, int> s_nameToRuntimeID = new();

    internal static readonly Dictionary<int, Type> s_runtimeIDToType = new();

    public static unsafe Identifier<IComponent> GetOrRegisterComponentID<T>()
        where T : unmanaged, IComponent
    {
        var type = typeof(T);
        var typeHandle = type.TypeHandle.Value;

        lock (s_registeredComponents)
        {
            if (s_typeHandleToID.TryGetValue(typeHandle, out var existingID))
            {
                return existingID;
            }

            var newID = new Identifier<IComponent>(s_registeredComponents.Count);
            var stableName = typeof(T).FullName ?? typeof(T).Name;
            var info = new ComponentInfo
            {
                // stableName = new FixedText64(stableName),
                id = newID,
                size = sizeof(T),
                alignment = (int)MemoryUtility.AlignOf<T>(),
                isEnableable = typeof(IEnableableComponent).IsAssignableFrom(type),
                // isManaged = typeof(IManagedWrapper).IsAssignableFrom(type),
            };

            s_registeredComponents.Add(info);

            s_typeHandleToID[typeHandle] = newID;
            s_nameToRuntimeID[stableName] = newID;
            s_runtimeIDToType[newID.value] = typeof(T);

            return newID;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Identifier<IComponent> GetComponentID(Type type)
    {
        var typeHandle = type.TypeHandle.Value;
        lock (s_registeredComponents)
        {
            if (s_typeHandleToID.TryGetValue(typeHandle, out var existingID))
            {
                return existingID;
            }
        }

        return Identifier<IComponent>.Invalid;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ComponentInfo GetComponentInfo(Identifier<IComponent> typeId)
    {
        lock (s_registeredComponents)
        {
            return s_registeredComponents[typeId];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ComponentInfo GetComponentInfo(Type type)
    {
        lock (s_registeredComponents)
        {
            var typeId = GetComponentID(type);
            if (typeId.IsInvalid)
            {
                throw new KeyNotFoundException($"Component type {type.FullName} is not registered.");
            }

            return s_registeredComponents[typeId];
        }
    }

    public static int GetHashCode(params ReadOnlySpan<Identifier<IComponent>> componentTypeIDs)
    {
        var largestID = 0;
        foreach (var id in componentTypeIDs)
        {
            if (id.value > largestID)
            {
                largestID = id.value;
            }
        }

        var length = UnsafeBitSet.RequiredLength(largestID + 1);
        var bits = (Span<uint>)stackalloc uint[length];
        bits.Clear();

        var bitSet = new SpanBitSet(bits);
        foreach (var id in componentTypeIDs)
        {
            bitSet.SetBit(id.value);
        }

        return bitSet.GetHashCode();
    }
}
