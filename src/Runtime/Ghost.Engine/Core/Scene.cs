using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Engine.Components;
using Ghost.Engine.Streaming;
using Ghost.Entities;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Collections.Concurrent;
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

public class LoadedSceneData : IDisposable
{
    public struct EntityData : IDisposable
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

    public UnsafeArray<EntityData> entities;

    public void Dispose()
    {
        for (int i = 0; i < entities.Length; i++)
        {
            entities[i].Dispose();
        }

        entities.Dispose();

        GC.SuppressFinalize(this);
    }
}

public enum SceneLoadStatus
{
    Queued = 0,
    WaitingForDependencies = 1,
    Parsing = 2,
    WaitingForMaterialization = 3,
    Materializing = 4,
    Completed = 5,
    Failed = 6,
    Canceled = 7,
}

public struct SceneLoadOptions
{
    public bool DeferMaterialization;
    public int MaxEntitiesPerFrame;
    public int Priority;

    public readonly bool AutoMaterialize => !DeferMaterialization;
}

public readonly struct SceneMaterializeBudget
{
    public readonly int MaxScenes;
    public readonly int MaxEntities;

    public SceneMaterializeBudget(int maxEntities, int maxScenes = 0)
    {
        MaxEntities = maxEntities;
        MaxScenes = maxScenes;
    }

    public static SceneMaterializeBudget Unlimited => new(int.MaxValue, int.MaxValue);
}

public sealed class SceneLoadOperation
{
    private readonly PendingSceneLoad _pendingLoad;

    internal PendingSceneLoad PendingLoad => _pendingLoad;

    public SceneLoadStatus Status => _pendingLoad.Status;
    public float Progress => _pendingLoad.Progress;
    public Scene Scene => _pendingLoad.Scene;
    public string? ErrorMessage => _pendingLoad.ErrorMessage;
    public bool IsParsed => _pendingLoad.Status >= SceneLoadStatus.WaitingForMaterialization && _pendingLoad.Status < SceneLoadStatus.Failed;
    public bool IsMaterialized => _pendingLoad.Status == SceneLoadStatus.Completed;
    public bool IsCompleted => _pendingLoad.Status is SceneLoadStatus.Completed or SceneLoadStatus.Failed or SceneLoadStatus.Canceled;

    internal SceneLoadOperation(PendingSceneLoad pendingLoad)
    {
        _pendingLoad = pendingLoad;
    }

    public void Cancel()
    {
        _pendingLoad.Cancel();
    }
}

internal sealed class PendingSceneLoad : IDisposable
{
    private enum MaterializePhase
    {
        NotStarted = 0,
        CreateEntities = 1,
        SetComponents = 2,
        RemapEntityReferences = 3,
        Completed = 4,
    }

    private readonly AssetEntry _sceneEntry;
    private readonly SceneLoadOptions _options;

    private LoadedSceneData? _loadedData;
    private UnsafeArray<Entity> _fileLocalToRuntimeEntity;
    private MaterializePhase _phase;
    private int _nextCreateIndex;
    private int _nextSetComponentIndex;
    private int _nextRemapIndex;
    private int _status;
    private int _releasedSceneEntry;
    private bool _singleResetApplied;
    private bool _disposed;

    public World World { get; }
    public AssetRef<Scene> SceneAsset { get; }
    public SceneLoadingType LoadingType { get; }
    public Scene Scene { get; private set; }
    public SceneLoadOptions Options => _options;
    public SceneLoadStatus Status => (SceneLoadStatus)Volatile.Read(ref _status);
    public string? ErrorMessage { get; private set; }
    public bool IsTerminal => Status is SceneLoadStatus.Completed or SceneLoadStatus.Failed or SceneLoadStatus.Canceled;

