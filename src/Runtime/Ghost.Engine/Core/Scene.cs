using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Engine.Components;
using Ghost.Engine.Streaming;
using Ghost.Entities;
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
    public static Scene Invalid => new() { _id = INVALID_ID };

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
    internal struct SceneLoadResult : IDisposable
    {
        internal struct PendingEntity : IDisposable
        {
            public int fileLocalIndex;
            public UnsafeList<Identifier<IComponent>> componentTypeIDs;
            public UnsafeList<(Identifier<IComponent> typeID, UnsafeArray<byte> data)> componentData;
            public UnsafeList<(int componentDataIndex, UnsafeArray<int> fieldOffsets)> entityFields;

            public void Dispose()
            {
                componentTypeIDs.Dispose();
                for (int i = 0; i < componentData.Count; i++)
                {
                    componentData[i].data.Dispose();
                }
                componentData.Dispose();
                for (int i = 0; i < entityFields.Count; i++)
                {
                    entityFields[i].fieldOffsets.Dispose();
                }
                entityFields.Dispose();
            }
        }

        public UnsafeArray<PendingEntity> entities;

        public void Dispose()
        {
            for (int i = 0; i < entities.Length; i++)
            {
                entities[i].Dispose();
            }
            entities.Dispose();
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

    internal static SceneLoadResult ParseSceneData(SceneContentHeader header, Stream stream, AllocationHandle allocationHandle)
    {
        var result = new SceneLoadResult
        {
            entities = new UnsafeArray<SceneLoadResult.PendingEntity>(header.entityCount, allocationHandle)
        };

        using var scope = AllocationManager.CreateStackScope();
        using var str = new UnsafeArray<byte>(128, scope.AllocationHandle);

        for (var i = 0; i < header.entityCount; i++)
        {
            var compCount = stream.Read<int>();

            if (compCount == 0)
            {
                result.entities[i] = new SceneLoadResult.PendingEntity
                {
                    fileLocalIndex = i,
                    componentTypeIDs = new UnsafeList<Identifier<IComponent>>(0, allocationHandle),
                    componentData = new UnsafeList<(Identifier<IComponent> typeID, UnsafeArray<byte> data)>(0, allocationHandle),
                    entityFields = new UnsafeList<(int componentDataIndex, UnsafeArray<int> fieldOffsets)>(0, allocationHandle)
                };
                continue;
            }

            var pending = new SceneLoadResult.PendingEntity
            {
                fileLocalIndex = i,
                componentTypeIDs = new UnsafeList<Identifier<IComponent>>(compCount, allocationHandle),
                componentData = new UnsafeList<(Identifier<IComponent> typeID, UnsafeArray<byte> data)>(compCount, allocationHandle),
                entityFields = new UnsafeList<(int componentDataIndex, UnsafeArray<int> fieldOffsets)>(compCount, allocationHandle)
            };

            for (var j = 0; j < compCount; j++)
            {
                var typeHash = stream.Read<uint>();
                var nameLength = stream.Read<int>();

                if (nameLength > str.Length)
                {
                    str.Resize(nameLength);
                }

                var strSpan = str.AsSpan(0, nameLength);
                stream.ReadExactly(strSpan.Slice(0, nameLength));

                var typeName = Encoding.UTF8.GetString(strSpan);

                var dataSz = stream.Read<int>();
                var compData = new UnsafeArray<byte>(dataSz, allocationHandle);
                stream.ReadExactly(compData);

                var fieldCount = stream.Read<int>();

                UnsafeArray<int> fieldOffsets = default;
                if (fieldCount > 0)
                {
                    fieldOffsets = new UnsafeArray<int>(fieldCount, allocationHandle);
                    for (var f = 0; f < fieldCount; f++)
                    {
                        fieldOffsets[f] = stream.Read<int>();
                    }
                }

                var typeID = ComponentRegistry.GetComponentIDByName(typeName);
                if (typeID.IsValid)
                {
                    pending.componentTypeIDs.Add(typeID);
                    pending.componentData.Add((typeID, compData));
                    if (fieldCount > 0)
                    {
                        pending.entityFields.Add((pending.componentData.Count - 1, fieldOffsets));
                    }
                }
                else
                {
                    compData.Dispose();
                    if (fieldCount > 0) fieldOffsets.Dispose();
                }
            }

            result.entities[i] = pending;
        }

        return result;
    }

    internal static unsafe Result<int> MaterializeScene(World world, ref SceneLoadResult result, Scene scene)
    {
        using var scope = AllocationManager.CreateStackScope();
        using var forwardMap = new UnsafeHashMap<int, Entity>(result.entities.Length, scope.AllocationHandle);
        using var sharedCom = new SharedComponentSet(256, scope.AllocationHandle);

        // Create entities and set SceneID
        for (var i = 0; i < result.entities.Length; i++)
        {
            ref var pending = ref result.entities[i];

            using var typeIds = new UnsafeList<Identifier<IComponent>>(pending.componentTypeIDs.Count + 1, scope.AllocationHandle);
            typeIds.Add(ComponentTypeID<SceneID>.Value);
            for (int j = 0; j < pending.componentTypeIDs.Count; j++)
            {
                typeIds.Add(pending.componentTypeIDs[j]);
            }

            sharedCom.With(new SceneID { value = scene.ID });

            var set = new ComponentSetView(typeIds, sharedCom);
            var entity = world.EntityManager.CreateEntity(set);
            forwardMap.TryAdd(pending.fileLocalIndex, entity);

            sharedCom.Reset();
        }

        // Set component data
        for (var i = 0; i < result.entities.Length; i++)
        {
            ref var pending = ref result.entities[i];
            if (!forwardMap.TryGetValue(pending.fileLocalIndex, out var entity))
                continue;

            for (var j = 0; j < pending.componentData.Count; j++)
            {
                var (typeID, data) = pending.componentData[j];
                world.EntityManager.SetComponent(entity, typeID, data.GetUnsafePtr());
            }
        }

        // Remap entity references
        for (var i = 0; i < result.entities.Length; i++)
        {
            ref var pending = ref result.entities[i];
            if (!forwardMap.TryGetValue(pending.fileLocalIndex, out var entity))
                continue;

            for (var j = 0; j < pending.entityFields.Count; j++)
            {
                var (componentDataIndex, fieldOffsets) = pending.entityFields[j];
                var compTypeID = pending.componentData[componentDataIndex].typeID;

                var pComponent = world.EntityManager.GetComponent(entity, compTypeID);
                if (pComponent == null)
                    continue;

                for (var f = 0; f < fieldOffsets.Length; f++)
                {
                    var fieldOffset = fieldOffsets[f];
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

        return Result.Success(result.entities.Length);
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
        using var ecb = new EntityCommandBuffer(512, scope.AllocationHandle);

        // Iterate through all matching entities
        foreach (var chunk in query.GetChunkIterator())
        {
            ref readonly var sceneID = ref chunk.GetSharedComponent<SceneID>();
            if (sceneID.value == scene.ID)
            {
                ecb.DestroyEntities(chunk.GetEntities());
            }
        }

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
            ref readonly var sceneID = ref chunk.GetSharedComponent<SceneID>();
            if (sceneID.value == scene.ID)
            {
                entities.AddRange(chunk.GetEntities());
            }
        }

        return entities;
    }
}
