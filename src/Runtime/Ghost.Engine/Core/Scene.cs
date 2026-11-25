using Ghost.Core;
using Ghost.Engine.Components;
using Ghost.Engine.Streaming;
using Ghost.Engine.Utilities;
using Ghost.Engine.Volumes;
using Ghost.Entities;
using Ghost.Graphics.Core;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.Core;

/// <summary>
/// Represents a loaded runtime scene instance grouping entities via a shared <see cref="SceneID"/>.
/// </summary>
public struct Scene : IEquatable<Scene>
{
    public const ushort INVALID_ID = 65535;

    private ushort _id;

    public readonly ushort ID => _id;

    /// <summary>
    /// Gets whether this scene handle is valid.
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

    /// <summary>
    /// Creates a <see cref="Scene"/> handle from a raw ID.
    /// </summary>
    public static Scene FromID(ushort id) => new(id);

    public readonly bool Equals(Scene other) => ID == other.ID;

    public override readonly bool Equals(object? obj) => obj is Scene other && Equals(other);

    public override readonly int GetHashCode() => ID.GetHashCode();

    public override readonly string ToString() => $"Scene(ID: {ID})";

    public static bool operator ==(Scene left, Scene right) => left.Equals(right);

    public static bool operator !=(Scene left, Scene right) => !left.Equals(right);
}

public enum SceneLoadStatus
{
    Queued = 0,
    WaitingForDependencies = 1,
    WaitingForMaterialization = 2,
    Materializing = 3,
    Completed = 4,
    Failed = 5,
    Canceled = 6,
}

public struct SceneLoadOptions
{
    public bool DeferMaterialization;
    public bool AllowBackgroundStreaming;
    public int MaxEntitiesPerFrame;
    public int Priority;

    public readonly bool AutoMaterialize => !DeferMaterialization;
}

public readonly struct SceneMaterializeBudget
{
    public readonly int MaxEntities;
    public readonly int MaxScenes;
    public readonly double MaxMaterializeMsPerFrame;

    public SceneMaterializeBudget(int maxEntities, int maxScenes = 0, double maxMaterializeMsPerFrame = 0.0)
    {
        MaxEntities = maxEntities;
        MaxScenes = maxScenes;
        MaxMaterializeMsPerFrame = maxMaterializeMsPerFrame;
    }

    public static SceneMaterializeBudget Default => new(256, 4, 4.0);
    public static SceneMaterializeBudget Unlimited => new(int.MaxValue, int.MaxValue, 0.0);
}

/// <summary>
/// Tracks asynchronous loading, streaming dependency completion, and time-sliced materialization of a scene.
/// </summary>
public sealed class SceneLoadOperation
{
    private readonly PendingSceneLoad _pendingLoad;

    internal PendingSceneLoad PendingLoad => _pendingLoad;

    public SceneLoadStatus Status => _pendingLoad.Status;
    public float Progress => _pendingLoad.Progress;
    public Scene Scene => _pendingLoad.Scene;
    public string? ErrorMessage => _pendingLoad.ErrorMessage;
    public bool IsMaterialized => _pendingLoad.Status == SceneLoadStatus.Completed;
    public bool IsCompleted => _pendingLoad.Status is SceneLoadStatus.Completed or SceneLoadStatus.Failed or SceneLoadStatus.Canceled;

    internal SceneLoadOperation(PendingSceneLoad pendingLoad)
    {
        _pendingLoad = pendingLoad;
    }

    /// <summary>
    /// Allows a scene loaded with <see cref="SceneLoadOptions.DeferMaterialization"/> to begin instantiating entities.
    /// </summary>
    public void Activate()
    {
        _pendingLoad.Activate();
    }

    public void Cancel()
    {
        _pendingLoad.Cancel();
    }
}

internal sealed unsafe class PendingSceneLoad : ISceneReferenceResolver
{
    private enum MaterializePhase
    {
        NotStarted = 0,
        CreateAndPopulate = 1,
        WireReferencesAndHierarchy = 2,
        Completed = 3,
    }

    private readonly struct ResolvedFixups
    {
        public readonly int TemplateEntityIndex;
        public readonly Handle<Mesh> MeshHandle;
        public readonly Identifier<MaterialPalette> Palette;

        public ResolvedFixups(int templateEntityIndex, Handle<Mesh> meshHandle, Identifier<MaterialPalette> palette)
        {
            TemplateEntityIndex = templateEntityIndex;
            MeshHandle = meshHandle;
            Palette = palette;
        }
    }

    private readonly SceneAssetEntry _sceneEntry;
    private readonly SceneLoadOptions _options;

    private LoadedSceneData? _sceneData;
    private Entity[] _standaloneEntities = Array.Empty<Entity>();
    private PrefabAssetEntry[] _prefabEntries = Array.Empty<PrefabAssetEntry>();
    private Entity[][] _prefabSlots = Array.Empty<Entity[]>();
    private ResolvedFixups[][] _prefabFixups = Array.Empty<ResolvedFixups[]>();
    private int[] _prefabFlatOffsets = Array.Empty<int>();
    private bool[] _standaloneNeedsHierarchy = Array.Empty<bool>();
    private bool[][] _prefabNeedsHierarchy = Array.Empty<bool[]>();

    private int _totalFlatEntities;
    private MaterializePhase _phase;
    private int _createCursor;
    private int _wireCursor;
    private int _status;
    private int _activated;
    private int _releasedSceneEntry;
    private bool _singleResetApplied;
    private bool _prepared;

    public World World { get; }
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

            if (status is SceneLoadStatus.Failed or SceneLoadStatus.Canceled)
            {
                return 0.0f;
            }

            if (!_prepared)
            {
                var assetProgress = _sceneEntry.GetLoadProgress();
                var weighted = assetProgress.Progress * 0.75f;
                if (status == SceneLoadStatus.WaitingForMaterialization)
                {
                    return 0.80f;
                }

                if (_sceneEntry.State == AssetState.Ready && (_options.AllowBackgroundStreaming || assetProgress.ReadyCount == assetProgress.TotalCount))
                {
                    return 0.78f;
                }

                return weighted;
            }

            if (_totalFlatEntities <= 0)
            {
                return 0.99f;
            }

