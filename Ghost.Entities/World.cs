using Ghost.Entities.Query;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Entities;

// TODO: Archetype system for better performance
public partial class World
{
    private static List<World> s_worlds = new(2);
    private static Queue<WorldID> s_freeWorldSlots = new();

    private static int s_maxWorldCount = (int)MathF.Pow(2, Entity.WORLD_INDEX_BITS);

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
                if (s_worlds.Count >= s_maxWorldCount)
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

public partial class World : IDisposable
{
    private readonly WorldID _id;
    private readonly EntityManager _entityManager;
    private readonly ComponentStorage _componentStorage;
    private readonly SystemStorage _systemStorage;

    internal ComponentStorage ComponentStorage => _componentStorage;

    public WorldID ID => _id;
    public EntityManager EntityManager => _entityManager;
    public SystemStorage SystemStorage => _systemStorage;

    private World(WorldID id, int entityCapacity)
    {
        _id = id;
        _entityManager = new EntityManager(this, entityCapacity);
        _componentStorage = new ComponentStorage(this);
        _systemStorage = new SystemStorage(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Ref<T> GetSingleton<T>()
        where T : struct, IComponentData
    {
        ref var component = ref CollectionsMarshal.GetValueRefOrAddDefault(SingletonContainer<T>.container, _id, out _);
        return new Ref<T>(ref component);
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

    public void Dispose()
    {
        _entityManager.Dispose();
        _componentStorage.Dispose();
        _systemStorage.Dispose();

        s_freeWorldSlots.Enqueue(_id);
    }
}