    public float Progress
    {
        get
        {
            var status = Status;
            if (status == SceneLoadStatus.Completed)
            {
                return 1.0f;
            }

            var loadedData = _loadedData;
            if (loadedData == null || !loadedData.entities.IsCreated || loadedData.entities.Length == 0)
            {
                return status switch
                {
                    SceneLoadStatus.Queued => 0.0f,
                    SceneLoadStatus.WaitingForDependencies => 0.1f,
                    SceneLoadStatus.Parsing => 0.25f,
                    SceneLoadStatus.WaitingForMaterialization => 0.5f,
                    SceneLoadStatus.Materializing => 0.75f,
                    _ => 0.0f,
                };
            }

            var entityCount = loadedData.entities.Length;
            var completedEntities = Math.Min(entityCount * 3, _nextCreateIndex + _nextSetComponentIndex + _nextRemapIndex);
            return 0.5f + (0.5f * completedEntities / (entityCount * 3));
        }
    }

    public PendingSceneLoad(World world, AssetRef<Scene> sceneAsset, SceneLoadingType loadingType, SceneLoadOptions options, AssetEntry sceneEntry)
    {
        World = world;
        SceneAsset = sceneAsset;
        LoadingType = loadingType;
        _options = options;
        _sceneEntry = sceneEntry;
        Scene = Scene.Invalid;
        _status = (int)SceneLoadStatus.Queued;
    }

    public void SetStatus(SceneLoadStatus status)
    {
        Volatile.Write(ref _status, (int)status);
    }

    public void CompleteParsing(LoadedSceneData loadedData)
    {
        _loadedData = loadedData;
        SetStatus(SceneLoadStatus.WaitingForMaterialization);
    }

    public void Fail(string message)
    {
        ErrorMessage = message;
        SetStatus(SceneLoadStatus.Failed);
        Dispose();
    }

    public void Cancel()
    {
        var status = Status;
        if (status is SceneLoadStatus.Completed or SceneLoadStatus.Failed or SceneLoadStatus.Canceled)
        {
            return;
        }

        SetStatus(SceneLoadStatus.Canceled);
        if (status >= SceneLoadStatus.WaitingForMaterialization)
        {
            Dispose();
        }
    }

    public int Materialize(int maxEntities)
    {
        if (maxEntities <= 0 || IsTerminal)
        {
            return 0;
        }

        var loadedData = _loadedData;
        if (loadedData == null)
        {
            return 0;
        }

        if (!_singleResetApplied && LoadingType == SceneLoadingType.Single)
        {
            SceneManager.ReleaseMaterializedSceneReferences(World);
            World.Reset();
            _singleResetApplied = true;
        }

        if (_phase == MaterializePhase.NotStarted)
        {
            Scene = SceneManager.CreateScene();
            _fileLocalToRuntimeEntity = new UnsafeArray<Entity>(loadedData.entities.Length, AllocationHandle.Persistent);
            _phase = MaterializePhase.CreateEntities;
            SetStatus(SceneLoadStatus.Materializing);
        }

        var consumed = 0;
        while (consumed < maxEntities && _phase != MaterializePhase.Completed)
        {
            switch (_phase)
            {
                case MaterializePhase.CreateEntities:
                    consumed += MaterializeCreateEntities(loadedData, maxEntities - consumed);
                    if (_nextCreateIndex >= loadedData.entities.Length)
                    {
                        _phase = MaterializePhase.SetComponents;
                    }
                    break;

                case MaterializePhase.SetComponents:
                    consumed += MaterializeSetComponents(loadedData, maxEntities - consumed);
                    if (_nextSetComponentIndex >= loadedData.entities.Length)
                    {
                        _phase = MaterializePhase.RemapEntityReferences;
                    }
                    break;

                case MaterializePhase.RemapEntityReferences:
                    consumed += MaterializeRemapEntityReferences(loadedData, maxEntities - consumed);
                    if (_nextRemapIndex >= loadedData.entities.Length)
                    {
                        CompleteMaterialization();
                    }
                    break;
            }
        }

        return consumed;
    }

