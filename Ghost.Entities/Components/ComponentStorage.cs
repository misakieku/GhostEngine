using Ghost.Core;
using Misaki.HighPerformance.Unsafe.Collections;
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
    private struct ComponentData
    {
        public T data;
        public Entity owner;
    }

    private EntityID _nextId;
    private EntityID _capacity;

    private ComponentData[] _components;
    private EntityID[] _lookup;

    public EntityID Count => _nextId;

    public ComponentPool(int initialSize)
    {
        _nextId = 0;
        _capacity = initialSize;

        _components = new ComponentData[initialSize];
        _lookup = new EntityID[initialSize];

        _lookup.AsSpan().Fill(Entity.INVALID_ID);
    }

    public ComponentPool() : this(16)
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
        _components[_nextId] = new ComponentData
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
        _components = Array.Empty<ComponentData>();
        _lookup = Array.Empty<EntityID>();
        _nextId = 0;
        _capacity = 0;
    }
}

internal class ScriptComponentPool : IComponentPool<ScriptComponent>
{
    private Dictionary<Entity, List<ScriptComponent>>? _scriptComponents;
    private List<ScriptComponent>? _executionList;

    internal Dictionary<Entity, List<ScriptComponent>>? ScriptComponents => _scriptComponents;
    internal List<ScriptComponent>? ExecutionList => _executionList;

    public bool IsInitialized => _scriptComponents != null;
    public int Count => _scriptComponents?.Keys.Count ?? 0;

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

        if (!_scriptComponents!.TryGetValue(entity, out var scriptList))
        {
            scriptList = new();
            _scriptComponents[entity] = scriptList;
        }

        scriptList.Add(component);
        component.Owner = entity;
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
internal readonly struct ComponentStorage : IDisposable
{
    private readonly Dictionary<TypeHandle, IComponentPool> _componentPools = new();
    private readonly Dictionary<TypeHandle, BitSet> _componentEntityMasks = new();
    private readonly ScriptComponentPool _scriptComponentPool = new();

    private readonly World _world;

    internal ComponentStorage(World world)
    {
        _world = world;
    }

    internal Dictionary<TypeHandle, IComponentPool> ComponentPools => _componentPools;
    internal Dictionary<TypeHandle, BitSet> ComponentEntityMasks => _componentEntityMasks;
    internal ScriptComponentPool ScriptComponentPool => _scriptComponentPool;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetPool(TypeHandle typeHandle, [MaybeNullWhen(false)] out IComponentPool pool)
    {
        return _componentPools.TryGetValue(typeHandle, out pool);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetPool(Type type, [MaybeNullWhen(false)] out IComponentPool pool)
    {
        return TryGetPool(TypeHandle.Get(type), out pool);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetPool<T>([MaybeNullWhen(false)] out ComponentPool<T> pool)
        where T : unmanaged, IComponentData
    {
        var result = TryGetPool(TypeHandle.Get<T>(), out var obj);
        pool = (ComponentPool<T>?)obj ?? default;
        return result;
    }

    public ComponentPool<T> GetOrCreateComponentPool<T>()
        where T : unmanaged, IComponentData
    {
        var key = TypeHandle.Get<T>();
        if (!_componentPools.TryGetValue(key, out var obj))
        {
            var pool = new ComponentPool<T>(16);
            _componentPools[key] = pool;
            return pool;
        }

        return (ComponentPool<T>)obj;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetMask(TypeHandle typeHandle, [MaybeNullWhen(false)] out BitSet bitSet)
    {
        return _componentEntityMasks.TryGetValue(typeHandle, out bitSet);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetMask(Type type, [MaybeNullWhen(false)] out BitSet bitSet)
    {
        return TryGetMask(TypeHandle.Get(type), out bitSet);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetMask<T>([MaybeNullWhen(false)] out BitSet bitSet)
        where T : unmanaged, IComponentData
    {
        return TryGetMask(TypeHandle.Get<T>(), out bitSet);
    }

    public BitSet GetOrCreateMask(TypeHandle typeHandle)
    {
        if (!_componentEntityMasks.TryGetValue(typeHandle, out var mask))
        {
            mask = new BitSet();
            _componentEntityMasks[typeHandle] = mask;
        }
        return mask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RebuildExecutionList()
    {
        _scriptComponentPool.RebuildExecutionList();
    }

    public void Remove(Entity entity)
    {
        _scriptComponentPool.Remove(entity);
    }

    public void Dispose()
    {
        foreach (var pool in _componentPools.Values)
        {
            pool.Dispose();
        }
        _componentPools.Clear();
        _scriptComponentPool.Dispose();
    }
}