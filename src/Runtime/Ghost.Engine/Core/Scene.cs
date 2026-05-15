using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Engine.Components;
using Ghost.Engine.Streaming;
using Ghost.Entities;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Text;

namespace Ghost.Engine.Core;

/// <summary>
/// Represents a runtime scene - a collection of entities with the same SceneID.
/// </summary>
public struct Scene : IEquatable<Scene>
{
    public const ushort INVALID_ID = 65535;

    private ushort _id;

    public readonly ushort ID => _id;

    /// <summary>
    /// Gets whether this scene is valid.
    /// </summary>
    public readonly bool IsValid => ID != INVALID_ID;

    /// <summary>
    /// Gets an invalid scene instance.
    /// </summary>
    public static Scene Invalid => new Scene { _id = INVALID_ID };

    internal Scene(ushort id)
    {
        _id = id;
    }

    public readonly bool Equals(Scene other)
    {
        return ID == other.ID;
    }

    public readonly override bool Equals(object? obj)
    {
        return obj is Scene other && Equals(other);
    }

    public readonly override int GetHashCode()
    {
        return ID.GetHashCode();
    }

    public readonly override string ToString()
    {
        return $"Scene(ID: {ID})";
    }

    public static bool operator ==(Scene left, Scene right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Scene left, Scene right)
    {
        return !left.Equals(right);
    }
}

/// <summary>
/// Manages scenes within a world.
/// </summary>
/// <remarks>
/// This is a minimal runtime representation. All metadata (like scene names) 
/// should be stored in editor-only classes (SceneNode).
/// </remarks>
public static class SceneManager
{
    private struct BinaryEntityInfo : IDisposable
    {
        public int entityIndex;
        public int componentCount;
        public struct ComponentInfo
        {
            public UnsafeArray<int> entityFieldOffsets;
            public long dataOffset;
            public uint typeHash;
            public Identifier<IComponent> typeID;
            public int dataSize;
            public int entityFieldCount;
        }

        public UnsafeArray<ComponentInfo> components;

        public void Dispose()
        {
            for (var i = 0; i < components.Length; i++)
            {
                components[i].entityFieldOffsets.Dispose();
            }

            components.Dispose();
        }
    }

    private struct BinaryEntityInfoArray : IDisposable
    {
        public UnsafeArray<BinaryEntityInfo> data;

        public readonly ref BinaryEntityInfo this[int index] => ref data[index];

        public BinaryEntityInfoArray(int count, AllocationHandle handle)
        {
            data = new UnsafeArray<BinaryEntityInfo>(count, handle, AllocationOption.Clear);
        }

        public void Dispose()
        {
            for (var i = 0; i < data.Length; i++)
            {
                data[i].Dispose();
            }

            data.Dispose();
        }
    }

    internal struct SceneLoadResult : IDisposable
    {
        internal struct PendingEntity : IDisposable
        {
            public int fileLocalIndex;
            public ComponentSet componentSet;
            public UnsafeList<(Identifier<IComponent> typeID, UnsafeArray<byte> data)> componentData;
            public UnsafeList<(int componentIndex, UnsafeArray<int> fieldOffsets)> entityFields;

            public void Dispose()
            {
                for (int i = 0; i < componentData.Count; i++)
                {
                    componentData[i].data.Dispose();
                }

                componentSet.Dispose();
                componentData.Dispose();
                entityFields.Dispose();
            }
        }

        public UnsafeList<PendingEntity> entities;
        public Scene scene;

        public void Dispose()
        {
            for (int i = 0; i < entities.Count; i++)
            {
                entities[i].Dispose();
            }

            entities.Dispose();
        }
    }

    internal unsafe struct LoadSceneJob : IJob
    {
        public SceneContentHeader header;
        public Stream stream;

        public SceneLoadResult* result;
        public AllocationHandle allocationHandle;

        public void Execute(ref readonly JobExecutionContext ctx)
        {

        }
    }

    private static ushort s_nextSceneID;
    private static readonly Queue<ushort> s_recycledSceneIDs = new();

    private static readonly Lock s_creationLock = new();

    /// <summary>
    /// Creates a new scene in the world.
    /// </summary>
    /// <returns>The created scene.</returns>
    public static Scene CreateScene()
    {
        lock (s_creationLock)
        {
            if (!s_recycledSceneIDs.TryDequeue(out var id))
            {
                id = s_nextSceneID++;
            }

            return new Scene(id);
        }
    }

