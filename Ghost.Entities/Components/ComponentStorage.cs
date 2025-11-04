using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Entities.Components;

internal static class SingletonContainer<T>
    where T : unmanaged, IComponentData
{
    public static readonly Dictionary<int, T> container = new();
}

internal interface IComponentPool : IDisposable
{
    public EntityID Count
    {
        get;
    }

    public void Add(Entity entity, IComponentData component);
    public bool Remove(Entity entity);
    public bool Has(Entity entity);
    public IComponentData Get(Entity entity);
    public IntPtr GetUnsafe(Entity entity);
    public void Set(Entity entity, in IComponentData component);

    public IEnumerable<(Entity entity, IComponentData component)> Enumerate();
}

internal interface IComponentPool<T> : IComponentPool
    where T : IComponentData
{
    public void Add(Entity entity, T Component);
    public void Set(Entity entity, in T component);
}

internal class ComponentPool<T> : IComponentPool<T>
    where T : unmanaged, IComponentData
{
    private struct ComponentMetadata
    {
        public T data;
        public Entity owner;
    }

    private EntityID _nextId;
    private EntityID _capacity;

    private ComponentMetadata[] _components;
    private EntityID[] _lookup;

    public EntityID Count => _nextId;

    public ComponentPool(int initialSize)
    {
        _nextId = 0;
        _capacity = initialSize;

        _components = new ComponentMetadata[initialSize];
        _lookup = new EntityID[initialSize];

        _lookup.AsSpan().Fill(Entity.INVALID_ID);
    }

    public ComponentPool()
        : this(16)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static EntityID GetLookupIndex(Entity entity)
    {
        return entity.ID;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private EntityID GetComponentIndex(Entity entity)
    {
        return _lookup[GetLookupIndex(entity)];
    }

    public void Add(Entity entity, IComponentData component)
    {
        if (component is not T typedComponent)
        {
            throw new ArgumentException($"Component type mismatch. Expected {typeof(T)}, but got {component.GetType()}.");
        }

        Add(entity, typedComponent);
    }

    public void Add(Entity entity, T component)
    {
        if (!entity.IsValid)
        {
            return;
        }

        var lookupIndex = GetLookupIndex(entity);
        var componentIndex = GetComponentIndex(entity);

        if (componentIndex != Entity.INVALID_ID)
        {
            // Overwrite the old data if generation is larger
            if (entity.Generation > _components[componentIndex].owner.Generation)
            {
                var index = _lookup[lookupIndex];
                _components[index].data = component;
                _components[index].owner = entity;
            }

            return;
        }

        if (_nextId >= _capacity)
        {
            var newCapacity = _capacity * 2;
            Array.Resize(ref _components, newCapacity);
            Array.Resize(ref _lookup, newCapacity);
            _lookup.AsSpan(_capacity, newCapacity - _capacity).Fill(Entity.INVALID_ID);

            _capacity = newCapacity;
        }

        _lookup[lookupIndex] = _nextId;
        _components[_nextId] = new ComponentMetadata
        {
            data = component,
            owner = entity
        };

        _nextId++;
    }

    public bool Remove(Entity entity)
    {
        // We do not remove anything here, the generation of the entity will be used to determine if the component is valid.
        return true;
    }

    public IComponentData Get(Entity entity)
    {
        return GetRef(entity);
    }

    public unsafe IntPtr GetUnsafe(Entity entity)
    {
        return (IntPtr)Unsafe.AsPointer(ref GetRef(entity));
    }

    public ref T GetRef(Entity entity)
    {
        if (!entity.IsValid)
        {
            return ref Unsafe.NullRef<T>();
        }

        var index = GetComponentIndex(entity);
        return ref _components[index].data;
    }

    public bool Has(Entity entity)
    {
        if (entity.ID >= _lookup.Length)
        {
            return false;
        }

        var index = GetComponentIndex(entity);
        return index != Entity.INVALID_ID && _components[index].owner.Generation == entity.Generation;
    }

    public void Set(Entity entity, in IComponentData component)
    {
        if (component is not T typedComponent)
        {
            throw new ArgumentException($"Component type mismatch. Expected {typeof(T)}, but got {component.GetType()}.");
        }
        Set(entity, typedComponent);
    }

    public void Set(Entity entity, in T component)
    {
        if (!entity.IsValid || entity.ID >= _lookup.Length || GetComponentIndex(entity) == Entity.INVALID_ID)
        {
            return;
        }

        var index = GetComponentIndex(entity);
        _components[index].data = component;
        _components[index].owner = entity;
    }

    public IEnumerable<(Entity entity, IComponentData component)> Enumerate()
    {
        for (var i = 0; i < _nextId; i++)
        {
            if (_components[i].owner.IsValid)
            {
                yield return (_components[i].owner, _components[i].data);
            }
        }
    }

    public void Dispose()
    {
        _components = Array.Empty<ComponentMetadata>();
        _lookup = Array.Empty<EntityID>();
        _nextId = 0;
        _capacity = 0;
    }
}

internal class ScriptComponentPool : IComponentPool<ScriptComponent>
{
    private Dictionary<Entity, List<ScriptComponent>>? _scriptComponents;
    private List<ScriptComponent>? _executionList;

    private readonly World _world;

    internal IReadOnlyDictionary<Entity, List<ScriptComponent>>? ScriptComponents => _scriptComponents;
    internal IReadOnlyList<ScriptComponent>? ExecutionList => _executionList;

    public bool IsInitialized => _scriptComponents != null;
    public int Count => _scriptComponents?.Keys.Count ?? 0;

    public ScriptComponentPool(World world)
    {
        _world = world;
    }

    internal void Initialize(int capacity = 16)
    {
        _scriptComponents ??= new(capacity);
    }

    internal void RebuildExecutionList()
    {
        if (_scriptComponents == null)
        {
            return;
        }

        _executionList ??= new List<ScriptComponent>(_scriptComponents.Count);
        _executionList.Clear();

        foreach (var kvp in _scriptComponents)
        {
            _executionList.AddRange(kvp.Value);
        }

        _executionList.Sort((a, b) => a.ExecutionOrder.CompareTo(b.ExecutionOrder));
    }

    public void Add(Entity entity, IComponentData component)
    {
        if (component is not ScriptComponent scriptComponent)
        {
            throw new ArgumentException($"Component type mismatch. Expected {typeof(ScriptComponent)}, but got {component.GetType()}.");
        }

        Add(entity, scriptComponent);
    }

    public void Add(Entity entity, ScriptComponent component)
    {
        if (!IsInitialized)
        {
            Initialize();
        }

        ref var scriptList = ref CollectionsMarshal.GetValueRefOrAddDefault(_scriptComponents!, entity, out var exists);
        scriptList ??= new List<ScriptComponent>();

        scriptList.Add(component);

        component.Owner = entity;
        component._world = _world;
        component.Enable = true;
        component.Initialize();
    }

    public bool Remove<T>(Entity entity)
        where T : ScriptComponent
    {
        if (!Has(entity)
            || !_scriptComponents!.TryGetValue(entity, out var scriptList)
            || scriptList == null)
        {
            return false;
        }

        var scriptToRemove = scriptList.FirstOrDefault(script => script is T);
        if (scriptToRemove == null)
        {
            return false;
        }

        scriptToRemove.OnDestroy();
        scriptList.Remove(scriptToRemove);
        if (scriptList.Count == 0)
        {
            _scriptComponents.Remove(entity);
        }

        return true;
    }

    public bool RemoveAt(Entity entity, int index)
    {
        if (!Has(entity)
            || !_scriptComponents!.TryGetValue(entity, out var scriptList)
            || scriptList == null)
        {
            return false;
        }

        if (index < 0 || index > scriptList.Count)
        {
            return false;
        }

        scriptList.RemoveAt(index);
        if (scriptList.Count == 0)
        {
            _scriptComponents.Remove(entity);
        }

        return true;
    }

    public bool Remove(Entity entity)
    {
        if (!Has(entity)
            || !_scriptComponents!.TryGetValue(entity, out var scriptList)
            || scriptList == null)
        {
            return false;
        }

        foreach (var script in scriptList)
        {
            script.OnDestroy();
        }

        _scriptComponents.Remove(entity);
        return true;
    }

    public bool Has(Entity entity)
    {
        return _scriptComponents?.ContainsKey(entity) ?? false;
    }

    [Obsolete("Use GetAll instead of Get for ScriptComponentPool.")]
    public IComponentData Get(Entity entity)
    {
        if (!Has(entity)
            || !_scriptComponents!.TryGetValue(entity, out var scriptList)
            || scriptList == null
            || scriptList.Count == 0)
        {
            return null!;
        }

        return scriptList[0];
    }

    [Obsolete("Use GetAll instead of GetUnsafe for ScriptComponentPool.")]
    public unsafe IntPtr GetUnsafe(Entity entity)
    {
        if (!Has(entity)
            || !_scriptComponents!.TryGetValue(entity, out var scriptList)
            || scriptList == null
            || scriptList.Count == 0)
        {
            return IntPtr.Zero;
        }

        return (IntPtr)Unsafe.AsPointer(ref CollectionsMarshal.AsSpan(scriptList)[0]);
    }

    public void Set(Entity entity, in IComponentData component)
    {
        if (component is not ScriptComponent scriptComponent)
        {
            throw new ArgumentException($"Component type mismatch. Expected {typeof(ScriptComponent)}, but got {component.GetType()}.");
        }
        Set(entity, scriptComponent);
    }

    public void Set(Entity entity, in ScriptComponent component)
    {
        if (!Has(entity)
            || !_scriptComponents!.TryGetValue(entity, out var scriptList)
            || scriptList == null)
        {
            return;
        }
        var index = scriptList.IndexOf(component);
        if (index >= 0)
        {
            scriptList[index] = component;
            component.Owner = entity;
        }
    }

    public IEnumerable<(Entity entity, IComponentData component)> Enumerate()
    {
        if (_scriptComponents == null)
        {
            yield break;
        }

        foreach (var kvp in _scriptComponents)
        {
            foreach (var script in kvp.Value)
            {
                yield return (kvp.Key, script);
            }
        }
    }

    public IReadOnlyList<IComponentData>? GetAll(Entity entity)
    {
        if (_scriptComponents == null
            || !_scriptComponents.TryGetValue(entity, out var scriptList))
        {
            return null;
        }

        return scriptList;
    }

    public void Dispose()
    {
        if (_scriptComponents != null)
        {
            if (_executionList != null)
            {
                foreach (var script in _executionList)
                {
                    script.OnDestroy();
                }

                _executionList.Clear();
            }
            else
            {
                foreach (var scriptList in _scriptComponents.Values)
                {
                    if (scriptList == null)
                    {
                        continue;
                    }

                    foreach (var script in scriptList)
                    {
                        script.OnDestroy();
                    }
                }

            }

            _scriptComponents.Clear();
        }
    }
}

[SkipLocalsInit]
internal struct ComponentStorage : IDisposable
{
    private static int s_nextId = 0;
    private static class TypeID<T>
    {
        public static readonly int value = s_nextId++;
    }

    private int _currentCapacity = 16;

    private IComponentPool?[] _componentPools = new IComponentPool[16];
    private UnsafeBitSet[] _componentEntityMasks = new UnsafeBitSet[16];

    private readonly Dictionary<TypeHandle, int> _typeIDMap = new(16);
    private readonly Dictionary<int, TypeHandle> _typeHandleMap = new(16);

    private readonly ScriptComponentPool _scriptComponentPool;

    private readonly World _world;

    internal ComponentStorage(World world)
    {
        _world = world;
        _scriptComponentPool = new ScriptComponentPool(world);
    }

    internal readonly IReadOnlyList<IComponentPool?> ComponentPools => _componentPools;
    internal readonly IReadOnlyList<UnsafeBitSet> ComponentEntityMasks => _componentEntityMasks;
    internal readonly ScriptComponentPool ScriptComponentPool => _scriptComponentPool;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly int GetTypeID(TypeHandle typeHandle)
    {
        if (_typeIDMap.TryGetValue(typeHandle, out var id))
        {
            return id;
        }

        return typeof(TypeID<>).MakeGenericType(typeHandle!)
            .GetField("value", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
            ?.GetValue(null) as int? ?? -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly int GetTypeID<T>()
    {
        return TypeID<T>.value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Resize(int newCapacity)
    {
        Array.Resize(ref _componentPools, newCapacity);
        Array.Resize(ref _componentEntityMasks, newCapacity);
        _currentCapacity = newCapacity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal readonly TypeHandle GetComponentPoolType(int poolIndex)
    {
        if (poolIndex < 0 || poolIndex >= _currentCapacity)
        {
            throw new ArgumentOutOfRangeException(nameof(poolIndex), "Invalid pool index.");
        }

        return _typeHandleMap[poolIndex];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool TryGetPool(TypeHandle typeHandle, [NotNullWhen(true)] out IComponentPool? pool)
    {
        var result = _typeIDMap.TryGetValue(typeHandle, out var id);
        if (!result || id >= _currentCapacity)
        {
            pool = null;
            return false;
        }

        pool = _componentPools[id];
        return pool != null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool TryGetPool(Type type, [NotNullWhen(true)] out IComponentPool? pool)
    {
        return TryGetPool(TypeHandle.Get(type), out pool);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool TryGetPool<T>([NotNullWhen(true)] out ComponentPool<T>? pool)
        where T : unmanaged, IComponentData
    {
        var id = TypeID<T>.value;
        if (id >= _currentCapacity)
        {
            pool = null;
            return false;
        }

        pool = (ComponentPool<T>?)_componentPools[id];
        return pool != null;
    }

    public IComponentPool GetOrCreateComponentPool(Type type)
    {
        var typeHandle = TypeHandle.Get(type);
        if (_typeIDMap.TryGetValue(typeHandle, out var id))
        {
            if (id < _currentCapacity && _componentPools[id] is IComponentPool existingPool)
            {
                return existingPool;
            }
        }
        else
        {
            id = GetTypeID(typeHandle);
        }

        if (id >= _currentCapacity)
        {
            Resize(_currentCapacity * 2);
            _typeIDMap[typeHandle] = id;
            _typeHandleMap[id] = typeHandle;
        }
        else if (_componentPools[id] is IComponentPool existingPool)
        {
            return existingPool;
        }

        var pool = Activator.CreateInstance(typeof(ComponentPool<>).MakeGenericType(type)) as IComponentPool
            ?? throw new InvalidOperationException($"Failed to create component pool for type {type.FullName}");

        _componentPools[id] = pool;

        return pool;
    }

    public ComponentPool<T> GetOrCreateComponentPool<T>()
        where T : unmanaged, IComponentData
    {
        var id = TypeID<T>.value;
        var typeHandle = TypeHandle.Get<T>();

        if (id >= _currentCapacity)
        {
            Resize(_currentCapacity * 2);
            _typeIDMap[typeHandle] = id;
            _typeHandleMap[id] = typeHandle;
        }
        else if (_componentPools[id] is ComponentPool<T> existingPool)
        {
            return existingPool;
        }

        var pool = new ComponentPool<T>();
        _componentPools[id] = pool;

        return pool;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool TryGetMask(TypeHandle typeHandle, [NotNullWhen(true)] out UnsafeBitSet? bitSet)
    {
        if (!_typeIDMap.TryGetValue(typeHandle, out var id)
            || id >= _currentCapacity)
        {
            bitSet = null;
            return false;
        }

        bitSet = _componentEntityMasks[id];
        return bitSet.HasValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool TryGetMask<T>([NotNullWhen(true)] out UnsafeBitSet? bitSet)
        where T : unmanaged, IComponentData
    {
        return TryGetMask(TypeHandle.Get<T>(), out bitSet);
    }

    public ref UnsafeBitSet GetOrCreateMask(TypeHandle typeHandle)
    {
        if (!_typeIDMap.TryGetValue(typeHandle, out var id))
        {
            id = GetTypeID(typeHandle);
            _typeIDMap[typeHandle] = id;
            _typeHandleMap[id] = typeHandle;
        }

        if (id >= _currentCapacity)
        {
            Resize(_currentCapacity * 2);
        }

        ref var set = ref _componentEntityMasks[id];
        if (!set.IsCreated)
        {
            set = new UnsafeBitSet();
        }

        return ref set;
    }

    public ref UnsafeBitSet GetOrCreateMask<T>()
        where T : unmanaged, IComponentData
    {
        return ref GetOrCreateMask(TypeHandle.Get<T>());
    }

    public ref UnsafeBitSet GetOrCreateMask(Type type)
    {
        return ref GetOrCreateMask(TypeHandle.Get(type));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void RebuildExecutionList()
    {
        _scriptComponentPool.RebuildExecutionList();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Remove(Entity entity)
    {
        _scriptComponentPool.Remove(entity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Dispose()
    {
        foreach (var pool in _componentPools)
        {
            pool?.Dispose();
        }

        foreach (var bitSet in _componentEntityMasks)
        {
            bitSet.Dispose();
        }

        _scriptComponentPool.Dispose();
    }
}