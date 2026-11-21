using Ghost.Core;
using Ghost.Editor.Core.Commands;
using Ghost.Editor.Core.Selection;
using Ghost.Editor.Core.Sync;
using Ghost.Engine.Components;
using Ghost.Engine.Editor;
using Ghost.Engine.Streaming;
using Ghost.Engine.Systems;
using Ghost.Entities;
using Ghost.Graphics;
using Ghost.Studio.Rendering;
using System.Reflection;

namespace Ghost.Studio.Systems;

/// <summary>
/// Synchronizes the Editor <see cref="World"/> with <see cref="EditorSyncBridge"/>, drains pending UI/undo mutations, and refreshes live Inspector values without per-frame structural rebuilds.
/// </summary>
[UpdateBefore<EditorImGuiSystem>]
[UpdateBefore<EditorRenderSystemGroup>]
internal sealed unsafe class EditorEcsSyncSystem : SystemBase
{
    private interface IComponentAccessor
    {
        object Read(void* ptr);
        void Write(void* ptr, object value);
    }

    private sealed class ComponentAccessor<T> : IComponentAccessor
        where T : unmanaged
    {
        public object Read(void* ptr)
        {
            return *(T*)ptr;
        }

        public void Write(void* ptr, object value)
        {
            *(T*)ptr = (T)value;
        }
    }

    private readonly Dictionary<Type, IComponentAccessor> _accessors = new();
    private readonly List<PendingPropertyMutation> _pendingMutations = new();
    private readonly Dictionary<Entity, int> _entityNodeIdLookup = new();
    private readonly Dictionary<Entity, EntityNode> _entityNodeCache = new();

    private Identifier<EntityQuery> _sceneEntitiesQueryID;
    private int _lastSignatureDigest = -1;
    private IReadOnlyList<EntityNode> _cachedEntities = Array.Empty<EntityNode>();

    protected override void OnInitialize(scoped in SystemAPI systemAPI)
    {
        var bridge = EditorSyncBridge.Instance;
        if (systemAPI.World.TryGetService<AssetManager>(out var assetManager))
        {
            bridge.AssetManager = assetManager;
        }

        if (systemAPI.World.TryGetService<RenderEngine>(out var renderEngine))
        {
            bridge.ResourceManager = renderEngine.ResourceManager;
        }

        _sceneEntitiesQueryID = QueryBuilder.New()
            .WithAbsent<EditorCamera>()
            .Build(systemAPI.World);
    }

    protected override void OnUpdate(scoped in SystemUpdateAPI systemAPI)
    {
        var world = systemAPI.World;
        var em = world.EntityManager;
        var bridge = EditorSyncBridge.Instance;

        bridge.DrainWorldActions(world);
        ApplyPendingMutations(world, em, bridge);
        PumpPendingPropertyResolutions();

        ref var query = ref world.ComponentManager.GetEntityQueryReference(_sceneEntitiesQueryID);
        var digest = ComputeStructuralDigest(em, ref query);

        if (digest != _lastSignatureDigest)
        {
            _lastSignatureDigest = digest;
            var activeEcs = EditorSelection.Instance.ActiveEntity?.EcsEntity ?? Entity.Invalid;
            _cachedEntities = BuildEntityGraph(world, em, ref query, bridge);
            RestoreSelection(activeEcs);
        }
        else
        {
            RefreshActiveEntityValues(world, em);
        }

        var dt = MathF.Max(systemAPI.Time.DeltaTime, 0.0001f);
        var frameMs = systemAPI.Time.DeltaTime * 1000.0f;
        var fps = 1.0f / dt;

        bridge.PublishSnapshot(new SceneSnapshot(_cachedEntities, frameMs, fps));
    }

