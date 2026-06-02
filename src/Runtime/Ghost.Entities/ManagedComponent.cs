#if false
using Ghost.Core;
using Misaki.HighPerformance.Collections;
using System.Runtime.CompilerServices;

namespace Ghost.Entities;

public interface IManagedComponent;
public interface IManagedWrapper;

public readonly struct Managed<T> : IComponentData, IManagedWrapper
    where T : IManagedComponent
{
    public readonly int id;
    public readonly int generation;

    internal Managed(int id, int generation)
    {
        this.id = id;
        this.generation = generation;
    }
}

public static class ManagedComponemtnID<T>
    where T : IManagedComponent
{
    public static readonly Identifier<IManagedComponent> Value = ManagedComponentRegistry.GetOrRegisterComponent<T>();
}

internal struct ManagedComponentInfo
{
    public int id;
    public bool isScriptComponent;
}

internal static class ManagedComponentRegistry
{
    private static readonly List<ManagedComponentInfo> s_registeredComponents = new();
    private static readonly Dictionary<IntPtr, int> s_typeHandleToID = new();
    private static readonly Dictionary<string, int> s_nameToRuntimeID = new();
#if GHOST_EDITOR
    internal static readonly Dictionary<int, Type> s_runtimeIDToType = new();
#endif

    public static Identifier<IManagedComponent> GetOrRegisterComponent<T>()
        where T : IManagedComponent
    {
        var typeHandle = typeof(T).TypeHandle.Value;

        lock (s_registeredComponents)
        {
            if (s_typeHandleToID.TryGetValue(typeHandle, out var existingID))
            {
                return existingID;
            }

            var newID = new Identifier<IManagedComponent>(s_registeredComponents.Count);
            var stableName = typeof(T).FullName ?? typeof(T).Name;
            var info = new ManagedComponentInfo
            {
                id = newID,
                isScriptComponent = typeof(ScriptComponent).IsAssignableFrom(typeof(T)),
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
    public static Identifier<IManagedComponent> GetComponentID(Type type)
    {
        var typeHandle = type.TypeHandle.Value;
        lock (s_registeredComponents)
        {
            if (s_typeHandleToID.TryGetValue(typeHandle, out var existingID))
            {
                return existingID;
            }
        }

        return Identifier<IManagedComponent>.Invalid;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ManagedComponentInfo GetComponentInfo(Identifier<IManagedComponent> typeId)
    {
        lock (s_registeredComponents)
        {
            return s_registeredComponents[typeId];
        }
    }
}

internal interface IManagedComponentStorage
{
    public Identifier<IManagedComponent> TypeID { get; }
}

internal class ManagedComponentStorage<T> : IManagedComponentStorage
    where T : IManagedComponent
{
    private readonly SlotMap<T> _storage = new(16);

    public Identifier<IManagedComponent> TypeID => ManagedComponemtnID<T>.Value;

    public Managed<T> Add(T component)
    {
        var id = _storage.Add(component, out var generation);
        return new Managed<T>(id, generation);
    }

    public void Remove(Managed<T> managed)
    {
        _storage.Remove(managed.id, managed.generation);
    }

    public ref T GetComponentReference(Managed<T> managed)
    {
        return ref _storage.GetElementReferenceAt(managed.id, managed.generation, out _);
    }
}

public abstract class ScriptComponent : IManagedComponent
{
    internal World _world = null!;
    internal Entity _entity;

    public World World => _world;
    public Entity Entity => _entity;

    protected ref T GetComponent<T>()
        where T : unmanaged, IComponentData
    {
        return ref _world.EntityManager.GetComponent<T>(_entity);
    }

    public virtual void OnCreate()
    {
    }

    public virtual void OnDestroy()
    {
    }

    public virtual void OnEnable()
    {
    }

    public virtual void OnDisable()
    {
    }

    public virtual void Start()
    {
    }

    public virtual void Update()
    {
    }

    public virtual void FixedUpdate()
    {
    }

    public virtual void LateUpdate()
    {
    }
}

public partial class EntityManager
{
    private IManagedComponentStorage[] _managedStorages = new IManagedComponentStorage[64];

    internal IManagedComponentStorage[] ManagedStorages => _managedStorages;

    private ManagedComponentStorage<T> GetOrCreateManagedComponentStorage<T>()
        where T : IManagedComponent
    {
        var id = ManagedComponemtnID<T>.Value;
        if (_managedStorages == null || _managedStorages.Length <= id.Value)
        {
            Array.Resize(ref _managedStorages, id.Value + 1);
        }

        ref var storage = ref _managedStorages[id.Value];
        storage ??= new ManagedComponentStorage<T>();

        return (ManagedComponentStorage<T>)storage;
    }

    public Managed<T> AddManagedComponent<T>(Entity entity)
        where T : IManagedComponent, new()
    {
        var instance = new T();
        if (instance is ScriptComponent scriptComponent)
        {
            scriptComponent._world = _world;
            scriptComponent._entity = entity;
            scriptComponent.OnCreate();
        }

        var managed = GetOrCreateManagedComponentStorage<T>().Add(instance);
        AddComponent(entity, managed);

        return managed;
    }

    public bool RemoveManagedComponent<T>(Entity entity)
        where T : IManagedComponent
    {
        ref var component = ref GetComponent<Managed<T>>(entity);
        if (!Unsafe.IsNullRef(ref component))
        {
            var storage = GetOrCreateManagedComponentStorage<T>();
            var componentRef = storage.GetComponentReference(component);
            if (componentRef is ScriptComponent scriptComponent)
            {
                scriptComponent.OnDestroy();
            }

            RemoveComponent<Managed<T>>(entity);
            storage.Remove(component);

            return true;
        }

        return false;
    }

    public ref T GetManagedComponent<T>(Entity entity)
        where T : IManagedComponent
    {
        ref var component = ref GetComponent<Managed<T>>(entity);
        if (Unsafe.IsNullRef(ref component))
        {
            return ref Unsafe.NullRef<T>();
        }

        return ref GetOrCreateManagedComponentStorage<T>().GetComponentReference(component);
    }

    public ref T GetManagedComponent<T>(Managed<T> managedComponent)
        where T : IManagedComponent
    {
        return ref GetOrCreateManagedComponentStorage<T>().GetComponentReference(managedComponent);
    }
}
#endif
