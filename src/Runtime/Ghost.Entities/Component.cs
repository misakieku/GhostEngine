using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Runtime.CompilerServices;

namespace Ghost.Entities;

public interface IComponent;
public interface IEnableableComponent : IComponent;
public interface ICleanupComponent : IComponent;

[AttributeUsage(AttributeTargets.Struct)]
public class RequireComponentAttribute<T> : Attribute
    where T : unmanaged, IComponent
{
    public Type RequiredType => typeof(T);
}

internal struct ComponentInfo
{
    // public string stableName; // Do we actually need this?
    public Identifier<IComponent> id;
    public int size;
    public int alignment;
    public bool isEnableable;
    public bool isSharedWarper;
    public bool isCleanup;
}

/// <summary>
/// Provides a unique identifier for the specified unmanaged component space.
/// </summary>
/// <typeparam name="T">The component space for which to obtain an identifier. Must be unmanaged and implement <see cref="IComponent"/>.</typeparam>
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

#if GHOST_EDITOR
    internal static readonly Dictionary<int, Type> s_runtimeIDToType = new();
#endif

    static ComponentRegistry()
    {
        GetOrRegisterComponentID<ManagedEntityRef>();
    }

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
                // stableName = stableName,
                id = newID,
                size = sizeof(T),
                alignment = (int)MemoryUtility.AlignOf<T>(),
                isEnableable = typeof(IEnableableComponent).IsAssignableFrom(type),
                isSharedWarper = typeof(ISharedWrapper).IsAssignableFrom(type),
                isCleanup = typeof(ICleanupComponent).IsAssignableFrom(type),
            };

            s_registeredComponents.Add(info);

            s_typeHandleToID[typeHandle] = newID;
            s_nameToRuntimeID[stableName] = newID;
#if GHOST_EDITOR
            s_runtimeIDToType[newID.Value] = typeof(T);
#endif

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
            if (id.Value > largestID)
            {
                largestID = id.Value;
            }
        }

        var length = UnsafeBitSet.RequiredLength(largestID + 1);
        var bits = (Span<uint>)stackalloc uint[length];
        bits.Clear();

        var bitSet = new SpanBitSet(bits);
        foreach (var id in componentTypeIDs)
        {
            bitSet.SetBit(id.Value);
        }

        return bitSet.GetHashCode();
    }
}

public partial class ComponentManager : IDisposable
{
    private readonly World _world;

    private UnsafeList<Archetype> _archetypes;
    private UnsafeList<EntityQuery> _entityQueries;

    private UnsafeHashMap<int, Identifier<Archetype>> _archetypeLookup; // Signature Hash to Archetype ID
    private UnsafeHashMap<int, Identifier<EntityQuery>> _querieLookup; // Query Mask Hash to Query ID

    private bool _isDisposed;

    public int ArchetypeCount => _archetypes.Count;

    internal ComponentManager(World world)
    {
        _world = world;

        _archetypes = new UnsafeList<Archetype>(16, AllocationHandle.Persistent);
        _entityQueries = new UnsafeList<EntityQuery>(16, AllocationHandle.Persistent);

        _archetypeLookup = new UnsafeHashMap<int, Identifier<Archetype>>(16, AllocationHandle.Persistent);
        _querieLookup = new UnsafeHashMap<int, Identifier<EntityQuery>>(16, AllocationHandle.Persistent);

        // Create the empty archetype
        CreateArchetype(ReadOnlySpan<Identifier<IComponent>>.Empty, 0);
    }

    ~ComponentManager()
    {
        Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Identifier<Archetype> CreateArchetype(ReadOnlySpan<Identifier<IComponent>> componentTypeIDs, int signatureHash)
    {
        var arcID = new Identifier<Archetype>(_archetypes.Count);
        _archetypes.Add(new Archetype(arcID, _world.ID, componentTypeIDs));
        _archetypeLookup.Add(signatureHash, arcID);

        for (var i = 0; i < _entityQueries.Count; i++)
        {
            ref var query = ref _entityQueries[i];
            query.AddArchetypeIfMatch(in _archetypes[arcID.Value]);
        }

        return arcID;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Identifier<Archetype> GetArchetypeIDBySignatureHash(int signatureHash)
    {
        if (_archetypeLookup.TryGetValue(signatureHash, out var arcID))
        {
            return arcID;
        }

        return Identifier<Archetype>.Invalid;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref Archetype GetArchetypeReference(Identifier<Archetype> id)
    {
        return ref _archetypes[id.Value];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Identifier<EntityQuery> CreateEntityQuery(ref readonly EntityQueryMask mask, int maskHash)
    {
        var queryID = new Identifier<EntityQuery>(_entityQueries.Count);
        _entityQueries.Add(new EntityQuery(queryID, _world.ID, in mask));
        _querieLookup.Add(maskHash, queryID);

        ref var query = ref _entityQueries[queryID.Value];
        for (var i = 0; i < _archetypes.Count; i++)
        {
            query.AddArchetypeIfMatch(in _archetypes[i]);
        }

        return queryID;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Identifier<EntityQuery> GetEntityQueryIDByMaskHash(int maskHash)
    {
        if (_querieLookup.TryGetValue(maskHash, out var queryID))
        {
            return queryID;
        }

        return Identifier<EntityQuery>.Invalid;
    }

    /// <summary>
    /// Gets a reference to the entity query with the specified identifier.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref EntityQuery GetEntityQueryReference(Identifier<EntityQuery> id)
    {
        return ref _entityQueries[id.Value];
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        foreach (ref var archetype in _archetypes)
        {
            archetype.Dispose();
        }

        foreach (ref var query in _entityQueries)
        {
            query.Dispose();
        }

        _archetypes.Dispose();
        _entityQueries.Dispose();
        _archetypeLookup.Dispose();
        _querieLookup.Dispose();

        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
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

    public override readonly bool Equals(object? obj)
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
