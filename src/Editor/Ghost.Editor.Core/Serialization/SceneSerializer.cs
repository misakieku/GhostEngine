using System.Reflection;
using System.Text.Json;
using Ghost.AssetForge.Core.Models;
using Ghost.Core;
using Ghost.Editor.Core.Commands;
using Ghost.Editor.Core.Selection;
using Ghost.Editor.Core.Sync;
using Ghost.Engine;
using Ghost.Engine.Components;
using Ghost.Engine.Streaming;
using Ghost.Engine.Utilities;
using Ghost.Entities;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Editor.Core.Serialization;

/// <summary>
/// Serializes and deserializes editor authoring node hierarchies (<see cref="SceneSnapshot"/>) to and from
/// <see cref="SceneAssetDocument"/> (<c>.gscene</c>) JSON files without reading runtime GPU/ECS handles directly.
/// </summary>
public static unsafe class SceneSerializer
{
    private static readonly object s_typeCacheLock = new();
    private static readonly Dictionary<string, Type?> s_componentTypeCache = new(StringComparer.Ordinal);
    private static int s_cachedAssemblyCount = -1;

    /// <summary>
    /// Captures the current editor <paramref name="snapshot"/> as a <see cref="SceneAssetDocument"/>,
    /// storing standalone entities explicitly and prefab-linked groups by reference with sparse overrides.
    /// </summary>
    public static SceneAssetDocument SaveToDocument(SceneSnapshot snapshot, EditorSyncBridge bridge, IReadOnlyDictionary<Guid, IAssetEntry>? prefabLookup = null, ushort? sceneIdFilter = null)
    {
        var doc = new SceneAssetDocument();
        IReadOnlyList<EntityNode> sourceEntities = snapshot.Entities;
        if (sceneIdFilter.HasValue)
        {
            var filtered = new List<EntityNode>(snapshot.Entities.Count);
            for (var i = 0; i < snapshot.Entities.Count; i++)
            {
                if (snapshot.Entities[i].SceneId == sceneIdFilter.Value)
                {
                    filtered.Add(snapshot.Entities[i]);
                }
            }

            sourceEntities = filtered;
        }

        var ordered = OrderEntitiesForSerialization(sourceEntities);
        var byId = new Dictionary<int, EntityNode>(ordered.Count);
        for (var i = 0; i < ordered.Count; i++)
        {
            byId[ordered[i].Id] = ordered[i];
        }

        var prefabGroups = new Dictionary<Guid, List<EntityNode>>();
        var prefabGroupOrder = new List<Guid>();
        var jsonOptions = SceneJsonOptions.Default;

        for (var i = 0; i < ordered.Count; i++)
        {
            var node = ordered[i];
            if (node.PrefabLink != null)
            {
                if (!prefabGroups.TryGetValue(node.PrefabLink.InstanceGuid, out var group))
                {
                    group = new List<EntityNode>();
                    prefabGroups[node.PrefabLink.InstanceGuid] = group;
                    prefabGroupOrder.Add(node.PrefabLink.InstanceGuid);
                }

                group.Add(node);
                continue;
            }

            Guid? parentGuid = node.ParentId.HasValue && byId.TryGetValue(node.ParentId.Value, out var parentNode)
                ? parentNode.EditorGuid
                : null;

            var entityRec = new SceneEntityRecord
            {
                Guid = node.EditorGuid,
                Name = node.Name,
                ParentGuid = parentGuid
            };

            for (var c = 0; c < node.Components.Count; c++)
            {
                var compNode = node.Components[c];
                if (IsTransientComponent(compNode.ComponentType))
                {
                    continue;
                }

                entityRec.Components.Add(compNode.SerializeToRecord(jsonOptions));
            }

            doc.Entities.Add(entityRec);
        }

        for (var g = 0; g < prefabGroupOrder.Count; g++)
        {
            var instanceGuid = prefabGroupOrder[g];
            var nodes = prefabGroups[instanceGuid];
            var record = BuildPrefabInstanceRecord(instanceGuid, nodes, byId, bridge, prefabLookup, jsonOptions);
            doc.PrefabInstances.Add(record);
        }

        return doc;
    }