    protected override void OnCleanup(scoped in SystemAPI systemAPI)
    {
        for (var e = 0; e < _cachedEntities.Count; e++)
        {
            var components = _cachedEntities[e].Components;
            for (var c = 0; c < components.Count; c++)
            {
                var props = components[c].Properties;
                for (var p = 0; p < props.Count; p++)
                {
                    props[p].ReleaseResources();
                }
            }
        }

        var bridge = EditorSyncBridge.Instance;
        bridge.ReleaseAllTrackedResources();
        bridge.AssetManager = null;
        bridge.ResourceManager = null;

        _entityNodeCache.Clear();
        _entityNodeIdLookup.Clear();
        _pendingMutations.Clear();
        _cachedEntities = Array.Empty<EntityNode>();

        EditorSelection.Instance.Clear();
        CommandHistory.Instance.Clear();
    }

    private void ApplyPendingMutations(World world, EntityManager em, EditorSyncBridge bridge)
    {
        bridge.DrainMutations(_pendingMutations);
        for (var i = 0; i < _pendingMutations.Count; i++)
        {
            var mutation = _pendingMutations[i];
            if (!mutation.Entity.IsValid || !em.Exists(mutation.Entity))
            {
                continue;
            }

            var compId = ComponentRegistry.GetComponentID(mutation.ComponentType);
            if (compId.IsInvalid || !TryGetComponentDataPtr(world, em, mutation.Entity, compId, out var ptr))
            {
                continue;
            }

            var info = ComponentRegistry.GetComponentInfo(compId);
            if (info.IsShared)
            {
                continue;
            }

            var field = mutation.ComponentType.GetField(mutation.FieldName, BindingFlags.Public | BindingFlags.Instance);
            if (field == null)
            {
                continue;
            }

            var accessor = GetOrCreateAccessor(mutation.ComponentType);
            var boxed = accessor.Read(ptr);
            field.SetValue(boxed, mutation.Value);
            accessor.Write(ptr, boxed);

            if (mutation.ComponentType == typeof(LocalTransform) || mutation.ComponentType == typeof(LocalToWorld))
            {
                bridge.DirtyTracker.MarkTransformDirty(mutation.Entity);
            }
            else if (mutation.ComponentType == typeof(MeshInstance))
            {
                bridge.DirtyTracker.MarkGpuInstanceDirty(mutation.Entity);
            }
        }
    }

    private int ComputeStructuralDigest(EntityManager em, ref EntityQuery query)
    {
        var hash = new HashCode();
        var count = 0;

        foreach (var chunk in query.GetChunkIterator())
        {
            var entities = chunk.GetEntities();
            count += chunk.EntityCount;
            for (var i = 0; i < chunk.EntityCount; i++)
            {
                var entity = entities[i];
                hash.Add(entity.ID);
                hash.Add(entity.Generation);
                if (em.TryGetEntityLocation(entity, out var location))
                {
                    hash.Add((int)location.archetypeID);
                }

                if (em.HasComponent<Hierarchy>(entity))
                {
                    var parent = em.GetComponent<Hierarchy>(entity).parent;
                    hash.Add(parent.ID);
                    hash.Add(parent.Generation);
                }
            }
        }

        hash.Add(count);
        return hash.ToHashCode();
    }

