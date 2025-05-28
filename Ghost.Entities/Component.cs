using Ghost.Entities.Utilities;
using Misaki.HighPerformance.Unsafe.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Ghost.Entities;

public interface IComponentData
{

}

internal static class SingletonContainer<T>
    where T : struct, IComponentData
{
    public static readonly Dictionary<int, T> container = new();
}

internal interface IComponentPool : IDisposable
{
    public EntityID Count
    {
        get;
    }

    public void Remove(Entity entity);
    public bool Has(Entity entity);
}

internal interface IComponentPool<T> : IComponentPool
{
    public void Add(Entity entity, T Component);
}

internal class ComponentPool<T> : IComponentPool<T>
    where T : struct, IComponentData
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

    public ComponentPool(int initialSize = 16)
    {
        _nextId = 0;
        _capacity = initialSize;

        _components = new ComponentData[initialSize];
        _lookup = new EntityID[initialSize];

        _lookup.AsSpan().Fill(Entity.INVALID_ID);
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

    public void Remove(Entity entity)
    {
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

    public void Set(Entity entity, T component)
    {
        if (!entity.IsValid || entity.ID >= _lookup.Length || GetComponentIndex(entity) == Entity.INVALID_ID)
        {
            return;
        }

        var index = GetComponentIndex(entity);
        _components[index].data = component;
        _components[index].owner = entity;
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

    public void Remove(Entity entity)
    {
        if (!Has(entity)
            || !_scriptComponents!.TryGetValue(entity, out var scriptList)
            || scriptList == null)
        {
            return;
        }

        foreach (var script in scriptList)
        {
            script.OnDestroy();
        }

        _scriptComponents.Remove(entity);
    }

    public bool Has(Entity entity)
    {
        return _scriptComponents?.ContainsKey(entity) ?? false;
    }

    public List<ScriptComponent>? Get(Entity entity)
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

internal class ComponentStorage : IDisposable
{
    private readonly Dictionary<nint, IComponentPool> _componentPools = new();
    private readonly Dictionary<nint, BitSet> _componentEntityMasks = new();
    private readonly ScriptComponentPool _scriptComponentPool = new();

    internal Dictionary<nint, IComponentPool> ComponentPools => _componentPools;
    internal ScriptComponentPool ScriptComponentPool => _scriptComponentPool;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetPool(nint typeHandle, [MaybeNullWhen(false)] out IComponentPool pool)
    {
        return _componentPools.TryGetValue(typeHandle, out pool);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetPool(Type type, [MaybeNullWhen(false)] out IComponentPool pool)
    {
        return TryGetPool(type.TypeHandle.Value, out pool);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetPool<T>([MaybeNullWhen(false)] out ComponentPool<T> pool)
        where T : struct, IComponentData
    {
        var result = TryGetPool(TypeHandle<T>.Value, out var obj);
        pool = (ComponentPool<T>?)obj;
        return result;
    }

    public ComponentPool<T> GetOrCreateComponentPool<T>()
        where T : struct, IComponentData
    {
        var key = TypeHandle<T>.Value;
        if (!_componentPools.TryGetValue(key, out var obj))
        {
            var pool = new ComponentPool<T>();
            _componentPools[key] = pool;
            return pool;
        }

        return (ComponentPool<T>)obj;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetMask(nint typeHandle, [MaybeNullWhen(false)] out BitSet bitSet)
    {
        return _componentEntityMasks.TryGetValue(typeHandle, out bitSet);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetMask(Type type, [MaybeNullWhen(false)] out BitSet bitSet)
    {
        return TryGetMask(type.TypeHandle.Value, out bitSet);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetMask<T>([MaybeNullWhen(false)] out BitSet bitSet)
        where T : struct, IComponentData
    {
        return TryGetMask(TypeHandle<T>.Value, out bitSet);
    }

    public BitSet GetOrCreateMask(nint typeHandle)
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