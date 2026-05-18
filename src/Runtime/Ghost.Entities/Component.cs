using Ghost.Core;
using Ghost.Core.Utilities;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.IO.Hashing;
using System.Runtime.CompilerServices;

namespace Ghost.Entities;

public interface IComponent;
public interface IComponentData : IComponent;
public interface IEnableableComponent : IComponentData;
public interface ICleanupComponent : IComponentData;
public interface ISharedComponent : IComponent;

[AttributeUsage(AttributeTargets.Struct)]
public class RequireComponentAttribute<T> : Attribute
    where T : unmanaged, IComponent
{
    public Type RequiredType => typeof(T);
}

internal struct ComponentInfo
{
    public Identifier<IComponent> id;
    public int size;
    public int alignment;
    public bool isEnableable;
    public bool isCleanup;
    public bool isShared;
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

    // NOTE: Can we remove the lock? Ideally all the component registeration will happend during module init, way before the first get.
    private static readonly Lock s_registerLock = new();

#if DEBUG || GHOST_EDITOR
    internal static readonly Dictionary<int, Type> s_runtimeIDToType = new();
#endif

    public static unsafe Identifier<IComponent> GetOrRegisterComponentID<T>()
        where T : unmanaged, IComponent
    {
        var type = typeof(T);
        var typeHandle = type.TypeHandle.Value;

        lock (s_registerLock)
        {
            if (s_typeHandleToID.TryGetValue(typeHandle, out var existingID))
            {
                return existingID;
            }

            var newID = new Identifier<IComponent>(s_registeredComponents.Count);
            var stableName = typeof(T).FullName ?? typeof(T).Name;
            var info = new ComponentInfo
            {
                id = newID,
                size = sizeof(T),
                alignment = (int)MemoryUtility.AlignOf<T>(),
                isEnableable = typeof(IEnableableComponent).IsAssignableFrom(type),
                isCleanup = typeof(ICleanupComponent).IsAssignableFrom(type),
                isShared = typeof(ISharedComponent).IsAssignableFrom(type),
            };

            s_registeredComponents.Add(info);

            s_typeHandleToID[typeHandle] = newID;
            s_nameToRuntimeID[stableName] = newID;
#if DEBUG || GHOST_EDITOR
            s_runtimeIDToType[newID.Value] = typeof(T);
#endif

            return newID;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Identifier<IComponent> GetComponentID(Type type)
    {
        var typeHandle = type.TypeHandle.Value;
        lock (s_registerLock)
        {
            if (s_typeHandleToID.TryGetValue(typeHandle, out var existingID))
            {
                return existingID;
            }
        }

        return Identifier<IComponent>.Invalid;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Identifier<IComponent> GetComponentIDByName(string fullName)
    {
        lock (s_registerLock)
        {
            if (s_nameToRuntimeID.TryGetValue(fullName, out var id))
            {
                return id;
            }
        }

        return Identifier<IComponent>.Invalid;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ComponentInfo GetComponentInfo(Identifier<IComponent> typeId)
    {
        lock (s_registerLock)
        {
            return s_registeredComponents[typeId];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ComponentInfo GetComponentInfo(Type type)
    {
        lock (s_registerLock)
        {
            var typeId = GetComponentID(type);
            if (typeId.IsInvalid)
            {
                throw new KeyNotFoundException($"Component type {type.FullName} is not registered.");
            }

            return s_registeredComponents[typeId];
        }
    }

    public static int GetHashCodeForTypeIDs(params ReadOnlySpan<Identifier<IComponent>> componentTypeIDs)
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

    public static int GetHashCodeForSharedData(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            return 0;
        }

        return Unsafe.BitCast<uint, int>(XxHash32.HashToUInt32(data));
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Clear()
    {
        for (var i = 0; i < _archetypes.Count; i++)
        {
            _archetypes[i].Dispose();
        }

        for (var i = 0; i < _entityQueries.Count; i++)
        {
            _entityQueries[i].Dispose();
        }

        _archetypes.Clear();
        _entityQueries.Clear();

        _archetypeLookup.Clear();
        _querieLookup.Clear();

        CreateArchetype(ReadOnlySpan<Identifier<IComponent>>.Empty, 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Collect()
    {
        for (var i = 0; i < _archetypes.Count; i++)
        {
            _archetypes[i].Collect();
        }
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

public struct SharedComponentSet : IDisposable
{
    private BufferWriter _writer;

    public SharedComponentSet(int capacity, AllocationHandle allocationHandle)
    {
        _writer = new BufferWriter(capacity, allocationHandle);
    }

    public void With<T>(scoped in T data)
        where T : unmanaged, ISharedComponent
    {
        _writer.Write(in data);
    }

    public readonly ReadOnlySpan<byte> AsSpan()
    {
        return _writer.AsSpan();
    }

    public void Reset()
    {
        _writer.Reset();
    }

    public void Dispose()
    {
        _writer.Dispose();
    }
}

/// <summary>
/// Represents an immutable set of component identifiers used to define a group of components within an entity or system.
/// </summary>
public struct ComponentSet : IDisposable, IEquatable<ComponentSet>
{
    private UnsafeArray<Identifier<IComponent>> _components;
    private UnsafeArray<byte> _sharedData;
    private int _hashCode;
    private int _sharedHashCode;

    public readonly ReadOnlySpan<Identifier<IComponent>> Components => _components;
    public readonly ReadOnlySpan<byte> SharedComponentData => _sharedData;

    public int ComponentHashCode
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (_hashCode == -1)
            {
                _hashCode = ComponentRegistry.GetHashCodeForTypeIDs(_components);
            }

            return _hashCode;
        }
    }

    public int SharedDataHashCode
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (_sharedHashCode == -1)
            {
                _sharedHashCode = ComponentRegistry.GetHashCodeForSharedData(_sharedData);
            }

            return _sharedHashCode;
        }
    }

    public ComponentSet(AllocationHandle allocationHandle, params ReadOnlySpan<Identifier<IComponent>> components)
    {
        _components = new UnsafeArray<Identifier<IComponent>>(components.Length, allocationHandle);
        components.CopyTo(_components.AsSpan());

        _hashCode = -1;
        _sharedHashCode = -1;
    }

    public ComponentSet(AllocationHandle allocationHandle, ReadOnlySpan<Identifier<IComponent>> components, ReadOnlySpan<byte> sharedData)
    {
        _components = new UnsafeArray<Identifier<IComponent>>(components.Length, allocationHandle);
        components.CopyTo(_components.AsSpan());

        _sharedData = new UnsafeArray<byte>(sharedData.Length, allocationHandle);
        sharedData.CopyTo(_sharedData);

        _hashCode = -1;
        _sharedHashCode = -1;
    }

    public ComponentSet(AllocationHandle allocationHandle, ReadOnlySpan<Identifier<IComponent>> components, SharedComponentSet sharedComponentSet)
        : this(allocationHandle, components, sharedComponentSet.AsSpan())
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ComponentSetView AsView()
    {
        return new ComponentSetView(_components, _sharedData);
    }

    public readonly bool Equals(ComponentSet other)
    {
        return _hashCode == other._hashCode && _sharedHashCode == other._sharedHashCode;
    }

    public override int GetHashCode()
    {
        return ComponentHashCode ^ (SharedDataHashCode >> 16);
    }

    public void Dispose()
    {
        _components.Dispose();
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

    public static implicit operator ComponentSetView(in ComponentSet set)
    {
        return new ComponentSetView(set._components, set._sharedData);
    }
}

/// <summary>
/// Represents a view of component set from external buffer, used to define a group of components within an entity or system.
/// </summary>
public ref struct ComponentSetView : IEquatable<ComponentSetView>
{
    private readonly ReadOnlySpan<Identifier<IComponent>> _components;
    private readonly ReadOnlySpan<byte> _sharedData;
    private int _hashCode;
    private int _sharedHashCode;

    public readonly ReadOnlySpan<Identifier<IComponent>> Components => _components;
    public readonly ReadOnlySpan<byte> SharedComponentData => _sharedData;

    public int ComponentHashCode
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (_hashCode == -1)
            {
                _hashCode = ComponentRegistry.GetHashCodeForTypeIDs(_components);
            }

            return _hashCode;
        }
    }

    public int SharedDataHashCode
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (_sharedHashCode == -1)
            {
                _sharedHashCode = ComponentRegistry.GetHashCodeForSharedData(_sharedData);
            }

            return _sharedHashCode;
        }
    }

    public ComponentSetView(ReadOnlySpan<Identifier<IComponent>> components)
    {
        _components = components;
        _hashCode = -1;
        _sharedHashCode = -1;
    }

    public ComponentSetView(ReadOnlySpan<Identifier<IComponent>> components, ReadOnlySpan<byte> sharedData)
    {
        _components = components;
        _sharedData = sharedData;
        _hashCode = -1;
        _sharedHashCode = -1;
    }

    public ComponentSetView(ReadOnlySpan<Identifier<IComponent>> components, SharedComponentSet sharedComponentSet)
        : this(components, sharedComponentSet.AsSpan())
    {
    }

    public readonly bool Equals(ComponentSetView other)
    {
        return _hashCode == other._hashCode && _sharedHashCode == other._sharedHashCode;
    }

    public override int GetHashCode()
    {
        return ComponentHashCode ^ (SharedDataHashCode >> 16);
    }

    public override readonly bool Equals(object? obj)
    {
        return false;
    }

    public static bool operator ==(ComponentSetView left, ComponentSetView right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ComponentSetView left, ComponentSetView right)
    {
        return !(left == right);
    }
}