    private int MaterializeCreateEntities(LoadedSceneData loadedData, int maxEntities)
    {
        using var scope = AllocationManager.CreateStackScope();
        using var sharedCom = new SharedComponentSet(256, scope.AllocationHandle);

        var consumed = 0;
        while (consumed < maxEntities && _nextCreateIndex < loadedData.entities.Length)
        {
            ref var pending = ref loadedData.entities[_nextCreateIndex];

            using var typeIds = new UnsafeList<Identifier<IComponent>>(pending.componentTypeIDs.Count + 1, scope.AllocationHandle);
            typeIds.Add(ComponentTypeID<SceneID>.Value);
            for (var i = 0; i < pending.componentTypeIDs.Count; i++)
            {
                typeIds.Add(pending.componentTypeIDs[i]);
            }

            sharedCom.With(new SceneID { value = Scene.ID });

            var set = new ComponentSetView(typeIds, sharedCom);
            var entity = World.EntityManager.CreateEntity(set);
            _fileLocalToRuntimeEntity[pending.fileLocalIndex] = entity;

            sharedCom.Reset();
            _nextCreateIndex++;
            consumed++;
        }

        return consumed;
    }

    private unsafe int MaterializeSetComponents(LoadedSceneData loadedData, int maxEntities)
    {
        var consumed = 0;
        while (consumed < maxEntities && _nextSetComponentIndex < loadedData.entities.Length)
        {
            ref var pending = ref loadedData.entities[_nextSetComponentIndex];
            var entity = _fileLocalToRuntimeEntity[pending.fileLocalIndex];
            if (entity.IsValid)
            {
                for (var i = 0; i < pending.componentData.Count; i++)
                {
                    var (typeID, data) = pending.componentData[i];
                    World.EntityManager.SetComponent(entity, typeID, data.GetUnsafePtr());
                }
            }

            _nextSetComponentIndex++;
            consumed++;
        }

        return consumed;
    }

    private unsafe int MaterializeRemapEntityReferences(LoadedSceneData loadedData, int maxEntities)
    {
        var consumed = 0;
        while (consumed < maxEntities && _nextRemapIndex < loadedData.entities.Length)
        {
            ref var pending = ref loadedData.entities[_nextRemapIndex];
            var entity = _fileLocalToRuntimeEntity[pending.fileLocalIndex];
            if (entity.IsValid)
            {
                for (var i = 0; i < pending.entityFields.Count; i++)
                {
                    var (componentDataIndex, fieldOffsets) = pending.entityFields[i];
                    var compTypeID = pending.componentData[componentDataIndex].typeID;

                    var pComponent = World.EntityManager.GetComponent(entity, compTypeID);
                    if (pComponent == null)
                    {
                        continue;
                    }

                    for (var f = 0; f < fieldOffsets.Length; f++)
                    {
                        var fieldOffset = fieldOffsets[f];
                        var pField = (byte*)pComponent + fieldOffset;
                        var fileLocalIndex = *(int*)pField;
                        var remappedEntity = fileLocalIndex >= 0 && fileLocalIndex < _fileLocalToRuntimeEntity.Length ?
                            _fileLocalToRuntimeEntity[fileLocalIndex] :
                            Entity.Invalid;

                        *(Entity*)pField = remappedEntity;
                    }
                }
            }

            _nextRemapIndex++;
            consumed++;
        }

        return consumed;
    }

    private void CompleteMaterialization()
    {
        _phase = MaterializePhase.Completed;
        SceneManager.RegisterMaterializedScene(this);

        _loadedData?.Dispose();
        _loadedData = null;

        if (_fileLocalToRuntimeEntity.IsCreated)
        {
            _fileLocalToRuntimeEntity.Dispose();
        }

        SetStatus(SceneLoadStatus.Completed);
    }