            var totalWork = _totalFlatEntities * 2;
            var done = Math.Min(totalWork, _createCursor + _wireCursor);
            return 0.80f + (0.20f * done / totalWork);
        }
    }

    public PendingSceneLoad(World world, SceneAssetEntry sceneEntry, SceneLoadingType loadingType, SceneLoadOptions options)
    {
        World = world;
        _sceneEntry = sceneEntry;
        LoadingType = loadingType;
        _options = options;
        Scene = Scene.Invalid;
        _status = (int)SceneLoadStatus.Queued;
        _activated = options.AutoMaterialize ? 1 : 0;
    }

    public void Activate()
    {
        Volatile.Write(ref _activated, 1);
    }

    public void Fail(string message)
    {
        ErrorMessage = message;
        Volatile.Write(ref _status, (int)SceneLoadStatus.Failed);
        CleanupOnAbort();
        ReleaseEntryReference();
    }

    public void Cancel()
    {
        if (IsTerminal)
        {
            return;
        }

        Volatile.Write(ref _status, (int)SceneLoadStatus.Canceled);
        CleanupOnAbort();
        ReleaseEntryReference();
    }

    internal void ReleaseEntryReference()
    {
        if (Interlocked.Exchange(ref _releasedSceneEntry, 1) == 0)
        {
            _sceneEntry.Release();
        }
    }

    public int Materialize(in SceneMaterializeBudget budget)
    {
        if (IsTerminal)
        {
            return 0;
        }

        if (_sceneEntry.HasFailedAssetInGraph())
        {
            Fail($"Scene asset '{_sceneEntry.AssetName}' or one of its dependencies failed to load.");
            return 0;
        }

        if (_sceneEntry.State != AssetState.Ready)
        {
            Volatile.Write(ref _status, (int)SceneLoadStatus.WaitingForDependencies);
            return 0;
        }

        if (!_options.AllowBackgroundStreaming)
        {
            var depProgress = _sceneEntry.GetLoadProgress();
            if (depProgress.ReadyCount < depProgress.TotalCount)
            {
                Volatile.Write(ref _status, (int)SceneLoadStatus.WaitingForDependencies);
                return 0;
            }
        }

        if (_options.DeferMaterialization && Volatile.Read(ref _activated) == 0)
        {
            Volatile.Write(ref _status, (int)SceneLoadStatus.WaitingForMaterialization);
            return 0;
        }

        if (!_singleResetApplied && LoadingType == SceneLoadingType.Single)
        {
            SceneManager.DestroyAllScenesInWorld(World);
            _singleResetApplied = true;
        }

        if (!_prepared)
        {
            var prepResult = PrepareMaterialization();
            if (prepResult.IsFailure)
            {
                Fail(prepResult.Message ?? "Failed to prepare scene materialization.");
                return 0;
            }
        }

        if (_totalFlatEntities == 0)
        {
            CompleteMaterialization();
            return 0;
        }

        var maxEntities = budget.MaxEntities > 0 ? budget.MaxEntities : int.MaxValue;
        if (_options.MaxEntitiesPerFrame > 0)
        {
            maxEntities = Math.Min(maxEntities, _options.MaxEntitiesPerFrame);
        }

        Volatile.Write(ref _status, (int)SceneLoadStatus.Materializing);

        var startTimestamp = Stopwatch.GetTimestamp();
        var maxMs = budget.MaxMaterializeMsPerFrame;
        var consumed = 0;

        while (_phase == MaterializePhase.CreateAndPopulate && _createCursor < _totalFlatEntities)
        {
            CreateAndPopulateFlatEntity(_createCursor);
            _createCursor++;
            consumed++;

            if (BudgetExceeded(consumed, maxEntities, startTimestamp, maxMs))
            {
                return consumed;
            }
        }

        if (_phase == MaterializePhase.CreateAndPopulate && _createCursor >= _totalFlatEntities)
        {
            _phase = MaterializePhase.WireReferencesAndHierarchy;
        }

        while (_phase == MaterializePhase.WireReferencesAndHierarchy && _wireCursor < _totalFlatEntities)
        {
            WireFlatEntity(_wireCursor);
            _wireCursor++;
            consumed++;

            if (BudgetExceeded(consumed, maxEntities, startTimestamp, maxMs))
            {
                return consumed;
            }
        }

        if (_phase == MaterializePhase.WireReferencesAndHierarchy && _wireCursor >= _totalFlatEntities)
        {
            CompleteMaterialization();
        }

        return consumed;
    }

    private static bool BudgetExceeded(int consumed, int maxEntities, long startTimestamp, double maxMs)
    {
        if (consumed >= maxEntities)
        {
            return true;
        }

        return maxMs > 0.0 && Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds >= maxMs;
    }

    private Result PrepareMaterialization()
    {
        var data = _sceneEntry.SceneData;
        if (data == null)
        {
            return Result.Failure($"Scene '{_sceneEntry.AssetName}' has no parsed payload.");
        }

        _sceneData = data;
        var standaloneCount = data.Entities.Length;
        var prefabCount = data.PrefabInstances.Length;

        _standaloneEntities = standaloneCount > 0 ? new Entity[standaloneCount] : Array.Empty<Entity>();
        _standaloneNeedsHierarchy = standaloneCount > 0 ? new bool[standaloneCount] : Array.Empty<bool>();
        _prefabEntries = prefabCount > 0 ? new PrefabAssetEntry[prefabCount] : Array.Empty<PrefabAssetEntry>();
        _prefabSlots = prefabCount > 0 ? new Entity[prefabCount][] : Array.Empty<Entity[]>();
        _prefabFixups = prefabCount > 0 ? new ResolvedFixups[prefabCount][] : Array.Empty<ResolvedFixups[]>();
        _prefabNeedsHierarchy = prefabCount > 0 ? new bool[prefabCount][] : Array.Empty<bool[]>();
        _prefabFlatOffsets = prefabCount > 0 ? new int[prefabCount] : Array.Empty<int>();

        var flatOffset = standaloneCount;
        var sceneDeps = _sceneEntry.Dependencies;
        for (var p = 0; p < prefabCount; p++)
        {
            ref readonly var spawn = ref data.PrefabInstances[p];
            if ((uint)spawn.prefabDependencyIndex >= (uint)sceneDeps.Length)
            {
                return Result.Failure($"Prefab dependency index {spawn.prefabDependencyIndex} is out of bounds.");
            }

            var prefabGuid = sceneDeps[spawn.prefabDependencyIndex];
            if (!_sceneEntry.Manager.TryGetEntry(prefabGuid, out var rawEntry)
                || rawEntry is not PrefabAssetEntry prefabEntry
                || prefabEntry.State != AssetState.Ready
                || prefabEntry.PrefabData is not { } prefabData)
            {
                return Result.Failure($"Referenced prefab '{prefabGuid}' is not ready during scene materialization.");
            }

            var nodeCount = prefabData.Entities.Length;
            _prefabEntries[p] = prefabEntry;
            _prefabSlots[p] = new Entity[nodeCount];
            _prefabNeedsHierarchy[p] = new bool[nodeCount];
            _prefabFixups[p] = ResolvePrefabFixups(prefabEntry, prefabData);
            _prefabFlatOffsets[p] = flatOffset;
            flatOffset += nodeCount;

            if (nodeCount > 1 || spawn.parentTarget.IsValid)
            {
                Array.Fill(_prefabNeedsHierarchy[p], true);
            }
        }

        for (var s = 0; s < standaloneCount; s++)
        {
            var target = data.Entities[s].parentTarget;
            if (!target.IsValid)
            {
                continue;
            }

            _standaloneNeedsHierarchy[s] = true;
            MarkTargetNeedsHierarchy(target);
        }

        for (var p = 0; p < prefabCount; p++)
        {
            var target = data.PrefabInstances[p].parentTarget;
            if (target.IsValid)
            {
                MarkTargetNeedsHierarchy(target);
            }
        }

        _totalFlatEntities = flatOffset;
        Scene = SceneManager.CreateScene();
        _phase = MaterializePhase.CreateAndPopulate;
        _prepared = true;
        return Result.Success();
    }

    private void MarkTargetNeedsHierarchy(SceneEntityTarget target)
    {
        if (target.kind == SceneTargetKind.Standalone && (uint)target.index < (uint)_standaloneNeedsHierarchy.Length)
        {
            _standaloneNeedsHierarchy[target.index] = true;
        }
        else if (target.kind == SceneTargetKind.PrefabNode && (uint)target.index < (uint)_prefabNeedsHierarchy.Length)
        {
            var nodes = _prefabNeedsHierarchy[target.index];
            if ((uint)target.subIndex < (uint)nodes.Length)
            {
                nodes[target.subIndex] = true;
            }
        }
    }

    private void CreateAndPopulateFlatEntity(int flatIndex)
    {
        var data = _sceneData!;
        if (flatIndex < data.Entities.Length)
        {
            CreateAndPopulateStandaloneEntity(data, flatIndex);
            return;
        }

        FindPrefabNodeIndex(flatIndex, out var prefabIdx, out var templateIdx);
        CreateAndPopulatePrefabNode(data, prefabIdx, templateIdx);
    }

    private void WireFlatEntity(int flatIndex)
    {
        var data = _sceneData!;
        if (flatIndex < data.Entities.Length)
        {
            WireStandaloneEntity(data, flatIndex);
            return;
        }

        FindPrefabNodeIndex(flatIndex, out var prefabIdx, out var templateIdx);
        WirePrefabNode(data, prefabIdx, templateIdx);
    }

    private void FindPrefabNodeIndex(int flatIndex, out int prefabIdx, out int templateIdx)
    {
        for (var p = _prefabFlatOffsets.Length - 1; p >= 0; p--)
        {
            var start = _prefabFlatOffsets[p];
            if (flatIndex >= start)
            {
                prefabIdx = p;
                templateIdx = flatIndex - start;
                return;
            }
        }

        prefabIdx = 0;
        templateIdx = 0;
    }

    private void CreateAndPopulateStandaloneEntity(LoadedSceneData data, int index)
    {
        var em = World.EntityManager;
        ref readonly var eHeader = ref data.Entities[index];
        var hierId = ComponentTypeID<Hierarchy>.Value;
        var sceneIdComp = ComponentTypeID<SceneID>.Value;
        var rvdcId = ComponentTypeID<RenderVolumeDataContainer>.Value;

        Span<Identifier<IComponent>> compSpan = stackalloc Identifier<IComponent>[eHeader.componentCount + 2];
        var compCount = 0;
        AddUniqueComponent(compSpan, ref compCount, sceneIdComp);

        for (var c = 0; c < eHeader.componentCount; c++)
        {
            var compRec = data.Components[eHeader.componentStartIndex + c];
            var compId = ResolveStringComponentId(data, compRec.typeNameIndex);
            AddUniqueComponent(compSpan, ref compCount, compId);
        }

        if (_standaloneNeedsHierarchy[index])
        {
            AddUniqueComponent(compSpan, ref compCount, hierId);
        }

        var entity = CreateEntityWithSceneID(em, compSpan.Slice(0, compCount), Scene.ID);
        _standaloneEntities[index] = entity;

        for (var c = 0; c < eHeader.componentCount; c++)
        {
            ref readonly var compRec = ref data.Components[eHeader.componentStartIndex + c];
            var compId = ResolveStringComponentId(data, compRec.typeNameIndex);
            if (compId.IsInvalid)
            {
                continue;
            }

            if (compId == rvdcId)
            {
                PopulateVolumeOverrides(em, entity, data, compRec.volumeOverrideStartIndex, compRec.volumeOverrideCount, includeEntityRefs: false);
                continue;
            }

            var pComp = em.GetComponent(entity, compId);
            if (pComp == null)
            {
                continue;
            }

            ApplyComponentFields(data, compId, pComp, compRec.fieldStartIndex, compRec.fieldCount, includeEntityRefs: false);
        }
    }

    private void CreateAndPopulatePrefabNode(LoadedSceneData data, int prefabIndex, int templateIndex)
    {
        var em = World.EntityManager;
        ref readonly var spawn = ref data.PrefabInstances[prefabIndex];
        var prefabEntry = _prefabEntries[prefabIndex];
        var prefabData = prefabEntry.PrefabData!;
        ref readonly var baseEntity = ref prefabData.Entities[templateIndex];

        var hierId = ComponentTypeID<Hierarchy>.Value;
        var sceneIdComp = ComponentTypeID<SceneID>.Value;
        var meshInstId = ComponentTypeID<MeshInstance>.Value;
        var rvdcId = ComponentTypeID<RenderVolumeDataContainer>.Value;

        var maxComps = baseEntity.ComponentIDs.Length + spawn.addedComponentCount + 2;
        Span<Identifier<IComponent>> compSpan = maxComps <= 64 ? stackalloc Identifier<IComponent>[maxComps] : new Identifier<IComponent>[maxComps];
        var compCount = 0;
        AddUniqueComponent(compSpan, ref compCount, sceneIdComp);

        for (var c = 0; c < baseEntity.ComponentIDs.Length; c++)
        {
            var baseCompId = baseEntity.ComponentIDs[c];
            if (baseCompId.IsInvalid || IsComponentRemovedOnNode(data, in spawn, templateIndex, baseCompId))
            {
                continue;
            }

            AddUniqueComponent(compSpan, ref compCount, baseCompId);
        }

        for (var a = 0; a < spawn.addedComponentCount; a++)
        {
            ref readonly var added = ref data.AddedComponents[spawn.addedComponentStartIndex + a];
            if (added.templateEntityIndex != templateIndex)
            {
                continue;
            }

            var addedCompId = ResolveStringComponentId(data, added.component.typeNameIndex);
            AddUniqueComponent(compSpan, ref compCount, addedCompId);
        }

        if (_prefabNeedsHierarchy[prefabIndex][templateIndex])
        {
            AddUniqueComponent(compSpan, ref compCount, hierId);
        }

        var entity = CreateEntityWithSceneID(em, compSpan.Slice(0, compCount), Scene.ID);
        _prefabSlots[prefabIndex][templateIndex] = entity;

        for (var c = 0; c < baseEntity.ComponentIDs.Length; c++)
        {
            var baseCompId = baseEntity.ComponentIDs[c];
            if (baseCompId.IsInvalid || IsComponentRemovedOnNode(data, in spawn, templateIndex, baseCompId))
            {
                continue;
            }

            fixed (byte* pData = baseEntity.ComponentData[c])
            {
                em.SetComponent(entity, baseCompId, pData);
            }
        }

        if (!IsComponentRemovedOnNode(data, in spawn, templateIndex, meshInstId) && em.HasComponent<MeshInstance>(entity))
        {
            var fixups = _prefabFixups[prefabIndex];
            for (var f = 0; f < fixups.Length; f++)
            {
                if (fixups[f].TemplateEntityIndex != templateIndex)
                {
                    continue;
                }

                ref var meshInst = ref em.GetComponent<MeshInstance>(entity);
                meshInst.mesh = fixups[f].MeshHandle;
                meshInst.materialPalette = fixups[f].Palette;
                break;
            }
        }

        for (var a = 0; a < spawn.addedComponentCount; a++)
        {
            ref readonly var added = ref data.AddedComponents[spawn.addedComponentStartIndex + a];
            if (added.templateEntityIndex != templateIndex)
            {
                continue;
            }

            var compId = ResolveStringComponentId(data, added.component.typeNameIndex);
            if (compId.IsInvalid)
            {
                continue;
            }

            if (compId == rvdcId)
            {
                PopulateVolumeOverrides(em, entity, data, added.component.volumeOverrideStartIndex, added.component.volumeOverrideCount, includeEntityRefs: false);
                continue;
            }

            var pComp = em.GetComponent(entity, compId);
            if (pComp != null)
            {
                ApplyComponentFields(data, compId, pComp, added.component.fieldStartIndex, added.component.fieldCount, includeEntityRefs: false);
            }
        }

        for (var o = 0; o < spawn.propertyOverrideCount; o++)
        {
            ref readonly var ovr = ref data.PropertyOverrides[spawn.propertyOverrideStartIndex + o];
            if (ovr.templateEntityIndex != templateIndex || ovr.field.kind == SceneFieldValueKind.EntityRef)
            {
                continue;
            }

            var compId = ResolveStringComponentId(data, ovr.componentTypeNameIndex);
            if (compId.IsInvalid)
            {
                continue;
            }

            var pComp = em.GetComponent(entity, compId);
            if (pComp != null)
            {
                var reader = new SceneFieldReader(in ovr.field, data.DataBlob, data.MaterialDependencyIndices, this);
                ComponentRegistry.TryApplySerializedField(compId, pComp, ovr.field.fieldNameHash, in reader);
            }
        }

        ApplyPrefabVolumeOverrides(em, entity, data, in spawn, templateIndex, includeEntityRefs: true);

        ApplyPrefabVolumeOverrides(em, entity, data, in spawn, templateIndex, includeEntityRefs: false);
    }

    private void WireStandaloneEntity(LoadedSceneData data, int index)
    {
        var em = World.EntityManager;
        var entity = _standaloneEntities[index];
        if (!entity.IsValid)
        {
            return;
        }

        ref readonly var eHeader = ref data.Entities[index];
        var rvdcId = ComponentTypeID<RenderVolumeDataContainer>.Value;

        for (var c = 0; c < eHeader.componentCount; c++)
        {
            ref readonly var compRec = ref data.Components[eHeader.componentStartIndex + c];
            var compId = ResolveStringComponentId(data, compRec.typeNameIndex);
            if (compId.IsInvalid || compId == rvdcId)
            {
                continue;
            }

            var pComp = em.GetComponent(entity, compId);
            if (pComp != null)
            {
                ApplyComponentFields(data, compId, pComp, compRec.fieldStartIndex, compRec.fieldCount, includeEntityRefs: true);
            }
        }

        if (eHeader.parentTarget.IsValid)
        {
            var parent = ResolveTargetEntity(eHeader.parentTarget);
            if (parent.IsValid)
            {
                AttachChildToParent(em, parent, entity);
            }
        }
        else if (!em.HasComponent<Hierarchy>(entity))
        {
            TransformUtility.UpdateEntityAndDescendants(em, entity);
        }
    }

    private void WirePrefabNode(LoadedSceneData data, int prefabIndex, int templateIndex)
    {
        var em = World.EntityManager;
        var entity = _prefabSlots[prefabIndex][templateIndex];
        if (!entity.IsValid)
        {
            return;
        }

        ref readonly var spawn = ref data.PrefabInstances[prefabIndex];
        var rvdcId = ComponentTypeID<RenderVolumeDataContainer>.Value;

        for (var a = 0; a < spawn.addedComponentCount; a++)
        {
            ref readonly var added = ref data.AddedComponents[spawn.addedComponentStartIndex + a];
            if (added.templateEntityIndex != templateIndex)
            {
                continue;
            }

            var compId = ResolveStringComponentId(data, added.component.typeNameIndex);
            if (compId.IsInvalid || compId == rvdcId)
            {
                continue;
            }

            var pComp = em.GetComponent(entity, compId);
            if (pComp != null)
            {
                ApplyComponentFields(data, compId, pComp, added.component.fieldStartIndex, added.component.fieldCount, includeEntityRefs: true);
            }
        }

        for (var o = 0; o < spawn.propertyOverrideCount; o++)
        {
            ref readonly var ovr = ref data.PropertyOverrides[spawn.propertyOverrideStartIndex + o];
            if (ovr.templateEntityIndex != templateIndex || ovr.field.kind != SceneFieldValueKind.EntityRef)
            {
                continue;
            }

            var compId = ResolveStringComponentId(data, ovr.componentTypeNameIndex);
            if (compId.IsInvalid)
            {
                continue;
            }

            var pComp = em.GetComponent(entity, compId);
            if (pComp != null)
            {
                var reader = new SceneFieldReader(in ovr.field, data.DataBlob, data.MaterialDependencyIndices, this);
                ComponentRegistry.TryApplySerializedField(compId, pComp, ovr.field.fieldNameHash, in reader);
            }
        }

        ApplyPrefabVolumeOverrides(em, entity, data, in spawn, templateIndex, includeEntityRefs: true);

        var prefabData = _prefabEntries[prefabIndex].PrefabData!;
        ref readonly var baseHeader = ref prefabData.Entities[templateIndex].Header;
        if (templateIndex == 0)
        {
            if (spawn.parentTarget.IsValid)
            {
                var parent = ResolveTargetEntity(spawn.parentTarget);
                if (parent.IsValid)
                {
                    AttachChildToParent(em, parent, entity);
                }
            }
        }
        else if (baseHeader.parentIndex >= 0 && baseHeader.parentIndex < _prefabSlots[prefabIndex].Length)
        {
            var parent = _prefabSlots[prefabIndex][baseHeader.parentIndex];
            if (parent.IsValid)
            {
                AttachChildToParent(em, parent, entity);
            }
        }
        else if (spawn.parentTarget.IsValid)
        {
            var parent = ResolveTargetEntity(spawn.parentTarget);
            if (parent.IsValid)
            {
                AttachChildToParent(em, parent, entity);
            }
        }
    }

    private void CompleteMaterialization()
    {
        UpdateRootTransforms();
        _phase = MaterializePhase.Completed;
        Volatile.Write(ref _status, (int)SceneLoadStatus.Completed);
        SceneManager.RegisterMaterializedScene(this);
    }

    private void UpdateRootTransforms()
    {
        var em = World.EntityManager;
        for (var i = 0; i < _standaloneEntities.Length; i++)
        {
            var entity = _standaloneEntities[i];
            if (entity.IsValid && (!em.HasComponent<Hierarchy>(entity) || !em.GetComponent<Hierarchy>(entity).parent.IsValid))
            {
                TransformUtility.UpdateEntityAndDescendants(em, entity);
            }
        }

        for (var p = 0; p < _prefabSlots.Length; p++)
        {
            var nodes = _prefabSlots[p];
            if (nodes.Length > 0 && nodes[0].IsValid && (!em.HasComponent<Hierarchy>(nodes[0]) || !em.GetComponent<Hierarchy>(nodes[0]).parent.IsValid))
            {
                TransformUtility.UpdateEntityAndDescendants(em, nodes[0]);
            }
        }
    }

    private void CleanupOnAbort()
    {
        if (Scene.IsValid)
        {
            SceneManager.DestroyScene(Scene, World);
            Scene = Scene.Invalid;
        }
    }

    private void ApplyComponentFields(LoadedSceneData data, Identifier<IComponent> compId, void* pComp, int fieldStart, int fieldCount, bool includeEntityRefs)
    {
        for (var f = 0; f < fieldCount; f++)
        {
            ref readonly var fieldRec = ref data.Fields[fieldStart + f];
            var isEntityRef = fieldRec.kind == SceneFieldValueKind.EntityRef;
            if (isEntityRef != includeEntityRefs)
            {
                continue;
            }

            var reader = new SceneFieldReader(in fieldRec, data.DataBlob, data.MaterialDependencyIndices, this);
            ComponentRegistry.TryApplySerializedField(compId, pComp, fieldRec.fieldNameHash, in reader);
        }
    }

    private void ApplyPrefabVolumeOverrides(EntityManager em, Entity entity, LoadedSceneData data, in ScenePrefabSpawnHeader spawn, int templateIndex, bool includeEntityRefs)
    {
        if (spawn.volumeOverrideCount <= 0)
        {
            return;
        }

        var rvdcId = ComponentTypeID<RenderVolumeDataContainer>.Value;
        for (var v = 0; v < spawn.volumeOverrideCount; v++)
        {
            ref readonly var volOvr = ref data.PrefabVolumeOverrides[spawn.volumeOverrideStartIndex + v];
            if (volOvr.templateEntityIndex != templateIndex || (uint)volOvr.volumeTypeNameIndex >= (uint)data.StringVolumeTypeIDs.Length)
            {
                continue;
            }

            var volTypeId = data.StringVolumeTypeIDs[volOvr.volumeTypeNameIndex];
            if (volTypeId == ushort.MaxValue)
            {
                continue;
            }

            if (volOvr.removed != 0)
            {
                if (!includeEntityRefs && em.HasComponent<RenderVolumeDataContainer>(entity))
                {
                    var pContainer = (RenderVolumeDataContainer*)em.GetComponent(entity, rvdcId);
                    if (pContainer != null && pContainer->IsCreated)
                    {
                        pContainer->Remove(volTypeId);
                    }
                }

                continue;
            }

            if (!em.HasComponent<RenderVolumeDataContainer>(entity))
            {
                if (includeEntityRefs)
                {
                    continue;
                }

                var created = RenderVolumeDataContainer.Create();
                em.AddComponent(entity, in created);
            }

            var ptr = (RenderVolumeDataContainer*)em.GetComponent(entity, rvdcId);
            if (ptr == null)
            {
                continue;
            }

            if (!ptr->IsCreated)
            {
                *ptr = RenderVolumeDataContainer.Create();
            }

            if (!ptr->TryGetOrAddDefault(volTypeId, out var volPtr))
            {
                continue;
            }

            for (var f = 0; f < volOvr.fieldCount; f++)
            {
                ref readonly var fieldRec = ref data.Fields[volOvr.fieldStartIndex + f];
                if ((fieldRec.kind == SceneFieldValueKind.EntityRef) != includeEntityRefs)
                {
                    continue;
                }

                var reader = new SceneFieldReader(in fieldRec, data.DataBlob, data.MaterialDependencyIndices, this);
                VolumeRegistry.TryApplySerializedField(volTypeId, volPtr, fieldRec.fieldNameHash, in reader);
            }
        }
    }

    private void PopulateVolumeOverrides(EntityManager em, Entity entity, LoadedSceneData data, int volStart, int volCount, bool includeEntityRefs)
    {
        var rvdcId = ComponentTypeID<RenderVolumeDataContainer>.Value;
        var ptr = (RenderVolumeDataContainer*)em.GetComponent(entity, rvdcId);
        if (ptr == null)
        {
            return;
        }

        if (!ptr->IsCreated)
        {
            *ptr = RenderVolumeDataContainer.Create();
        }

        for (var v = 0; v < volCount; v++)
        {
            ref readonly var volRec = ref data.VolumeOverrides[volStart + v];
            if ((uint)volRec.typeNameIndex >= (uint)data.StringVolumeTypeIDs.Length)
            {
                continue;
            }

            var volTypeId = data.StringVolumeTypeIDs[volRec.typeNameIndex];
            if (volTypeId == ushort.MaxValue || !ptr->TryGetOrAddDefault(volTypeId, out var volPtr))
            {
                continue;
            }

            for (var f = 0; f < volRec.fieldCount; f++)
            {
                ref readonly var fieldRec = ref data.Fields[volRec.fieldStartIndex + f];
                if ((fieldRec.kind == SceneFieldValueKind.EntityRef) != includeEntityRefs)
                {
                    continue;
                }

                var reader = new SceneFieldReader(in fieldRec, data.DataBlob, data.MaterialDependencyIndices, this);
                VolumeRegistry.TryApplySerializedField(volTypeId, volPtr, fieldRec.fieldNameHash, in reader);
            }
        }
    }

    private static Identifier<IComponent> ResolveStringComponentId(LoadedSceneData data, int stringIndex)
    {
        return (uint)stringIndex < (uint)data.StringComponentIDs.Length ? data.StringComponentIDs[stringIndex] : Identifier<IComponent>.Invalid;
    }

    private static bool IsComponentRemovedOnNode(LoadedSceneData data, in ScenePrefabSpawnHeader spawn, int templateIndex, Identifier<IComponent> compId)
    {
        for (var r = 0; r < spawn.removedComponentCount; r++)
        {
            ref readonly var rem = ref data.RemovedComponents[spawn.removedComponentStartIndex + r];
            if (rem.templateEntityIndex == templateIndex && ResolveStringComponentId(data, rem.typeNameIndex) == compId)
            {
                return true;
            }
        }

        return false;
    }

    private static ResolvedFixups[] ResolvePrefabFixups(PrefabAssetEntry prefabEntry, LoadedPrefabData prefabData)
    {
        var fixups = prefabData.MeshMaterialFixups;
        if (fixups.Length == 0)
        {
            return Array.Empty<ResolvedFixups>();
        }

        var result = new ResolvedFixups[fixups.Length];
        var rm = prefabEntry.ResourceManager;
        var deps = prefabEntry.Dependencies;
        var maxMatCount = 0;
        for (var f = 0; f < fixups.Length; f++)
        {
            if (fixups[f].materialCount > maxMatCount)
            {
                maxMatCount = fixups[f].materialCount;
            }
        }

        Span<Handle<Material>> matBuffer = maxMatCount <= 32 ? stackalloc Handle<Material>[maxMatCount] : new Handle<Material>[maxMatCount];

        for (var f = 0; f < fixups.Length; f++)
        {
            ref readonly var fixup = ref fixups[f];
            var meshHandle = Handle<Mesh>.Invalid;
            if ((uint)fixup.meshDependencyIndex < (uint)deps.Length)
            {
                var meshGuid = deps[fixup.meshDependencyIndex];
                if (prefabEntry.Manager.TryGetEntry(meshGuid, out var meshAsset))
                {
                    meshAsset.ReadAssetData(ref meshHandle);
                }
            }

            var matPalette = new Identifier<MaterialPalette>(-1);
            if (fixup.materialCount > 0 && rm != null)
            {
                var matHandles = matBuffer.Slice(0, fixup.materialCount);
                for (var m = 0; m < fixup.materialCount; m++)
                {
                    var indexSlot = fixup.materialDependencyStartIndex + m;
                    var depIdx = prefabData.MaterialDependencyIndices.Length > 0
                        ? ((uint)indexSlot < (uint)prefabData.MaterialDependencyIndices.Length ? prefabData.MaterialDependencyIndices[indexSlot] : -1)
                        : indexSlot;
                    var matHandle = Handle<Material>.Invalid;
                    if ((uint)depIdx < (uint)deps.Length && prefabEntry.Manager.TryGetEntry(deps[depIdx], out var matAsset))
                    {
                        matAsset.ReadAssetData(ref matHandle);
                    }

                    matHandles[m] = matHandle;
                }

                matPalette = new Identifier<MaterialPalette>(rm.GetOrCreateMaterialPalette(matHandles));
            }

            result[f] = new ResolvedFixups(fixup.entityIndex, meshHandle, matPalette);
        }

        return result;
    }

    private static void AddUniqueComponent(Span<Identifier<IComponent>> buffer, ref int count, Identifier<IComponent> componentId)
    {
        if (componentId.IsInvalid)
        {
            return;
        }

        for (var i = 0; i < count; i++)
        {
            if (buffer[i] == componentId)
            {
                return;
            }
        }

        buffer[count++] = componentId;
    }

    private static Entity CreateEntityWithSceneID(EntityManager em, ReadOnlySpan<Identifier<IComponent>> componentIds, ushort sceneId)
    {
        using var scope = AllocationManager.CreateStackScope();
        using var sharedSet = new SharedComponentSet(sizeof(SceneID), scope.AllocationHandle);
        sharedSet.With(new SceneID { value = sceneId });

        var view = new ComponentSetView(componentIds, ref Unsafe.AsRef(in sharedSet));
        return em.CreateEntity(view);
    }

    private static void AttachChildToParent(EntityManager em, Entity parent, Entity child)
    {
        if (!em.HasComponent<Hierarchy>(child))
        {
            em.AddComponent(child, new Hierarchy { parent = parent, firstChild = Entity.Invalid, nextSibling = Entity.Invalid });
        }
        else
        {
            em.GetComponent<Hierarchy>(child).parent = parent;
        }

        if (!em.HasComponent<Hierarchy>(parent))
        {
            em.AddComponent(parent, new Hierarchy { parent = Entity.Invalid, firstChild = child, nextSibling = Entity.Invalid });
            return;
        }

        ref var parentH = ref em.GetComponent<Hierarchy>(parent);
        if (!parentH.firstChild.IsValid)
        {
            parentH.firstChild = child;
            return;
        }

        var cur = parentH.firstChild;
        while (cur.IsValid && cur != child)
        {
            ref var curH = ref em.GetComponent<Hierarchy>(cur);
            if (!curH.nextSibling.IsValid)
            {
                curH.nextSibling = child;
                return;
            }

            cur = curH.nextSibling;
        }
    }

    private Entity ResolveTargetEntity(SceneEntityTarget target)
    {
        if (target.kind == SceneTargetKind.Standalone && (uint)target.index < (uint)_standaloneEntities.Length)
        {
            return _standaloneEntities[target.index];
        }

        if (target.kind == SceneTargetKind.PrefabNode && (uint)target.index < (uint)_prefabSlots.Length)
        {
            var nodes = _prefabSlots[target.index];
            if ((uint)target.subIndex < (uint)nodes.Length)
            {
                return nodes[target.subIndex];
            }
        }

        return Entity.Invalid;
    }

    public Handle<T> ResolveAssetHandle<T>(int assetDependencyIndex)
    {
        var deps = _sceneEntry.Dependencies;
        if ((uint)assetDependencyIndex < (uint)deps.Length)
        {
            var guid = deps[assetDependencyIndex];
            if (_sceneEntry.Manager.TryGetEntry(guid, out var depEntry))
            {
                var handle = Handle<T>.Invalid;
                depEntry.ReadAssetData(ref handle);
                return handle;
            }
        }

        return Handle<T>.Invalid;
    }

    public int ResolveMaterialPalette(ReadOnlySpan<int> materialDependencyIndices)
    {
        var rm = _sceneEntry.ResourceManager;
        if (rm == null || materialDependencyIndices.Length == 0)
        {
            return -1;
        }

        var deps = _sceneEntry.Dependencies;
        Span<Handle<Material>> matHandles = materialDependencyIndices.Length <= 32
            ? stackalloc Handle<Material>[materialDependencyIndices.Length]
            : new Handle<Material>[materialDependencyIndices.Length];

        for (var i = 0; i < materialDependencyIndices.Length; i++)
        {
            var depIdx = materialDependencyIndices[i];
            var handle = Handle<Material>.Invalid;
            if ((uint)depIdx < (uint)deps.Length && _sceneEntry.Manager.TryGetEntry(deps[depIdx], out var matEntry))
            {
                matEntry.ReadAssetData(ref handle);
            }

            matHandles[i] = handle;
        }

        return rm.GetOrCreateMaterialPalette(matHandles);
    }

    public void ResolveEntityTarget(SceneEntityTarget target, out int entityId, out int entityGeneration)
    {
        var resolved = ResolveTargetEntity(target);
        if (resolved.IsValid)
        {
            entityId = resolved.ID;
            entityGeneration = resolved.Generation;
            return;
        }

        entityId = -1;
        entityGeneration = 0;
    }
}

