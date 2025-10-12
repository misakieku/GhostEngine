using Ghost.Entities.Components;
using Ghost.Entities.Query;
using Ghost.Entities.Systems;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Entities;

// TODO: Archetype system for better performance
public partial class World
{
    private static List<World> s_worlds = new(4);
    private static Queue<WorldID> s_freeWorldSlots = new();

    public static int WorldCount => s_worlds.Count - s_freeWorldSlots.Count;

    public static World Create(int entityCapacity = 16)
    {
        lock (s_worlds)
        {
            if (s_freeWorldSlots.TryDequeue(out var index))
            {
                s_worlds[index] = new World(index, entityCapacity);
            }
            else
            {
                if (s_worlds.Count >= WorldID.MaxValue)
                {
                    throw new InvalidOperationException("Maximum number of worlds reached");
                }

                index = (WorldID)s_worlds.Count;
                s_worlds.Add(new World(index, entityCapacity));
            }

            return s_worlds[index];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static World GetWorld(int index)
    {
        return s_worlds[index];
    }
}

public partial class World : IDisposable, IEquatable<World>
{
    private readonly WorldID _id;
    private readonly EntityManager _entityManager;
    private readonly ComponentStorage _componentStorage;
    private readonly SystemStorage _systemStorage;

    private bool _isDisposed = false;

    internal ComponentStorage ComponentStorage => _componentStorage;

    public WorldID ID => _id;
    public EntityManager EntityManager => _entityManager;
    public SystemStorage SystemStorage => _systemStorage;

    public event Action<World, Entity, Type>? ComponentChanged;

    private World(WorldID id, int entityCapacity)
    {
        _id = id;
        _entityManager = new EntityManager(this, entityCapacity);
        _componentStorage = new ComponentStorage(this);
        _systemStorage = new SystemStorage(this);
    }

    ~World()
    {
        Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CompRef<T> GetSingleton<T>()
        where T : unmanaged, IComponentData
    {
        ref var component = ref CollectionsMarshal.GetValueRefOrAddDefault(SingletonContainer<T>.container, _id, out _);
        return new CompRef<T>(ref component);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<ScriptComponent> QueryScript()
    {
        if (_componentStorage.ScriptComponentPool.IsInitialized)
        {
            return _componentStorage.ScriptComponentPool.ExecutionList!;
        }

        return Enumerable.Empty<ScriptComponent>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Conditional("GHOST_EDITOR")]
    public void NotifyComponentChanged(Entity entity, Type type)
    {
        ComponentChanged?.Invoke(this, entity, type);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Conditional("GHOST_EDITOR")]
    public void NotifyComponentChanged<T>(Entity entity)
        where T : unmanaged, IComponentData
    {
        NotifyComponentChanged(entity, typeof(T));
    }

    public bool Equals(World? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return _id == other._id;
    }

    public override int GetHashCode()
    {
        return _id.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        return obj is World other && Equals(other);
    }

    public static bool operator ==(World? left, World? right)
    {
        return left?.Equals(right) ?? right is null;
    }

    public static bool operator !=(World? left, World? right)
    {
        return !(left == right);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _entityManager.Dispose();
        _componentStorage.Dispose();
        _systemStorage.Dispose();

        s_freeWorldSlots.Enqueue(_id);

        _isDisposed = true;

        GC.SuppressFinalize(this);
    }
}