    public void ReleaseMaterializedReference()
    {
        if (Interlocked.Exchange(ref _releasedSceneEntry, 1) == 0)
        {
            _sceneEntry.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _loadedData?.Dispose();
        _loadedData = null;

        if (_fileLocalToRuntimeEntity.IsCreated)
        {
            _fileLocalToRuntimeEntity.Dispose();
        }

        if (Status != SceneLoadStatus.Completed)
        {
            ReleaseMaterializedReference();
        }

        _disposed = true;
    }
}

/// <summary>
/// Manages scenes within a world.
/// </summary>
public static class SceneManager
{
    private readonly struct SceneKey : IEquatable<SceneKey>
    {
        private readonly Identifier<World> _worldID;
        private readonly ushort _sceneID;

        public SceneKey(World world, Scene scene)
        {
            _worldID = world.ID;
            _sceneID = scene.ID;
        }

        public bool Equals(SceneKey other)
        {
            return _worldID == other._worldID && _sceneID == other._sceneID;
        }

        public override bool Equals(object? obj)
        {
            return obj is SceneKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_worldID, _sceneID);
        }
    }

    private static ushort s_nextSceneID;
    private static readonly Queue<ushort> s_recycledSceneIDs = new();

    private static readonly Lock s_creationLock = new();
    private static readonly Lock s_loadedScenesLock = new();
    private static readonly ConcurrentQueue<PendingSceneLoad> s_pendingMaterialization = new();
    private static readonly Dictionary<SceneKey, PendingSceneLoad> s_loadedScenes = new();

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