/// <summary>
/// Coordinates asynchronous scene streaming, prefab flattening, and scene unloading across worlds.
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

        public bool Equals(SceneKey other) => _worldID == other._worldID && _sceneID == other._sceneID;
        public override bool Equals(object? obj) => obj is SceneKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(_worldID, _sceneID);
    }

    private static ushort s_nextSceneID;
    private static readonly Queue<ushort> s_recycledSceneIDs = new();

    private static readonly Lock s_creationLock = new();
    private static readonly Lock s_loadedScenesLock = new();
    private static readonly ConcurrentQueue<PendingSceneLoad> s_pendingMaterialization = new();
    private static readonly Dictionary<SceneKey, PendingSceneLoad> s_loadedScenes = new();

    /// <summary>
    /// Allocates a new scene handle ID.
    /// </summary>
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

    /// <summary>
    /// Enqueues an already-resolved <see cref="IAssetEntry"/> (of type <see cref="AssetType.Scene"/>) to load into <paramref name="world"/>.
    /// </summary>
    public static Result<SceneLoadOperation> LoadSceneAsync(World world, IAssetEntry sceneAsset, SceneLoadingType loadingType = SceneLoadingType.Single, SceneLoadOptions options = default)
    {
        if (world == null)
        {
            return Result.Failure<SceneLoadOperation>("Target world cannot be null.");
        }

        if (sceneAsset is not SceneAssetEntry sceneEntry)
        {
            return Result.Failure<SceneLoadOperation>($"Expected Scene asset entry, got '{sceneAsset?.AssetType}'.");
        }

        sceneEntry.AddRef();
        var pending = new PendingSceneLoad(world, sceneEntry, loadingType, options);
        s_pendingMaterialization.Enqueue(pending);
        return Result.Success(new SceneLoadOperation(pending));
    }

    /// <summary>
    /// Resolves a scene by asset <see cref="Guid"/> via <paramref name="assetManager"/> and loads it into <paramref name="world"/>.
    /// </summary>
    public static Result<SceneLoadOperation> LoadSceneAsync(
        World world,
        AssetManager assetManager,
        Guid sceneGuid,
        SceneLoadingType loadingType = SceneLoadingType.Single,
        SceneLoadOptions options = default)
    {
        if (assetManager == null)
        {
            return Result.Failure<SceneLoadOperation>("AssetManager cannot be null.");
        }

        if (!assetManager.TryResolveAsset(sceneGuid, out var sceneAsset))
        {
            return Result.Failure<SceneLoadOperation>($"Scene asset '{sceneGuid}' was not found.");
        }

        var result = LoadSceneAsync(world, sceneAsset, loadingType, options);
        sceneAsset.Release();
        return result;
    }

    /// <summary>
    /// Resolves a scene by virtual path via <paramref name="assetManager"/> and loads it into <paramref name="world"/>.
    /// </summary>
    public static Result<SceneLoadOperation> LoadSceneAsync(
        World world,
        AssetManager assetManager,
        string virtualPath,
        SceneLoadingType loadingType = SceneLoadingType.Single,
        SceneLoadOptions options = default)
    {
        if (assetManager == null)
        {
            return Result.Failure<SceneLoadOperation>("AssetManager cannot be null.");
        }

        if (!assetManager.TryResolveAsset(virtualPath, out var sceneAsset))
        {
            return Result.Failure<SceneLoadOperation>($"Scene asset '{virtualPath}' was not found.");
        }

        var result = LoadSceneAsync(world, sceneAsset, loadingType, options);
        sceneAsset.Release();
        return result;
    }

    /// <summary>
    /// Resolves a scene by virtual path using the <see cref="AssetManager"/> service registered on <paramref name="world"/>.
    /// </summary>
    public static Result<SceneLoadOperation> LoadSceneAsync(World world, string virtualPath, SceneLoadingType loadingType = SceneLoadingType.Single, SceneLoadOptions options = default)
    {
        if (world == null)
        {
            return Result.Failure<SceneLoadOperation>("Target world cannot be null.");
        }

        if (!world.TryGetService<AssetManager>(out var assetManager) || assetManager == null)
        {
            return Result.Failure<SceneLoadOperation>("World does not have a registered AssetManager service.");
        }

        return LoadSceneAsync(world, assetManager, virtualPath, loadingType, options);
    }

    /// <summary>
    /// Advances queued scene loads for <paramref name="world"/> within the specified per-frame entity and time budget.
    /// </summary>
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

            var perCallBudget = new SceneMaterializeBudget(remainingEntities, 1, budget.MaxMaterializeMsPerFrame);
            var consumed = pendingLoad.Materialize(in perCallBudget);
            remainingEntities -= consumed;

            if (!pendingLoad.IsTerminal)
            {
                s_pendingMaterialization.Enqueue(pendingLoad);
            }

            processedScenes++;
        }
    }

    /// <summary>
    /// Blocks until <paramref name="operation"/> finishes by ticking <paramref name="engine"/> (recommended for startup boot scenes).
    /// </summary>
    public static Result WaitForCompletion(EngineCore engine, SceneLoadOperation operation, SceneMaterializeBudget budget = default)
    {
        if (engine == null || operation == null)
        {
            return Result.Failure("EngineCore and SceneLoadOperation cannot be null.");
        }

        var effectiveBudget = budget.MaxEntities > 0 ? budget : SceneMaterializeBudget.Unlimited;
        while (!operation.IsCompleted)
        {
            engine.Tick();
            MaterializePendingScenes(operation.PendingLoad.World, effectiveBudget);
            if (!operation.IsCompleted)
            {
                Thread.Yield();
            }
        }

        return operation.Status == SceneLoadStatus.Completed
            ? Result.Success()
            : Result.Failure(operation.ErrorMessage ?? $"Scene load ended with status {operation.Status}.");
    }

    internal static Result WaitForCompletion(World world, SceneLoadOperation operation, SceneMaterializeBudget budget = default)
    {
        var effectiveBudget = budget.MaxEntities > 0 ? budget : SceneMaterializeBudget.Unlimited;
        while (!operation.IsCompleted)
        {
            MaterializePendingScenes(world, effectiveBudget);
            if (!operation.IsCompleted)
            {
                Thread.Yield();
            }
        }

        return operation.Status == SceneLoadStatus.Completed
            ? Result.Success()
            : Result.Failure(operation.ErrorMessage ?? $"Scene load ended with status {operation.Status}.");
    }

    internal static void RegisterMaterializedScene(PendingSceneLoad pendingSceneLoad)
    {
        lock (s_loadedScenesLock)
        {
            s_loadedScenes[new SceneKey(pendingSceneLoad.World, pendingSceneLoad.Scene)] = pendingSceneLoad;
        }
    }

    internal static void DestroyAllScenesInWorld(World world)
    {
        PendingSceneLoad[]? toUnload = null;
        lock (s_loadedScenesLock)
        {
            List<PendingSceneLoad>? list = null;
            foreach (var kvp in s_loadedScenes)
            {
                if (kvp.Value.World == world)
                {
                    list ??= new List<PendingSceneLoad>();
                    list.Add(kvp.Value);
                }
            }

            if (list != null)
            {
                toUnload = list.ToArray();
            }
        }

        if (toUnload != null)
        {
            for (var i = 0; i < toUnload.Length; i++)
            {
                DestroyScene(toUnload[i].Scene, world);
            }
        }

        // Destroy any orphaned SceneID entities in this world that may have been spawned manually without a registered PendingSceneLoad.
        DestroyEntitiesWithSceneId(world, filterBySceneId: false, 0);
    }

    internal static void ReleaseWorldScenes(World world)
    {
        var count = s_pendingMaterialization.Count;
        for (var i = 0; i < count; i++)
        {
            if (!s_pendingMaterialization.TryDequeue(out var pending))
            {
                break;
            }

            if (pending.World == world)
            {
                pending.Cancel();
            }
            else if (!pending.IsTerminal)
            {
                s_pendingMaterialization.Enqueue(pending);
            }
        }

        lock (s_loadedScenesLock)
        {
            var keysToRemove = new List<SceneKey>();
            foreach (var kvp in s_loadedScenes)
            {
                if (kvp.Value.World == world)
                {
                    kvp.Value.ReleaseEntryReference();
                    s_recycledSceneIDs.Enqueue(kvp.Value.Scene.ID);
                    keysToRemove.Add(kvp.Key);
                }
            }

            for (var i = 0; i < keysToRemove.Count; i++)
            {
                s_loadedScenes.Remove(keysToRemove[i]);
            }
        }
    }

    /// <summary>
    /// Unloads <paramref name="scene"/> from <paramref name="world"/>, destroying its entities and releasing asset references.
    /// </summary>
    public static void UnloadScene(Scene scene, World world)
    {
        DestroyScene(scene, world);
    }

    /// <summary>
    /// Destroys all entities belonging to <paramref name="scene"/> in <paramref name="world"/> and releases its asset references.
    /// </summary>
    public static void DestroyScene(Scene scene, World world)
    {
        if (!scene.IsValid || world == null)
        {
            return;
        }

        DestroyEntitiesWithSceneId(world, filterBySceneId: true, scene.ID);

        lock (s_creationLock)
        {
            s_recycledSceneIDs.Enqueue(scene.ID);
        }

        lock (s_loadedScenesLock)
        {
            var key = new SceneKey(world, scene);
            if (s_loadedScenes.Remove(key, out var pendingLoad))
            {
                pendingLoad.ReleaseEntryReference();
            }
        }
    }

    /// <summary>
    /// Collects all entities belonging to <paramref name="scene"/> in <paramref name="world"/>.
    /// </summary>
    public static UnsafeList<Entity> GetSceneEntities(World world, Scene scene, AllocationHandle handle)
    {
        var entities = new UnsafeList<Entity>(128, handle);
        if (!scene.IsValid || world == null)
        {
            return entities;
        }

        var queryID = new QueryBuilder().WithAll<SceneID>().Build(world);
        ref var query = ref world.ComponentManager.GetEntityQueryReference(queryID);

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

    private static void DestroyEntitiesWithSceneId(World world, bool filterBySceneId, ushort targetSceneId)
    {
        var volumeQueryId = QueryBuilder.New().WithAll<SceneID, RenderVolumeDataContainer>().Build(world);
        ref var volumeQuery = ref world.ComponentManager.GetEntityQueryReference(volumeQueryId);
        foreach (var chunk in volumeQuery.GetChunkIterator())
        {
            ref readonly var sceneID = ref chunk.GetSharedComponent<SceneID>();
            if (filterBySceneId && sceneID.value != targetSceneId)
            {
                continue;
            }

            var containers = chunk.GetComponentDataRW<RenderVolumeDataContainer>();
            for (var i = 0; i < chunk.EntityCount; i++)
            {
                containers[i].Dispose();
            }
        }

        var queryID = QueryBuilder.New().WithAll<SceneID>().Build(world);
        ref var query = ref world.ComponentManager.GetEntityQueryReference(queryID);

        using var scope = AllocationManager.CreateStackScope();
        using var toDestroy = new UnsafeList<Entity>(64, scope.AllocationHandle);

        foreach (var chunk in query.GetChunkIterator())
        {
            ref readonly var sceneID = ref chunk.GetSharedComponent<SceneID>();
            if (filterBySceneId && sceneID.value != targetSceneId)
            {
                continue;
            }

            toDestroy.AddRange(chunk.GetEntities());
        }

        if (toDestroy.Count > 0)
        {
            world.EntityManager.DestroyEntities(toDestroy.AsSpan());
        }
    }
}