    private List<EntityNode> BuildEntityGraph(World world, EntityManager em, ref EntityQuery query, EditorSyncBridge bridge)
    {
        _entityNodeIdLookup.Clear();

        var snapshotLookup = new Dictionary<Entity, EntityNode>(_entityNodeCache);
        var latest = bridge.LatestSnapshot;
        if (latest != null)
        {
            for (var i = 0; i < latest.Entities.Count; i++)
            {
                var snapNode = latest.Entities[i];
                if (snapNode.EcsEntity.IsValid)
                {
                    snapshotLookup[snapNode.EcsEntity] = snapNode;
                }
            }
        }

        _entityNodeCache.Clear();
        var result = new List<EntityNode>();
        var entityIndex = 1;

        foreach (var chunk in query.GetChunkIterator())
        {
            var entities = chunk.GetEntities();
            for (var i = 0; i < chunk.EntityCount; i++)
            {
                var entity = entities[i];
                var hasCached = snapshotLookup.TryGetValue(entity, out var existingNode) && existingNode != null;
                var nodeId = hasCached ? existingNode!.Id : (bridge.TryGetMappedNodeId(entity, out var existingId) ? existingId : bridge.AllocateEntityId());
                bridge.RegisterEntityMapping(entity, nodeId);
                _entityNodeIdLookup[entity] = nodeId;

                var compList = new List<ComponentNode>();
                var name = bridge.TryGetEntityName(entity, out var customName)
                    ? customName
                    : (hasCached ? existingNode!.Name : ResolveEntityDisplayName(em, entity, entityIndex));

                EntityNode entityNode;
                if (hasCached)
                {
                    entityNode = existingNode!;
                    entityNode.Name = name;
                    entityNode.ParentId = null;
                    entityNode.EcsEntity = entity;
                    PopulateComponentNodes(world, em, entity, entityNode, compList, bridge, existingNode!.Components);
                    entityNode.Components = compList;
                }
                else
                {
                    entityNode = new EntityNode(nodeId, name, compList)
                    {
                        EcsEntity = entity,
                    };
                    PopulateComponentNodes(world, em, entity, entityNode, compList, bridge, null);
                }

                _entityNodeCache[entity] = entityNode;
                result.Add(entityNode);
                entityIndex++;
            }
        }

        for (var i = 0; i < result.Count; i++)
        {
            var node = result[i];
            if (node.EcsEntity.IsValid && em.HasComponent<Hierarchy>(node.EcsEntity))
            {
                var parent = em.GetComponent<Hierarchy>(node.EcsEntity).parent;
                if (parent.IsValid && _entityNodeIdLookup.TryGetValue(parent, out var parentNodeId))
                {
                    node.ParentId = parentNodeId;
                }
            }
        }

        return result;
    }

    private void RestoreSelection(Entity activeEcs)
    {
        if (!activeEcs.IsValid)
        {
            return;
        }

        for (var i = 0; i < _cachedEntities.Count; i++)
        {
            if (_cachedEntities[i].EcsEntity == activeEcs)
            {
                EditorSelection.Instance.Select(_cachedEntities[i]);
                return;
            }
        }
    }

    private void PopulateComponentNodes(World world, EntityManager em, Entity entity, EntityNode entityNode, List<ComponentNode> dest, EditorSyncBridge bridge, IReadOnlyList<ComponentNode>? existingComponents)
    {
        if (!em.TryGetEntityLocation(entity, out var location))
        {
            return;
        }

        ref var archetype = ref world.ComponentManager.GetArchetypeReference(location.archetypeID);
        var it = archetype._signature.GetIterator();

        while (it.Next(out var compIdx))
        {
            var compId = new Identifier<IComponent>(compIdx);
            var info = ComponentRegistry.GetComponentInfo(compId);
            var isVolumeContainer = compIdx == ComponentTypeID<RenderVolumeDataContainer>.Value.Value;
            if (info.IsShared || (info.IsCleanup && !isVolumeContainer))
            {
                continue;
            }

            var compType = ComponentRegistry.s_runtimeIDToType[compIdx];
            if (compType == null || compType.GetCustomAttribute<HideEditorAttribute>() != null)
            {
                continue;
            }

            var ptr = archetype.GetComponentData(location.chunkIndex, location.rowIndex, compId);
            if (ptr == null)
            {
                continue;
            }

            ComponentNode? reused = null;
            if (existingComponents != null)
            {
                for (var c = 0; c < existingComponents.Count; c++)
                {
                    if (existingComponents[c].ComponentType == compType)
                    {
                        reused = existingComponents[c];
                        break;
                    }
                }
            }

            if (isVolumeContainer)
            {
                var pContainer = (RenderVolumeDataContainer*)ptr;
                if (reused != null && reused.Properties.Count > 0 && reused.Properties[0] is VolumeOverridesPropertyNode volumeProp)
                {
                    reused.EntityNode = entityNode;
                    reused.LiveValue = null;
                    volumeProp.RefreshFromContainer(in *pContainer);
                    dest.Add(reused);
                }
                else
                {
                    dest.Add(ComponentNodeFactory.CreateVolumeComponentNode(entityNode, in *pContainer, bridge));
                }

                continue;
            }

            var accessor = GetOrCreateAccessor(compType);
            var initialValue = accessor.Read(ptr);

            if (reused != null)
            {
                reused.EntityNode = entityNode;
                reused.LiveValue = initialValue;
                RefreshComponentProperties(reused, initialValue);
                dest.Add(reused);
            }
            else
            {
                dest.Add(ComponentNodeFactory.CreateComponentNode(compType, entityNode, initialValue, bridge));
            }
        }
    }

