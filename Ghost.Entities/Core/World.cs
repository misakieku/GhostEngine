using Ghost.Entities.Helpers;
using Misaki.HighPerformance.Unsafe.Collections;

namespace Ghost.Entities.Core;

public partial struct World
{
    public static UnsafeArray<World> Worlds
    {
        get;
        private set;
    } = new(4, AllocationType.UnInitialized);

    public static UnsafeQueue<WorldID> FreeIndices
    {
        get;
        private set;
    } = new(4, AllocationType.UnInitialized);

    public static ushort Count
    {
        get;
        private set;
    }

    public static World Create(int chunkSizeInBytes = 16384, int minimumAmountOfEntitiesPerChunk = 100, int archetypeCapacity = 2, int entityCapacity = 64)
    {
        lock (ThreadLocker.WorldLock)
        {
            var recycle = FreeIndices.TryDequeue(out var id);
            var recycledId = recycle ? id : Count;

            var world = new World(recycledId, chunkSizeInBytes, minimumAmountOfEntitiesPerChunk, archetypeCapacity, entityCapacity);

            if (recycledId >= Worlds.Size)
            {
                var newCapacity = Worlds.Size * 2;
                Worlds.ReAlloc(newCapacity);
            }

            Worlds[recycledId] = world;
            Count++;
            return world;
        }
    }
}

public partial struct World
{
    /// <summary>
    ///     The unique <see cref="World"/> ID.
    /// </summary>
    public int Id
    {
        get;
    }

    /// <summary>
    ///     The amount of <see cref="Entity"/>s currently stored by this <see cref="World"/>.
    /// </summary>
    public int Size
    {
        get; internal set;
    }

    /// <summary>
    ///     The available <see cref="Entity"/> capacity of this <see cref="World"/>.
    /// </summary>
    public int Capacity
    {
        get; internal set;
    }

    ///// <summary>
    /////     All <see cref="Archetype"/>s that exist in this <see cref="World"/>.
    ///// </summary>
    //public Archetypes Archetypes
    //{
    //    get;
    //}

    ///// <summary>
    /////     Maps an <see cref="Entity"/> to its <see cref="EntityInfo"/> for quick lookup.
    ///// </summary>
    //internal EntityInfoStorage EntityInfo
    //{
    //    get;
    //}

    ///// <summary>
    /////     Stores recycled <see cref="Entity"/> IDs and their last version.
    ///// </summary>
    //internal PooledQueue<RecycledEntity> RecycledIds
    //{
    //    get; set;
    //}

    ///// <summary>
    /////     A cache to map <see cref="QueryDescription"/> to their <see cref="Core.Query"/>, to avoid allocs.
    ///// </summary>
    //internal PooledDictionary<QueryDescription, Query> QueryCache
    //{
    //    get; set;
    //}

    /// <summary>
    ///     The <see cref="Chunk"/> size of each <see cref="Archetype"/> in bytes.
    /// <remarks>For the best cache optimisation use values that are divisible by 16Kb.</remarks>
    /// </summary>
    public int BaseChunkSize { get; private set; } = 16_384;

    /// <summary>
    ///     The minimum number of <see cref="Arch.Core.Entity"/>'s that should fit into a <see cref="Chunk"/> within all <see cref="Archetype"/>s.
    ///     On the basis of this, the <see cref="Archetypes"/>s chunk size may increase.
    /// </summary>
    public int BaseChunkEntityCount { get; private set; } = 100;

    private World(int id, int baseChunkSize, int baseChunkEntityCount, int archetypeCapacity, int entityCapacity)
    {
        Id = id;

        // Mapping.
        //GroupToArchetype = new PooledDictionary<int, Archetype>(archetypeCapacity);

        // Entity stuff.
        //Archetypes = new Archetypes(archetypeCapacity);
        //EntityInfo = new EntityInfoStorage(baseChunkSize, entityCapacity);
        //RecycledIds = new PooledQueue<RecycledEntity>(entityCapacity);

        // Query.
        //QueryCache = new PooledDictionary<QueryDescription, Query>(archetypeCapacity);

        // Multithreading/Jobs.
        //JobHandles = new PooledList<JobHandle>(Environment.ProcessorCount);
        //JobsCache = new List<IJob>(Environment.ProcessorCount);

        // Config
        BaseChunkSize = baseChunkSize;
        BaseChunkEntityCount = baseChunkEntityCount;
    }
}