    /// <summary>
    /// Atomically writes <paramref name="doc"/> as indented <c>.gscene</c> JSON.
    /// </summary>
    public static void WriteDocumentToFile(string filePath, SceneAssetDocument doc)
    {
        var fullPath = Path.GetFullPath(filePath);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var tempPath = $"{fullPath}.tmp";
        var json = JsonSerializer.Serialize(doc, SceneAssetDocumentContext.Default.SceneAssetDocument);
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, fullPath, overwrite: true);
    }

    public static SceneAssetDocument SaveToFile(string filePath, SceneSnapshot snapshot, EditorSyncBridge bridge, IReadOnlyDictionary<Guid, IAssetEntry>? prefabLookup = null, ushort? sceneIdFilter = null)
    {
        var doc = SaveToDocument(snapshot, bridge, prefabLookup, sceneIdFilter);
        WriteDocumentToFile(filePath, doc);
        return doc;
    }

    public static SceneAssetDocument? LoadDocumentFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize(json, SceneAssetDocumentContext.Default.SceneAssetDocument);
    }

    public static Result LoadFromFile(World world, EditorSyncBridge bridge, string filePath, SceneLoadingType mode = SceneLoadingType.Single, Func<Guid, IAssetEntry?>? assetResolver = null, EditorSelection? selection = null, CommandHistory? history = null)
    {
        var doc = LoadDocumentFromFile(filePath);
        if (doc == null)
        {
            return Result.Failure($"Failed to deserialize scene file '{filePath}'.");
        }

        var fullPath = Path.GetFullPath(filePath);
        var res = LoadFromDocument(world, bridge, doc, mode, assetResolver, selection, history);
        if (res.IsSuccess)
        {
            var loadedSceneId = bridge.ActiveSceneId;
            bridge.ActiveScenePath = fullPath;
            bridge.RegisterScene(loadedSceneId, fullPath, isDirty: false);
            var snap = bridge.LatestSnapshot;
            if (snap != null)
            {
                bridge.PublishSnapshot(new SceneSnapshot(bridge.GetSceneNodeSnapshots(snap.Entities), snap.Entities, snap.FrameTimeMs, snap.Fps));
            }
        }

        return res;
    }

    /// <summary>
    /// Materializes a <see cref="SceneAssetDocument"/> into <paramref name="world"/> and publishes an updated <see cref="SceneSnapshot"/>.
    /// When <paramref name="mode"/> is <see cref="SceneLoadingType.Single"/>, clears only entities tagged with <see cref="SceneID"/>
    /// so persistent non-scene entities (such as <c>EditorCamera</c>) remain intact.
    /// </summary>
    public static Result LoadFromDocument(World world, EditorSyncBridge bridge, SceneAssetDocument doc, SceneLoadingType mode = SceneLoadingType.Single, Func<Guid, IAssetEntry?>? assetResolver = null, EditorSelection? selection = null, CommandHistory? history = null)
    {
        var em = world.EntityManager;
        var jsonOptions = SceneJsonOptions.Default;

        if (mode == SceneLoadingType.Single)
        {
            UnloadCurrentScene(world, bridge, selection, history);
        }
        else
        {
            bridge.ActiveSceneId = bridge.AllocateSceneId();
        }

        var sceneId = bridge.ActiveSceneId;
        var scenePath = mode == SceneLoadingType.Single ? bridge.ActiveScenePath : null;
        bridge.RegisterScene(sceneId, scenePath, isDirty: false);

        var existingNodes = mode == SceneLoadingType.Additive && bridge.LatestSnapshot != null
            ? new List<EntityNode>(bridge.LatestSnapshot.Entities)
            : new List<EntityNode>();

        var loadedNodes = new List<EntityNode>(existingNodes.Count + doc.Entities.Count);
        loadedNodes.AddRange(existingNodes);

        var guidToEntity = new Dictionary<Guid, Entity>();
        for (var i = 0; i < existingNodes.Count; i++)
        {
            if (existingNodes[i].EcsEntity.IsValid)
            {
                guidToEntity[existingNodes[i].EditorGuid] = existingNodes[i].EcsEntity;
            }
        }

        var pendingParents = new List<(Entity Child, Guid ParentGuid)>();
        var pendingEntityRefs = new List<(ComponentNode Component, SceneComponentRecord Record)>();
        var pendingPropertyOverrides = new List<(PropertyNode Prop, JsonElement Elem)>();

        for (var i = 0; i < doc.Entities.Count; i++)
        {
            var rec = doc.Entities[i];
            var entity = em.CreateEntity();
            em.AddSharedComponent(entity, new SceneID { value = sceneId });

            var entityGuid = rec.Guid != Guid.Empty ? rec.Guid : Guid.NewGuid();
            var nodeId = bridge.AllocateEntityId();
            var name = string.IsNullOrWhiteSpace(rec.Name) ? "Entity" : rec.Name;

            bridge.RegisterEntityMapping(entity, nodeId);
            bridge.RegisterEntityGuid(entity, entityGuid);
            bridge.SetEntityName(entity, name);

            var compNodes = new List<ComponentNode>(rec.Components.Count);
            var entityNode = new EntityNode(nodeId, name, compNodes)
            {
                EditorGuid = entityGuid,
                SceneId = sceneId,
                EcsEntity = entity
            };

            for (var c = 0; c < rec.Components.Count; c++)
            {
                var compRec = rec.Components[c];
                var compType = ResolveComponentType(compRec.Type);
                if (compType == null || IsTransientComponent(compType) || compType == typeof(Hierarchy))
                {
                    continue;
                }

                var compNode = ComponentNodeFactory.CreateComponentNode(compType, entityNode, null, bridge);
                compNode.DeserializeFromRecord(compRec, jsonOptions);
                ComponentReflectionAccessor.TryAddOrSet(em, entity, compNode);
                compNodes.Add(compNode);

                if (HasEntityRefProperty(compNode))
                {
                    pendingEntityRefs.Add((compNode, compRec));
                }
            }

            guidToEntity[entityGuid] = entity;
            loadedNodes.Add(entityNode);

            if (rec.ParentGuid.HasValue && rec.ParentGuid.Value != Guid.Empty)
            {
                pendingParents.Add((entity, rec.ParentGuid.Value));
            }
        }

        for (var p = 0; p < doc.PrefabInstances.Count; p++)
        {
            var prefabRec = doc.PrefabInstances[p];
            var prefabEntry = AcquirePrefabEntry(prefabRec.PrefabGuid, bridge, assetResolver, out var releaseAfterSpawn);
            if (prefabEntry == null)
            {
                Logger.Warning($"Skipping missing prefab asset '{prefabRec.PrefabGuid}' during scene load.");
                continue;
            }

            try
            {
                var spawnOptions = new PrefabSpawnOptions
                {
                    ApplyTransform = false,
                    InstanceGuidOverride = prefabRec.InstanceGuid,
                    NodeGuidOverrides = prefabRec.NodeGuids,
                    NameOverrides = prefabRec.NameOverrides
                };

                if (!EditorPrefabUtility.TrySpawnLinkedPrefab(world, prefabEntry, bridge, float3.zero, quaternion.identity, float3.one, out var spawnedNodes, spawnOptions))
                {
                    continue;
                }

                var spawnedByGuid = new Dictionary<Guid, EntityNode>(spawnedNodes.Count);
                for (var s = 0; s < spawnedNodes.Count; s++)
                {
                    var sn = spawnedNodes[s];
                    var nodeId = bridge.AllocateEntityId();
                    bridge.RegisterEntityMapping(sn.Entity, nodeId);

                    var compNodes = BuildComponentNodesFromEcs(world, em, sn.Entity, prefabEntry, sn.Link.TemplateEntityIndex, bridge, out var nodeTemplatePlaceholder);
                    nodeTemplatePlaceholder.Name = sn.Name;
                    nodeTemplatePlaceholder.EditorGuid = sn.EditorGuid;
                    nodeTemplatePlaceholder.PrefabLink = sn.Link;
                    nodeTemplatePlaceholder.EcsEntity = sn.Entity;

                    // Rebind EntityNode with allocated nodeId so Snapshot Id matches bridge mapping cleanly.
                    var entityNode = new EntityNode(nodeId, sn.Name, compNodes)
                    {
                        EditorGuid = sn.EditorGuid,
                        SceneId = sceneId,
                        PrefabLink = sn.Link,
                        EcsEntity = sn.Entity
                    };
                    for (var c = 0; c < compNodes.Count; c++)
                    {
                        compNodes[c].EntityNode = entityNode;
                    }

                    guidToEntity[sn.EditorGuid] = sn.Entity;
                    spawnedByGuid[sn.EditorGuid] = entityNode;
                    loadedNodes.Add(entityNode);
                }

                ApplyPrefabInstanceOverrides(em, prefabRec, spawnedByGuid, bridge, jsonOptions, pendingEntityRefs, pendingPropertyOverrides);

                if (prefabRec.ParentGuid.HasValue && prefabRec.ParentGuid.Value != Guid.Empty && spawnedNodes.Count > 0)
                {
                    pendingParents.Add((spawnedNodes[0].Entity, prefabRec.ParentGuid.Value));
                }
            }
            finally
            {
                if (releaseAfterSpawn)
                {
                    prefabEntry.Release();
                }
            }
        }

        // Wire parent-child hierarchy in reverse order because HierarchyUtility.SetParent prepends to firstChild.
        for (var i = pendingParents.Count - 1; i >= 0; i--)
        {
            var (child, parentGuid) = pendingParents[i];
            if (!guidToEntity.TryGetValue(parentGuid, out var parentEcs) || !em.Exists(parentEcs))
            {
                continue;
            }

            if (!em.HasComponent<Hierarchy>(child))
            {
                em.AddComponent(child, Hierarchy.Root);
            }

            if (!em.HasComponent<Hierarchy>(parentEcs))
            {
                em.AddComponent(parentEcs, Hierarchy.Root);
            }

            HierarchyUtility.SetParent(world, child, parentEcs);
        }

        var ecsToNodeId = new Dictionary<Entity, int>(loadedNodes.Count);
        for (var i = 0; i < loadedNodes.Count; i++)
        {
            ecsToNodeId[loadedNodes[i].EcsEntity] = loadedNodes[i].Id;
        }

        for (var i = 0; i < loadedNodes.Count; i++)
        {
            var node = loadedNodes[i];
            if (em.HasComponent<Hierarchy>(node.EcsEntity))
            {
                var parentEcs = em.GetComponent<Hierarchy>(node.EcsEntity).parent;
                if (parentEcs.IsValid && ecsToNodeId.TryGetValue(parentEcs, out var parentId))
                {
                    node.ParentId = parentId;
                }
            }
        }

        bridge.PublishSnapshot(new SceneSnapshot(bridge.GetSceneNodeSnapshots(loadedNodes), loadedNodes));

        // Second pass binds EntityRefPropertyNode targets now that LatestSnapshot contains every loaded EntityGuid.
        for (var i = 0; i < pendingEntityRefs.Count; i++)
        {
            var (compNode, compRec) = pendingEntityRefs[i];
            compNode.DeserializeFromRecord(compRec, jsonOptions);
            ComponentReflectionAccessor.TryAddOrSet(em, compNode.EntityNode.EcsEntity, compNode);
        }

        for (var i = 0; i < pendingPropertyOverrides.Count; i++)
        {
            var (prop, elem) = pendingPropertyOverrides[i];
            prop.DeserializeFromJson(elem, jsonOptions);
            ComponentReflectionAccessor.TryAddOrSet(em, prop.ComponentNode.EntityNode.EcsEntity, prop.ComponentNode);
        }

        for (var i = 0; i < loadedNodes.Count; i++)
        {
            var ecs = loadedNodes[i].EcsEntity;
            if (em.HasComponent<LocalTransform>(ecs) || em.HasComponent<LocalToWorld>(ecs))
            {
                bridge.DirtyTracker.MarkTransformDirty(ecs);
            }

            for (var c = 0; c < loadedNodes[i].Components.Count; c++)
            {
                var props = loadedNodes[i].Components[c].Properties;
                for (var p = 0; p < props.Count; p++)
                {
                    props[p].TryCompletePendingResolution();
                }
            }
        }

        if (mode == SceneLoadingType.Single)
        {
            bridge.SetSceneDirty(sceneId, false);
        }

        return Result.Success();
    }

    /// <summary>
    /// Unloads a specific loaded scene by <paramref name="sceneId"/>. If it is the sole loaded scene, resets into a blank Untitled scene.
    /// </summary>
    public static void UnloadScene(World world, EditorSyncBridge bridge, ushort sceneId, EditorSelection? selection = null, CommandHistory? history = null)
    {
        var registered = bridge.GetRegisteredSceneIds();
        var hasOtherScenes = false;
        ushort fallbackSceneId = 0;
        for (var i = 0; i < registered.Count; i++)
        {
            if (registered[i] != sceneId)
            {
                if (!hasOtherScenes)
                {
                    fallbackSceneId = registered[i];
                    hasOtherScenes = true;
                }
            }
        }

        if (!hasOtherScenes)
        {
            bridge.ActiveScenePath = null;
            UnloadCurrentScene(world, bridge, selection, history);
            bridge.PublishSnapshot(new SceneSnapshot(bridge.GetSceneNodeSnapshots(Array.Empty<EntityNode>()), Array.Empty<EntityNode>()));
            return;
        }

        var em = world.EntityManager;
        var queryId = new QueryBuilder().WithAll<SceneID>().Build(world);
        ref var query = ref world.ComponentManager.GetEntityQueryReference(queryId);
        var toDestroy = new List<Entity>();

        foreach (var chunk in query.GetChunkIterator())
        {
            var entities = chunk.GetEntities();
            for (var i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                if (em.HasComponent<SceneID>(entity) && em.GetSharedComponent<SceneID>(entity).value == sceneId)
                {
                    toDestroy.Add(entity);
                }
            }
        }

        for (var i = 0; i < toDestroy.Count; i++)
        {
            var entity = toDestroy[i];
            if (!em.Exists(entity))
            {
                continue;
            }

            if (em.HasComponent<Hierarchy>(entity))
            {
                var hier = em.GetComponent<Hierarchy>(entity);
                if (hier.parent.IsValid && em.Exists(hier.parent))
                {
                    HierarchyUtility.RemoveParent(world, entity);
                }

                var child = hier.firstChild;
                while (child.IsValid && em.Exists(child))
                {
                    var next = em.HasComponent<Hierarchy>(child) ? em.GetComponent<Hierarchy>(child).nextSibling : Entity.Invalid;
                    if (!em.HasComponent<SceneID>(child) || em.GetSharedComponent<SceneID>(child).value != sceneId)
                    {
                        HierarchyUtility.RemoveParent(world, child);
                    }

                    child = next;
                }
            }

            if (em.HasComponent<RenderVolumeDataContainer>(entity))
            {
                em.GetComponent<RenderVolumeDataContainer>(entity).Dispose();
                em.RemoveComponent<RenderVolumeDataContainer>(entity);
            }

            bridge.RemoveEntityMetadata(entity);
            em.DestroyEntity(entity);
        }

        bridge.UnregisterScene(sceneId);
        history?.Clear();

        if (selection != null)
        {
            if (selection.ActiveObject is SceneNode sn && sn.SceneId == sceneId)
            {
                selection.Clear();
            }
            else if (selection.ActiveEntity != null && selection.ActiveEntity.SceneId == sceneId)
            {
                selection.Clear();
            }
        }

        if (bridge.ActiveSceneId == sceneId)
        {
            bridge.ActiveSceneId = fallbackSceneId;
            bridge.ActiveScenePath = bridge.TryGetScenePath(fallbackSceneId, out var fallbackPath) ? fallbackPath : null;
        }

        var current = bridge.LatestSnapshot;
        var remainingEntities = new List<EntityNode>();
        var removedIds = new HashSet<int>();
        if (current != null)
        {
            for (var i = 0; i < current.Entities.Count; i++)
            {
                if (current.Entities[i].SceneId == sceneId)
                {
                    removedIds.Add(current.Entities[i].Id);
                }
                else
                {
                    remainingEntities.Add(current.Entities[i]);
                }
            }

            for (var i = 0; i < remainingEntities.Count; i++)
            {
                var e = remainingEntities[i];
                if (e.ParentId.HasValue && removedIds.Contains(e.ParentId.Value))
                {
                    e.ParentId = null;
                }
            }
        }

        bridge.PublishSnapshot(new SceneSnapshot(bridge.GetSceneNodeSnapshots(remainingEntities), remainingEntities, current?.FrameTimeMs ?? 16.6f, current?.Fps ?? 60.0f));
    }

    /// <summary>
    /// Unloads all entities, scene-retained assets, and tracked UI resources belonging to the active editor scene.
    /// </summary>
    public static void UnloadCurrentScene(World world, EditorSyncBridge bridge, EditorSelection? selection = null, CommandHistory? history = null)
    {
        selection?.Clear();
        history?.Clear();
        DestroyAllSceneEntities(world, bridge);
        bridge.ReleaseAllTrackedResources();
        bridge.ReleaseSceneAssets();
        bridge.ClearSceneMetadata();
        bridge.ActiveSceneId = 0;
    }

    private static void DestroyAllSceneEntities(World world, EditorSyncBridge bridge)
    {
        var em = world.EntityManager;
        var queryId = new QueryBuilder().WithAll<SceneID>().Build(world);
        ref var query = ref world.ComponentManager.GetEntityQueryReference(queryId);

        var toDestroy = new List<Entity>();
        foreach (var chunk in query.GetChunkIterator())
        {
            var entities = chunk.GetEntities();
            for (var i = 0; i < entities.Length; i++)
            {
                toDestroy.Add(entities[i]);
            }
        }

        for (var i = 0; i < toDestroy.Count; i++)
        {
            var entity = toDestroy[i];
            if (!em.Exists(entity))
            {
                continue;
            }

            if (em.HasComponent<RenderVolumeDataContainer>(entity))
            {
                em.GetComponent<RenderVolumeDataContainer>(entity).Dispose();
                em.RemoveComponent<RenderVolumeDataContainer>(entity);
            }

            bridge.RemoveEntityMetadata(entity);
            em.DestroyEntity(entity);
        }
    }

    private static ScenePrefabInstanceRecord BuildPrefabInstanceRecord(Guid instanceGuid, List<EntityNode> groupNodes, Dictionary<int, EntityNode> byId, EditorSyncBridge bridge, IReadOnlyDictionary<Guid, IAssetEntry>? prefabLookup, JsonSerializerOptions jsonOptions)
    {
        groupNodes.Sort((a, b) => a.PrefabLink!.TemplateEntityIndex.CompareTo(b.PrefabLink!.TemplateEntityIndex));

        var rootNode = groupNodes[0];
        var prefabGuid = rootNode.PrefabLink!.PrefabAssetGuid;
        Guid? rootParentGuid = rootNode.ParentId.HasValue && byId.TryGetValue(rootNode.ParentId.Value, out var parentNode)
            ? parentNode.EditorGuid
            : null;

        var record = new ScenePrefabInstanceRecord
        {
            InstanceGuid = instanceGuid,
            PrefabGuid = prefabGuid,
            ParentGuid = rootParentGuid
        };

        var prefabEntry = AcquirePrefabEntry(prefabGuid, bridge, prefabLookup != null ? g => prefabLookup.TryGetValue(g, out var e) ? e : null : null, out var releaseAfterRead);
        var defaultBaseName = prefabEntry != null ? EditorPrefabUtility.DerivePrefabDisplayName(prefabEntry) : "Prefab";

        var scratchBridge = prefabEntry != null ? new EditorSyncBridge() : null;
        try
        {
            var loadedData = (prefabEntry as PrefabAssetEntry)?.PrefabData;

            for (var n = 0; n < groupNodes.Count; n++)
            {
                var node = groupNodes[n];
                var templateIndex = node.PrefabLink!.TemplateEntityIndex;
                record.NodeGuids[templateIndex] = node.EditorGuid;

                var expectedName = templateIndex == 0 ? defaultBaseName : $"{defaultBaseName}_Node_{templateIndex}";
                if (!string.Equals(node.Name, expectedName, StringComparison.Ordinal))
                {
                    record.NameOverrides ??= new Dictionary<int, string>();
                    record.NameOverrides[templateIndex] = node.Name;
                }

                var liveRecords = new Dictionary<string, SceneComponentRecord>(StringComparer.Ordinal);
                for (var c = 0; c < node.Components.Count; c++)
                {
                    var compNode = node.Components[c];
                    if (IsTransientComponent(compNode.ComponentType))
                    {
                        continue;
                    }

                    var serialized = compNode.SerializeToRecord(jsonOptions);
                    liveRecords[serialized.Type] = serialized;
                }

                var baseRecords = BuildTemplateComponentRecords(loadedData, prefabEntry, templateIndex, scratchBridge!, jsonOptions);
                if (baseRecords == null)
                {
                    // Fallback when prefab asset isn't available locally: emit every live component as added.
                    foreach (var live in liveRecords.Values)
                    {
                        record.AddedComponents ??= new List<SceneAddedComponentRecord>();
                        record.AddedComponents.Add(new SceneAddedComponentRecord
                        {
                            TargetGuid = node.EditorGuid,
                            Component = live.Type,
                            Fields = live.Fields,
                            VolumeOverrides = live.VolumeOverrides
                        });
                    }

                    continue;
                }

                foreach (var baseKvp in baseRecords)
                {
                    if (!liveRecords.ContainsKey(baseKvp.Key))
                    {
                        record.RemovedComponents ??= new List<SceneRemovedComponentRecord>();
                        record.RemovedComponents.Add(new SceneRemovedComponentRecord
                        {
                            TargetGuid = node.EditorGuid,
                            Component = baseKvp.Key
                        });
                    }
                }

                foreach (var liveKvp in liveRecords)
                {
                    if (!baseRecords.TryGetValue(liveKvp.Key, out var baseRec))
                    {
                        record.AddedComponents ??= new List<SceneAddedComponentRecord>();
                        record.AddedComponents.Add(new SceneAddedComponentRecord
                        {
                            TargetGuid = node.EditorGuid,
                            Component = liveKvp.Key,
                            Fields = liveKvp.Value.Fields,
                            VolumeOverrides = liveKvp.Value.VolumeOverrides
                        });
                        continue;
                    }

                    if (liveKvp.Key == typeof(RenderVolumeDataContainer).FullName)
                    {
                        AppendVolumeOverrideDiffs(record, node.EditorGuid, baseRec.VolumeOverrides, liveKvp.Value.VolumeOverrides, jsonOptions);
                        continue;
                    }

                    foreach (var fieldKvp in liveKvp.Value.Fields)
                    {
                        if (!baseRec.Fields.TryGetValue(fieldKvp.Key, out var baseElem) || !SceneJsonOptions.JsonElementsEqual(fieldKvp.Value, baseElem))
                        {
                            record.PropertyOverrides ??= new List<ScenePropertyOverrideRecord>();
                            record.PropertyOverrides.Add(new ScenePropertyOverrideRecord
                            {
                                TargetGuid = node.EditorGuid,
                                Component = liveKvp.Key,
                                Field = fieldKvp.Key,
                                Value = fieldKvp.Value
                            });
                        }
                    }
                }
            }
        }
        finally
        {
            scratchBridge?.ReleaseAllTrackedResources();
            if (releaseAfterRead && prefabEntry != null && prefabEntry.State == AssetState.Ready)
            {
                prefabEntry.Release();
            }
        }

        return record;
    }

    private static Dictionary<string, SceneComponentRecord>? BuildTemplateComponentRecords(LoadedPrefabData? loadedData, IAssetEntry? prefabEntry, int templateIndex, EditorSyncBridge scratchBridge, JsonSerializerOptions jsonOptions)
    {
        if (loadedData == null || prefabEntry == null || templateIndex < 0 || templateIndex >= loadedData.Entities.Length)
        {
            return null;
        }

        ref readonly var prefabEnt = ref loadedData.Entities[templateIndex];
        var result = new Dictionary<string, SceneComponentRecord>(StringComparer.Ordinal);
        var dummyEntity = new EntityNode(0, string.Empty, Array.Empty<ComponentNode>());

        var hasMeshFixup = PrefabUtility.TryGetTemplateEntityFixupGuids(prefabEntry, templateIndex, out var baseMeshGuid, out var baseMatGuids);

        for (var c = 0; c < prefabEnt.ComponentIDs.Length; c++)
        {
            var compId = prefabEnt.ComponentIDs[c];
            if (compId.IsInvalid || !ComponentRegistry.s_runtimeIDToType.TryGetValue(compId.Value, out var compType))
            {
                continue;
            }

            if (compType == null || IsTransientComponent(compType) || compType == typeof(Hierarchy))
            {
                continue;
            }

            var info = ComponentRegistry.GetComponentInfo(compId);
            if (info.IsShared || (info.IsCleanup && compType != typeof(RenderVolumeDataContainer)))
            {
                continue;
            }

            object? boxed = null;
            var rawBytes = prefabEnt.ComponentData[c];
            if (rawBytes != null && rawBytes.Length > 0 && compType != typeof(RenderVolumeDataContainer))
            {
                fixed (byte* pData = rawBytes)
                {
                    ComponentReflectionAccessor.TryBoxFromPointer(compType, pData, out boxed);
                }
            }

            var compNode = ComponentNodeFactory.CreateComponentNode(compType, dummyEntity, boxed, scratchBridge);
            if (hasMeshFixup && compType == typeof(MeshInstance))
            {
                if (baseMeshGuid != Guid.Empty && compNode.FindProperty(nameof(MeshInstance.mesh)) is AssetPropertyNode meshProp)
                {
                    meshProp.ApplyValue(baseMeshGuid);
                }

                if (baseMatGuids.Length > 0 && compNode.FindProperty(nameof(MeshInstance.materialPalette)) is MaterialPalettePropertyNode paletteProp)
                {
                    paletteProp.ApplyValue(baseMatGuids);
                }
            }

            var record = compNode.SerializeToRecord(jsonOptions);
            result[record.Type] = record;
        }

        return result;
    }

    private static List<ComponentNode> BuildComponentNodesFromEcs(World world, EntityManager em, Entity entity, IAssetEntry prefabEntry, int templateIndex, EditorSyncBridge bridge, out EntityNode placeholderNode)
    {
        var compList = new List<ComponentNode>();
        placeholderNode = new EntityNode(0, string.Empty, compList);
        if (!em.TryGetEntityLocation(entity, out var location))
        {
            return compList;
        }

        var hasMeshFixup = PrefabUtility.TryGetTemplateEntityFixupGuids(prefabEntry, templateIndex, out var baseMeshGuid, out var baseMatGuids);

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

            if (!ComponentRegistry.s_runtimeIDToType.TryGetValue(compIdx, out var compType) || compType == null || compType == typeof(Hierarchy) || IsTransientComponent(compType))
            {
                continue;
            }

            var ptr = archetype.GetComponentData(location.chunkIndex, location.rowIndex, compId);
            if (ptr == null)
            {
                continue;
            }

            if (isVolumeContainer)
            {
                compList.Add(ComponentNodeFactory.CreateVolumeComponentNode(placeholderNode, in *(RenderVolumeDataContainer*)ptr, bridge));
                continue;
            }

            ComponentReflectionAccessor.TryBoxFromPointer(compType, ptr, out var initialValue);
            var compNode = ComponentNodeFactory.CreateComponentNode(compType, placeholderNode, initialValue, bridge);

            if (hasMeshFixup && compType == typeof(MeshInstance))
            {
                bridge.TryConsumeInitialMeshAuthoring(entity, out _, out _);
                if (baseMeshGuid != Guid.Empty && compNode.FindProperty(nameof(MeshInstance.mesh)) is AssetPropertyNode meshProp)
                {
                    meshProp.ApplyValue(baseMeshGuid);
                }

                if (baseMatGuids.Length > 0 && compNode.FindProperty(nameof(MeshInstance.materialPalette)) is MaterialPalettePropertyNode paletteProp)
                {
                    paletteProp.ApplyValue(baseMatGuids);
                }
            }

            compList.Add(compNode);
        }

        return compList;
    }

    private static void ApplyPrefabInstanceOverrides(EntityManager em, ScenePrefabInstanceRecord prefabRec, Dictionary<Guid, EntityNode> spawnedByGuid, EditorSyncBridge bridge, JsonSerializerOptions jsonOptions, List<(ComponentNode, SceneComponentRecord)> pendingEntityRefs, List<(PropertyNode, JsonElement)> pendingPropertyOverrides)
    {
        if (prefabRec.RemovedComponents != null)
        {
            for (var i = 0; i < prefabRec.RemovedComponents.Count; i++)
            {
                var rem = prefabRec.RemovedComponents[i];
                if (!spawnedByGuid.TryGetValue(rem.TargetGuid, out var node))
                {
                    continue;
                }

                var compType = ResolveComponentType(rem.Component);
                if (compType == null)
                {
                    continue;
                }

                ComponentReflectionAccessor.TryRemove(em, node.EcsEntity, compType);
                if (node.Components is List<ComponentNode> mutable)
                {
                    for (var c = mutable.Count - 1; c >= 0; c--)
                    {
                        if (mutable[c].ComponentType == compType)
                        {
                            mutable.RemoveAt(c);
                        }
                    }
                }
            }
        }

        if (prefabRec.AddedComponents != null)
        {
            for (var i = 0; i < prefabRec.AddedComponents.Count; i++)
            {
                var add = prefabRec.AddedComponents[i];
                if (!spawnedByGuid.TryGetValue(add.TargetGuid, out var node))
                {
                    continue;
                }

                var compType = ResolveComponentType(add.Component);
                if (compType == null || IsTransientComponent(compType) || compType == typeof(Hierarchy))
                {
                    continue;
                }

                var compRec = new SceneComponentRecord
                {
                    Type = add.Component,
                    Fields = add.Fields,
                    VolumeOverrides = add.VolumeOverrides
                };

                var compNode = ComponentNodeFactory.CreateComponentNode(compType, node, null, bridge);
                compNode.DeserializeFromRecord(compRec, jsonOptions);
                ComponentReflectionAccessor.TryAddOrSet(em, node.EcsEntity, compNode);

                if (node.Components is List<ComponentNode> mutable)
                {
                    mutable.Add(compNode);
                }

                if (HasEntityRefProperty(compNode))
                {
                    pendingEntityRefs.Add((compNode, compRec));
                }
            }
        }

        if (prefabRec.PropertyOverrides != null)
        {
            for (var i = 0; i < prefabRec.PropertyOverrides.Count; i++)
            {
                var ovr = prefabRec.PropertyOverrides[i];
                if (!spawnedByGuid.TryGetValue(ovr.TargetGuid, out var node))
                {
                    continue;
                }

                var compType = ResolveComponentType(ovr.Component);
                if (compType == null)
                {
                    continue;
                }

                ComponentNode? compNode = null;
                for (var c = 0; c < node.Components.Count; c++)
                {
                    if (node.Components[c].ComponentType == compType)
                    {
                        compNode = node.Components[c];
                        break;
                    }
                }

                if (compNode == null)
                {
                    continue;
                }

                var prop = compNode.FindProperty(ovr.Field);
                if (prop == null)
                {
                    continue;
                }

                if (prop.RequiresPostSnapshotBind)
                {
                    pendingPropertyOverrides.Add((prop, ovr.Value));
                    continue;
                }

                prop.DeserializeFromJson(ovr.Value, jsonOptions);
                ComponentReflectionAccessor.TryAddOrSet(em, node.EcsEntity, compNode);
            }
        }

        if (prefabRec.VolumeOverrides != null)
        {
            ApplyPrefabVolumeInstanceOverrides(em, prefabRec.VolumeOverrides, spawnedByGuid, bridge, jsonOptions, pendingPropertyOverrides);
        }
    }

    private static void AppendVolumeOverrideDiffs(ScenePrefabInstanceRecord record, Guid targetGuid, List<SceneVolumeOverrideRecord>? baseOverrides, List<SceneVolumeOverrideRecord>? liveOverrides, JsonSerializerOptions jsonOptions)
    {
        var baseByType = new Dictionary<string, SceneVolumeOverrideRecord>(StringComparer.Ordinal);
        if (baseOverrides != null)
        {
            for (var i = 0; i < baseOverrides.Count; i++)
            {
                baseByType[baseOverrides[i].Type] = baseOverrides[i];
            }
        }

        var liveByType = new Dictionary<string, SceneVolumeOverrideRecord>(StringComparer.Ordinal);
        if (liveOverrides != null)
        {
            for (var i = 0; i < liveOverrides.Count; i++)
            {
                liveByType[liveOverrides[i].Type] = liveOverrides[i];
            }
        }

        foreach (var baseKvp in baseByType)
        {
            if (!liveByType.ContainsKey(baseKvp.Key))
            {
                record.VolumeOverrides ??= new List<ScenePrefabVolumeOverrideRecord>();
                record.VolumeOverrides.Add(new ScenePrefabVolumeOverrideRecord
                {
                    TargetGuid = targetGuid,
                    VolumeType = baseKvp.Key,
                    Removed = true
                });
            }
        }

        foreach (var liveKvp in liveByType)
        {
            if (!baseByType.TryGetValue(liveKvp.Key, out var baseVol))
            {
                record.VolumeOverrides ??= new List<ScenePrefabVolumeOverrideRecord>();
                record.VolumeOverrides.Add(new ScenePrefabVolumeOverrideRecord
                {
                    TargetGuid = targetGuid,
                    VolumeType = liveKvp.Key,
                    Fields = new Dictionary<string, JsonElement>(liveKvp.Value.Fields, StringComparer.Ordinal)
                });
                continue;
            }

            Dictionary<string, JsonElement>? diffFields = null;
            foreach (var liveField in liveKvp.Value.Fields)
            {
                if (!baseVol.Fields.TryGetValue(liveField.Key, out var baseElem) || !SceneJsonOptions.JsonElementsEqual(liveField.Value, baseElem))
                {
                    diffFields ??= new Dictionary<string, JsonElement>(StringComparer.Ordinal);
                    diffFields[liveField.Key] = liveField.Value;
                }
            }

            foreach (var baseFieldKey in baseVol.Fields.Keys)
            {
                if (!liveKvp.Value.Fields.ContainsKey(baseFieldKey))
                {
                    diffFields ??= new Dictionary<string, JsonElement>(StringComparer.Ordinal);
                    diffFields[baseFieldKey] = JsonSerializer.SerializeToElement<object?>(null, jsonOptions);
                }
            }

            if (diffFields != null && diffFields.Count > 0)
            {
                record.VolumeOverrides ??= new List<ScenePrefabVolumeOverrideRecord>();
                record.VolumeOverrides.Add(new ScenePrefabVolumeOverrideRecord
                {
                    TargetGuid = targetGuid,
                    VolumeType = liveKvp.Key,
                    Fields = diffFields
                });
            }
        }
    }

    private static void ApplyPrefabVolumeInstanceOverrides(EntityManager em, List<ScenePrefabVolumeOverrideRecord> volumeOverrides, Dictionary<Guid, EntityNode> spawnedByGuid, EditorSyncBridge bridge, JsonSerializerOptions jsonOptions, List<(PropertyNode, JsonElement)> pendingPropertyOverrides)
    {
        var touchedNodes = new HashSet<(EntityNode Node, ComponentNode Comp)>();

        for (var i = 0; i < volumeOverrides.Count; i++)
        {
            var volOvr = volumeOverrides[i];
            if (!spawnedByGuid.TryGetValue(volOvr.TargetGuid, out var node))
            {
                continue;
            }

            if (!VolumeTypeRegistry.TryGetAccessor(volOvr.VolumeType, out var accessor))
            {
                continue;
            }

            ComponentNode? rvdcNode = null;
            VolumeOverridesPropertyNode? volumeProp = null;
            for (var c = 0; c < node.Components.Count; c++)
            {
                if (node.Components[c].ComponentType == typeof(RenderVolumeDataContainer) && node.Components[c].Properties.Count > 0 && node.Components[c].Properties[0] is VolumeOverridesPropertyNode vp)
                {
                    rvdcNode = node.Components[c];
                    volumeProp = vp;
                    break;
                }
            }

            if (volOvr.Removed)
            {
                if (volumeProp != null && rvdcNode != null && volumeProp.RemoveOverride(accessor.SettingsType))
                {
                    touchedNodes.Add((node, rvdcNode));
                }

                continue;
            }

            if (volumeProp == null || rvdcNode == null)
            {
                rvdcNode = ComponentNodeFactory.CreateVolumeComponentNode(node, default, bridge);
                if (node.Components is List<ComponentNode> mutable)
                {
                    mutable.Add(rvdcNode);
                }
                else
                {
                    var next = new List<ComponentNode>(node.Components);
                    next.Add(rvdcNode);
                    node.Components = next;
                }

                volumeProp = (VolumeOverridesPropertyNode)rvdcNode.Properties[0];
            }

            if (!volumeProp.HasOverride(accessor.SettingsType))
            {
                volumeProp.SetOverride(accessor.SettingsType, null, null, false);
            }

            var entry = volumeProp.FindOverride(accessor.SettingsType);
            if (entry == null)
            {
                continue;
            }

            foreach (var fieldKvp in volOvr.Fields)
            {
                var field = entry.FindField(fieldKvp.Key);
                if (field == null)
                {
                    continue;
                }

                if (fieldKvp.Value.ValueKind == JsonValueKind.Null)
                {
                    volumeProp.SetFieldState(accessor.SettingsType, field.Name, false, field.DefaultFieldValue);
                    continue;
                }

                if (field.ValueNode.RequiresPostSnapshotBind)
                {
                    pendingPropertyOverrides.Add((field.ValueNode, fieldKvp.Value));
                    continue;
                }

                field.ValueNode.DeserializeFromJson(fieldKvp.Value, jsonOptions);
                var valToApply = field.ValueNode is AssetPropertyNode assetNode ? assetNode.AssetGuid : field.ValueNode.Value;
                volumeProp.SetFieldState(accessor.SettingsType, field.Name, true, valToApply);
            }

            touchedNodes.Add((node, rvdcNode));
        }

        foreach (var (node, comp) in touchedNodes)
        {
            ComponentReflectionAccessor.TryAddOrSet(em, node.EcsEntity, comp);
        }
    }

    private static IAssetEntry? AcquirePrefabEntry(Guid prefabGuid, EditorSyncBridge bridge, Func<Guid, IAssetEntry?>? resolver, out bool releaseAfterUse)
    {
        releaseAfterUse = false;
        if (prefabGuid == Guid.Empty)
        {
            return null;
        }

        if (resolver != null)
        {
            var custom = resolver(prefabGuid);
            if (custom != null)
            {
                return custom;
            }
        }

        if (bridge.AssetManager != null && bridge.AssetManager.TryResolveAsset(prefabGuid, out var entry))
        {
            releaseAfterUse = true;
            return entry;
        }

        return null;
    }

    private static bool HasEntityRefProperty(ComponentNode compNode)
    {
        for (var i = 0; i < compNode.Properties.Count; i++)
        {
            if (compNode.Properties[i].RequiresPostSnapshotBind)
            {
                return true;
            }
        }

        return false;
    }

    private static List<EntityNode> OrderEntitiesForSerialization(IReadOnlyList<EntityNode> entities)
    {
        var byId = new Dictionary<int, EntityNode>(entities.Count);
        var children = new Dictionary<int, List<EntityNode>>();
        var roots = new List<EntityNode>();

        for (var i = 0; i < entities.Count; i++)
        {
            byId[entities[i].Id] = entities[i];
        }

        for (var i = 0; i < entities.Count; i++)
        {
            var node = entities[i];
            if (node.ParentId.HasValue && byId.ContainsKey(node.ParentId.Value))
            {
                if (!children.TryGetValue(node.ParentId.Value, out var list))
                {
                    list = new List<EntityNode>();
                    children[node.ParentId.Value] = list;
                }

                list.Add(node);
            }
            else
            {
                roots.Add(node);
            }
        }

        var ordered = new List<EntityNode>(entities.Count);
        for (var i = 0; i < roots.Count; i++)
        {
            AppendPreOrder(roots[i], children, ordered);
        }

        return ordered;
    }

    private static void AppendPreOrder(EntityNode node, Dictionary<int, List<EntityNode>> children, List<EntityNode> output)
    {
        output.Add(node);
        if (children.TryGetValue(node.Id, out var list))
        {
            for (var i = 0; i < list.Count; i++)
            {
                AppendPreOrder(list[i], children, output);
            }
        }
    }

    private static bool IsTransientComponent(Type componentType)
    {
        return componentType.GetCustomAttribute<TransientComponentAttribute>() != null;
    }

    private static Type? ResolveComponentType(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return null;
        }

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        lock (s_typeCacheLock)
        {
            if (s_cachedAssemblyCount != assemblies.Length)
            {
                s_componentTypeCache.Clear();
                s_cachedAssemblyCount = assemblies.Length;
            }

            if (s_componentTypeCache.TryGetValue(fullName, out var cached))
            {
                return cached;
            }

            Type? resolved = null;
            for (var i = 0; i < assemblies.Length; i++)
            {
                resolved = assemblies[i].GetType(fullName, throwOnError: false);
                if (resolved != null)
                {
                    break;
                }
            }

            s_componentTypeCache[fullName] = resolved;
            return resolved;
        }
    }
}