    private void RefreshActiveEntityValues(World world, EntityManager em)
    {
        var active = EditorSelection.Instance.ActiveEntity;
        if (active == null || !active.EcsEntity.IsValid || !em.Exists(active.EcsEntity))
        {
            return;
        }

        for (var i = 0; i < active.Components.Count; i++)
        {
            var compNode = active.Components[i];
            var compId = ComponentRegistry.GetComponentID(compNode.ComponentType);
            if (compId.IsInvalid || !TryGetComponentDataPtr(world, em, active.EcsEntity, compId, out var ptr))
            {
                continue;
            }

            if (compNode.ComponentType == typeof(RenderVolumeDataContainer))
            {
                if (compNode.Properties.Count > 0 && compNode.Properties[0] is VolumeOverridesPropertyNode volumeProp)
                {
                    volumeProp.RefreshFromContainer(in *(RenderVolumeDataContainer*)ptr);
                }

                continue;
            }

            var accessor = GetOrCreateAccessor(compNode.ComponentType);
            var boxed = accessor.Read(ptr);
            compNode.LiveValue = boxed;
            RefreshComponentProperties(compNode, boxed);
        }
    }

    private static void RefreshComponentProperties(ComponentNode compNode, object boxed)
    {
        for (var p = 0; p < compNode.Properties.Count; p++)
        {
            var prop = compNode.Properties[p];
            var field = compNode.ComponentType.GetField(prop.Name, BindingFlags.Public | BindingFlags.Instance);
            prop.RefreshFromEcs(boxed, field);
        }
    }

    private void PumpPendingPropertyResolutions()
    {
        for (var e = 0; e < _cachedEntities.Count; e++)
        {
            var components = _cachedEntities[e].Components;
            for (var c = 0; c < components.Count; c++)
            {
                var props = components[c].Properties;
                for (var p = 0; p < props.Count; p++)
                {
                    props[p].TryCompletePendingResolution();
                }
            }
        }
    }

    private static string ResolveEntityDisplayName(EntityManager em, Entity entity, int index)
    {
        if (em.HasComponent<DirectionalLight>(entity))
        {
            return $"Directional Light ({entity.ID})";
        }

        if (em.HasComponent<PunctualLight>(entity))
        {
            return $"Punctual Light ({entity.ID})";
        }

        if (em.HasComponent<RenderVolume>(entity))
        {
            return $"Global Volume ({entity.ID})";
        }

        if (em.HasComponent<Camera>(entity))
        {
            return $"Camera ({entity.ID})";
        }

        if (em.HasComponent<MeshInstance>(entity))
        {
            return $"Mesh Entity ({entity.ID})";
        }

        return $"Entity {index} ({entity.ID})";
    }

    private static bool TryGetComponentDataPtr(World world, EntityManager em, Entity entity, Identifier<IComponent> compId, out void* ptr)
    {
        ptr = null;
        if (!em.TryGetEntityLocation(entity, out var location))
        {
            return false;
        }

        ref var archetype = ref world.ComponentManager.GetArchetypeReference(location.archetypeID);
        if (!archetype.HasComponent(compId))
        {
            return false;
        }

        ptr = archetype.GetComponentData(location.chunkIndex, location.rowIndex, compId);
        return ptr != null;
    }

    private IComponentAccessor GetOrCreateAccessor(Type componentType)
    {
        if (!_accessors.TryGetValue(componentType, out var accessor))
        {
            var genericType = typeof(ComponentAccessor<>).MakeGenericType(componentType);
            accessor = (IComponentAccessor)Activator.CreateInstance(genericType)!;
            _accessors[componentType] = accessor;
        }

        return accessor;
    }
}