    internal static unsafe Result<JobHandle> LoadSceneIntoWorld(World world, SceneContentHeader header, Stream stream)
    {
        using var scope = AllocationManager.CreateStackScope();

        using var entityInfos = new BinaryEntityInfoArray(header.entityCount, scope.AllocationHandle);
        using var forwardMap = new UnsafeHashMap<int, Entity>(header.entityCount, scope.AllocationHandle);
        using var str = new UnsafeArray<byte>(128, scope.AllocationHandle);

        for (var i = 0; i < header.entityCount; i++)
        {
            var compCount = stream.Read<int>();
            if (compCount == 0)
            {
                continue;
            }

            var comps = new UnsafeArray<BinaryEntityInfo.ComponentInfo>(compCount, scope.AllocationHandle);

            for (var j = 0; j < compCount; j++)
            {
                var typeHash = stream.Read<uint>();
                var nameLength = stream.Read<int>();

                if (nameLength > str.Length)
                {
                    str.Resize(nameLength);
                }

                var strSpan = str.AsSpan(0, nameLength);
                stream.ReadExactly(strSpan);

                var typeName = Encoding.UTF8.GetString(strSpan);
                var dataSz = stream.Read<int>();
                var dataOff = stream.Position;
                stream.Position += dataSz;

                var fieldCount = stream.Read<int>();
                var fieldOffsets = new UnsafeArray<int>(fieldCount, scope.AllocationHandle);
                for (var f = 0; f < fieldCount; f++)
                {
                    fieldOffsets[f] = stream.Read<int>();
                }

                var typeID = ComponentRegistry.GetComponentIDByName(typeName);

                comps[j] = new BinaryEntityInfo.ComponentInfo
                {
                    dataOffset = dataOff,
                    entityFieldOffsets = fieldOffsets,
                    typeHash = typeHash,
                    typeID = typeID,
                    dataSize = dataSz,
                    entityFieldCount = fieldCount,
                };
            }

            entityInfos[i] = new BinaryEntityInfo
            {
                entityIndex = i,
                componentCount = compCount,
                components = comps,
            };
        }

        using var typeIds = new UnsafeList<Identifier<IComponent>>(32, scope.AllocationHandle);
        typeIds.Add(ComponentTypeID<SceneID>.Value);

        for (var i = 0; i < header.entityCount; i++)
        {
            ref var info = ref entityInfos[i];

            for (var j = 0; j < info.componentCount; j++)
            {
                if (info.components[j].typeID.IsValid)
                {
                    typeIds.Add(info.components[j].typeID);
                }
            }

            var set = new ComponentSetView(typeIds);
            var entity = world.EntityManager.CreateEntity(set);

            forwardMap.TryAdd(i, entity);
            typeIds.RemoveRange(1, typeIds.Count - 1);
        }

        var activeScene = CreateScene();

        for (var i = 0; i < header.entityCount; i++)
        {
            if (!forwardMap.TryGetValue(i, out var entity))
            {
                continue;
            }

            world.EntityManager.SetComponent(entity, new SceneID { value = activeScene.ID });

            using var compScope = AllocationManager.CreateStackScope();
            var info = entityInfos[i];

            for (var j = 0; j < info.componentCount; j++)
            {
                var comp = info.components[j];
                if (!comp.typeID.IsValid)
                {
                    continue;
                }

                var compSize = ComponentRegistry.GetComponentInfo(comp.typeID).size;

                stream.Position = comp.dataOffset;

                using var src = stream.ReadMemory(compSize, compScope.AllocationHandle);
                world.EntityManager.SetComponent(entity, comp.typeID, src.GetUnsafePtr());
            }
        }

        for (var i = 0; i < header.entityCount; i++)
        {
            if (!forwardMap.TryGetValue(i, out var entity))
            {
                continue;
            }

            var info = entityInfos[i];
            for (var j = 0; j < info.componentCount; j++)
            {
                var comp = info.components[j];
                if (!comp.typeID.IsValid || comp.entityFieldCount == 0)
                {
                    continue;
                }

                var pComponent = world.EntityManager.GetComponent(entity, comp.typeID);
                if (pComponent == null)
                {
                    continue;
                }

                for (var f = 0; f < comp.entityFieldCount; f++)
                {
                    var fieldOffset = comp.entityFieldOffsets[f];
                    var pField = (byte*)pComponent + fieldOffset;
                    var fileLocalIndex = *(int*)pField;
                    if (!forwardMap.TryGetValue(fileLocalIndex, out var remappedEntity))
                    {
                        remappedEntity = Entity.Invalid;
                    }

                    *(Entity*)pField = remappedEntity;
                }
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// Destroys all entities belonging to the specified scene.
    /// </summary>
    /// <param name="scene">The scene to unload.</param>
    /// <param name="world">The world containing the entities.</param>
    public static void UnloadScene(Scene scene, World world)
    {
        var queryID = new QueryBuilder().WithAll<SceneID>().Build(world);
        ref var query = ref world.ComponentManager.GetEntityQueryReference(queryID);

        using var scope = AllocationManager.CreateStackScope();
        var entitiesToDestroy = new UnsafeList<Entity>(128, scope.AllocationHandle);

        // Iterate through all matching entities
        foreach (var chunk in query.GetChunkIterator())
        {
            var entities = chunk.GetEntities();
            var sceneIDs = chunk.GetComponentData<SceneID>();

            for (var i = 0; i < chunk.EntityCount; i++)
            {
                if (sceneIDs[i].value == scene.ID)
                {
                    entitiesToDestroy.Add(entities[i]);
                }
            }
        }

        world.EntityManager.DestroyEntities(entitiesToDestroy.AsSpan());
        s_recycledSceneIDs.Enqueue(scene.ID);
    }

    public static void ReleaseScene(Scene scene)
    {
        s_recycledSceneIDs.Enqueue(scene.ID);
    }

    /// <summary>
    /// Gets all entities belonging to the specified scene.
    /// </summary>
    /// <param name="scene">The scene to query.</param>
    /// <param name="world">The world containing the entities.</param>
    /// <param name="entities">Span to store the entities.</param>
    /// <returns>The number of entities written to the span.</returns>
    public static UnsafeList<Entity> GetSceneEntities(Scene scene, World world, AllocationHandle handle)
    {
        var queryID = new QueryBuilder().WithAll<SceneID>().Build(world);
        ref var query = ref world.ComponentManager.GetEntityQueryReference(queryID);

        var entities = new UnsafeList<Entity>(128, handle);

        // Iterate through all matching entities
        foreach (var chunk in query.GetChunkIterator())
        {
            var chunkEntities = chunk.GetEntities();
            var sceneIDs = chunk.GetComponentData<SceneID>();

            for (var i = 0; i < chunk.EntityCount; i++)
            {
                if (sceneIDs[i].value == scene.ID)
                {
                    entities.Add(chunkEntities[i]);
                }
            }
        }

        return entities;
    }
}