    internal static Result<LoadedSceneData> ParseSceneData<T>(SceneContentHeader header, ref T reader, AllocationHandle allocationHandle)
        where T : struct, IBufferReader
    {
        var result = new LoadedSceneData
        {
            entities = new UnsafeArray<LoadedSceneData.EntityData>(header.entityCount, allocationHandle)
        };

        try
        {
            using var scope = AllocationManager.CreateStackScope();
            using var str = new UnsafeArray<byte>(128, scope.AllocationHandle);

            for (var i = 0; i < header.entityCount; i++)
            {
                var compCount = reader.Read<int>();

                if (compCount == 0)
                {
                    result.entities[i] = new LoadedSceneData.EntityData
                    {
                        fileLocalIndex = i,
                        componentTypeIDs = new UnsafeList<Identifier<IComponent>>(0, allocationHandle),
                        componentData = new UnsafeList<(Identifier<IComponent> typeID, UnsafeArray<byte> data)>(0, allocationHandle),
                        entityFields = new UnsafeList<(int componentDataIndex, UnsafeArray<int> fieldOffsets)>(0, allocationHandle)
                    };
                    continue;
                }

                var pending = new LoadedSceneData.EntityData
                {
                    fileLocalIndex = i,
                    componentTypeIDs = new UnsafeList<Identifier<IComponent>>(compCount, allocationHandle),
                    componentData = new UnsafeList<(Identifier<IComponent> typeID, UnsafeArray<byte> data)>(compCount, allocationHandle),
                    entityFields = new UnsafeList<(int componentDataIndex, UnsafeArray<int> fieldOffsets)>(compCount, allocationHandle)
                };

                for (var j = 0; j < compCount; j++)
                {
                    var typeHash = reader.Read<uint>();
                    var nameLength = reader.Read<int>();

                    if (nameLength > str.Length)
                    {
                        str.Resize(nameLength);
                    }

                    var strSpan = str.AsSpan(0, nameLength);
                    reader.ReadExactly(strSpan.Slice(0, nameLength));

                    var typeName = Encoding.UTF8.GetString(strSpan);

                    var dataSz = reader.Read<int>();
                    var compData = new UnsafeArray<byte>(dataSz, allocationHandle);
                    reader.ReadExactly(compData.AsSpan());

                    var fieldCount = reader.Read<int>();

                    UnsafeArray<int> fieldOffsets = default;
                    if (fieldCount > 0)
                    {
                        fieldOffsets = new UnsafeArray<int>(fieldCount, allocationHandle);
                        for (var f = 0; f < fieldCount; f++)
                        {
                            fieldOffsets[f] = reader.Read<int>();
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
                        if (fieldCount > 0)
                        {
                            fieldOffsets.Dispose();
                        }
                    }
                }

                result.entities[i] = pending;
            }

            return result;
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    internal static Result<LoadedSceneData> ParseSceneData(SceneContentHeader header, Stream stream, AllocationHandle allocationHandle)
    {
        var reader = new StreamBufferReader(stream);
        return ParseSceneData(header, ref reader, allocationHandle);
    }

    internal unsafe static Result<LoadedSceneData> ParseSceneData(SceneContentHeader header, void* buffer, nuint size, AllocationHandle allocationHandle)
    {
        var reader = new BufferReader((byte*)buffer, size);
        return ParseSceneData(header, ref reader, allocationHandle);
    }

    internal static void EnqueuePendingScene(PendingSceneLoad pendingSceneLoad)
    {
        s_pendingMaterialization.Enqueue(pendingSceneLoad);
    }

    internal static void RegisterMaterializedScene(PendingSceneLoad pendingSceneLoad)
    {
        lock (s_loadedScenesLock)
        {
            s_loadedScenes[new SceneKey(pendingSceneLoad.World, pendingSceneLoad.Scene)] = pendingSceneLoad;
        }
    }

    internal static void ReleaseMaterializedSceneReferences(World world)
    {
        lock (s_loadedScenesLock)
        {
            foreach (var (key, pendingLoad) in s_loadedScenes.ToArray())
            {
                if (pendingLoad.World != world)
                {
                    continue;
                }

                pendingLoad.ReleaseMaterializedReference();
                s_recycledSceneIDs.Enqueue(pendingLoad.Scene.ID);
                s_loadedScenes.Remove(key);
            }
        }
    }

    public static void MaterializePendingScenes(World world, SceneMaterializeBudget budget = default)
    {
        var maxScenes = budget.MaxScenes > 0 ? budget.MaxScenes : int.MaxValue;
        var remainingEntities = budget.MaxEntities > 0 ? budget.MaxEntities : int.MaxValue;
        var pendingCount = s_pendingMaterialization.Count;
        var processedScenes = 0;

        for (var i = 0; i < pendingCount && processedScenes < maxScenes && remainingEntities > 0; i++)
        {
            if (!s_pendingMaterialization.TryDequeue(out var pendingLoad))
            {
                break;
            }

            if (pendingLoad.World != world || pendingLoad.IsTerminal)
            {
                if (!pendingLoad.IsTerminal)
                {
                    s_pendingMaterialization.Enqueue(pendingLoad);
                }

                continue;
            }

            var sceneBudget = pendingLoad.Options.MaxEntitiesPerFrame > 0 ?
                Math.Min(remainingEntities, pendingLoad.Options.MaxEntitiesPerFrame) :
                remainingEntities;

            var consumed = pendingLoad.Materialize(sceneBudget);
            remainingEntities -= consumed;

            if (!pendingLoad.IsTerminal)
            {
                s_pendingMaterialization.Enqueue(pendingLoad);
            }

            processedScenes++;
        }
    }

    /// <summary>
    /// Materializes the loaded scene data into actual entities in the world, setting their components and remapping entity references.
    /// </summary>
    /// <remarks>
    /// This method create entities directly into the world. Must ensure it's the safe to perform such strcture changes before calling this method (e.g. not in the middle of a system update that might be iterating over entities).
    /// </remarks>
    /// <param name="world">The world into which to materialize the scene data.</param>
    /// <param name="result">The loaded scene data.</param>
    /// <param name="scene">The scene to which the entities belong.</param>
    /// <param name="startEntityIndex">The index of the first entity to materialize.</param>
    /// <param name="length">The number of entities to materialize.</param>
    public static unsafe void MaterializeScene(World world, ref readonly LoadedSceneData result, Scene scene, int startEntityIndex, int length)
    {
        if (startEntityIndex < 0 || startEntityIndex + length > result.entities.Length)
        {
            Logger.Error($"Invalid entity index range for materialization: start={startEntityIndex}, length={length}, total={result.entities.Length}");
            return;
        }

        using var scope = AllocationManager.CreateStackScope();
        using var forwardMap = new UnsafeHashMap<int, Entity>(result.entities.Length, scope.AllocationHandle);
        using var sharedCom = new SharedComponentSet(256, scope.AllocationHandle);

        // Create entities and set SceneID
        for (var i = startEntityIndex; i < startEntityIndex + length; i++)
        {
            ref var pending = ref result.entities[i];

            using var typeIds = new UnsafeList<Identifier<IComponent>>(pending.componentTypeIDs.Count + 1, scope.AllocationHandle);
            typeIds.Add(ComponentTypeID<SceneID>.Value);
            if (pending.componentTypeIDs.Count > 0)
            {
                typeIds.AddRange(pending.componentTypeIDs);
            }

            sharedCom.With(new SceneID { value = scene.ID });

            var set = new ComponentSetView(typeIds, sharedCom);
            var entity = world.EntityManager.CreateEntity(set);
            forwardMap.TryAdd(pending.fileLocalIndex, entity);

            sharedCom.Reset();
        }

        // Set component data
        for (var i = startEntityIndex; i < startEntityIndex + length; i++)
        {
            ref var pending = ref result.entities[i];
            if (!forwardMap.TryGetValue(pending.fileLocalIndex, out var entity))
            {
                continue;
            }

            for (var j = 0; j < pending.componentData.Count; j++)
            {
                var (typeID, data) = pending.componentData[j];
                world.EntityManager.SetComponent(entity, typeID, data.GetUnsafePtr());
            }
        }

        // Remap entity references
        for (var i = startEntityIndex; i < startEntityIndex + length; i++)
        {
            ref var pending = ref result.entities[i];
            if (!forwardMap.TryGetValue(pending.fileLocalIndex, out var entity))
            {
                continue;
            }

            for (var j = 0; j < pending.entityFields.Count; j++)
            {
                var (componentDataIndex, fieldOffsets) = pending.entityFields[j];
                var compTypeID = pending.componentData[componentDataIndex].typeID;

                var pComponent = world.EntityManager.GetComponent(entity, compTypeID);
                if (pComponent == null)
                {
                    continue;
                }

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
    }

    /// <summary>
    /// Destroys all entities belonging to the specified scene.
    /// </summary>
    /// <param name="scene">The scene to destroy.</param>
    /// <param name="world">The world containing the entities.</param>
    public static void DestroyScene(Scene scene, World world)
    {
        var queryID = new QueryBuilder().WithAll<SceneID>().Build(world);
        ref var query = ref world.ComponentManager.GetEntityQueryReference(queryID);

        // Iterate through all matching entities
        foreach (var chunk in query.GetChunkIterator())
        {
            ref readonly var sceneID = ref chunk.GetSharedComponent<SceneID>();
            if (sceneID.value == scene.ID)
            {
                world.EntityManager.DestroyEntities(chunk.GetEntities());
            }
        }

        s_recycledSceneIDs.Enqueue(scene.ID);

        lock (s_loadedScenesLock)
        {
            var key = new SceneKey(world, scene);
            if (s_loadedScenes.Remove(key, out var pendingLoad))
            {
                pendingLoad.ReleaseMaterializedReference();
            }
        }
    }

    /// <summary>
    /// Gets all entities belonging to the specified scene.
    /// </summary>
    /// <param name="scene">The scene to query.</param>
    /// <param name="world">The world containing the entities.</param>
    /// <param name="entities">Span to store the entities.</param>
    /// <returns>The number of entities written to the span.</returns>
    public static UnsafeList<Entity> GetSceneEntities(World world, Scene scene, AllocationHandle handle)
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
