using Ghost.Editor.Core.Selection;
using Ghost.Editor.Core.Serialization;
using Ghost.Editor.Core.Sync;
using Ghost.Engine.Components;
using Ghost.Engine.Utilities;
using Ghost.Entities;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Editor.Core.Commands;

/// <summary>
/// Undoable command that instantiates an <see cref="AssetType.EntityPrefab"/> by asset <see cref="Guid"/>
/// into the editor scene as a linked prefab instance (<see cref="PrefabLinkInfo"/>).
/// </summary>
public sealed class InstantiatePrefabCommand : IEditorCommand
{
    private readonly Guid _prefabAssetGuid;
    private readonly string _displayName;
    private readonly float3 _position;
    private readonly quaternion _rotation;
    private readonly float3 _scale;
    private readonly int? _parentNodeId;
    private readonly EditorSyncBridge _bridge;
    private readonly EditorSelection _selection;
    private readonly Guid _instanceGuid = Guid.NewGuid();
    private readonly Dictionary<int, Guid> _allocatedNodeGuids = new();
    private readonly List<EntityNode> _spawnedEntityNodes = new();

    private Entity _spawnedRoot = Entity.Invalid;
    private object? _previousSelection;

    public ushort? TargetSceneId => _targetSceneId;

    public string Name => $"Instantiate {_displayName}";

    private readonly ushort _targetSceneId;

    public InstantiatePrefabCommand(Guid prefabAssetGuid, string displayName, float3 position, quaternion rotation, float3 scale, int? parentNodeId = null, EditorSyncBridge? bridge = null, EditorSelection? selection = null, ushort? targetSceneId = null)
    {
        _prefabAssetGuid = prefabAssetGuid;
        _displayName = string.IsNullOrWhiteSpace(displayName) ? "Prefab" : displayName;
        _position = position;
        _rotation = rotation;
        _scale = scale;
        _parentNodeId = parentNodeId;
        _bridge = bridge ?? EditorSyncBridge.Instance;
        _selection = selection ?? EditorSelection.Instance;
        _targetSceneId = targetSceneId ?? _bridge.ActiveSceneId;
    }

    public void Execute()
    {
        _previousSelection = _selection.ActiveObject;
        _spawnedEntityNodes.Clear();

        var world = _bridge.World;
        var assetManager = _bridge.AssetManager;
        if (world == null || assetManager == null || !assetManager.TryResolveAsset(_prefabAssetGuid, out var prefabAsset))
        {
            return;
        }

        try
        {
            var parentEcs = _parentNodeId.HasValue ? _bridge.ResolveEcsEntity(_parentNodeId.Value) : Entity.Invalid;
            var opts = new PrefabSpawnOptions
            {
                Parent = parentEcs,
                ApplyTransform = true,
                InstanceGuidOverride = _instanceGuid,
                NodeGuidOverrides = _allocatedNodeGuids.Count > 0 ? _allocatedNodeGuids : null,
                RootName = _displayName,
                TargetSceneId = _targetSceneId
            };

            if (!EditorPrefabUtility.TrySpawnLinkedPrefab(world, prefabAsset, _bridge, _position, _rotation, _scale, out var spawned, opts))
            {
                return;
            }

            _spawnedRoot = spawned[0].Entity;
            for (var i = 0; i < spawned.Count; i++)
            {
                _allocatedNodeGuids[spawned[i].Link.TemplateEntityIndex] = spawned[i].EditorGuid;
            }

            var createdNodes = SceneSerializer.CreatePrefabEntityNodes(world, prefabAsset, spawned, _targetSceneId, _bridge);
            _spawnedEntityNodes.AddRange(createdNodes);
            _bridge.MutateEntities(entities => entities.AddRange(createdNodes));

            var em = world.EntityManager;
            for (var i = 0; i < createdNodes.Count; i++)
            {
                var ecs = createdNodes[i].EcsEntity;
                if (em.HasComponent<LocalTransform>(ecs) || em.HasComponent<LocalToWorld>(ecs))
                {
                    _bridge.DirtyTracker.MarkTransformDirty(ecs);
                }
            }

            if (createdNodes.Count > 0)
            {
                _selection.Select(createdNodes[0]);
            }

            _bridge.SetSceneDirty(_targetSceneId, true);
        }
        finally
        {
            prefabAsset.Release();
        }
    }

    public void Undo()
    {
        var world = _bridge.World;
        if (world != null && _spawnedRoot.IsValid && world.EntityManager.Exists(_spawnedRoot))
        {
            ClearSubtreeMetadata(world, _spawnedRoot);
            HierarchyUtility.DestroyEntityWithChildren(world, _spawnedRoot);
        }

        _spawnedRoot = Entity.Invalid;

        if (_spawnedEntityNodes.Count > 0)
        {
            var removedSet = new HashSet<EntityNode>(_spawnedEntityNodes);
            _bridge.MutateEntities(entities =>
            {
                for (var i = entities.Count - 1; i >= 0; i--)
                {
                    if (removedSet.Contains(entities[i]))
                    {
                        entities.RemoveAt(i);
                    }
                }
            });
            _spawnedEntityNodes.Clear();
        }

        _bridge.SetSceneDirty(_targetSceneId, true);
        _selection.Select(_previousSelection);
    }

    public void Redo() => Execute();

    private void ClearSubtreeMetadata(World world, Entity entity)
    {
        if (!entity.IsValid || !world.EntityManager.Exists(entity))
        {
            return;
        }

        if (world.EntityManager.HasComponent<Hierarchy>(entity))
        {
            var child = world.EntityManager.GetComponent<Hierarchy>(entity).firstChild;
            while (child.IsValid && world.EntityManager.Exists(child))
            {
                var next = world.EntityManager.HasComponent<Hierarchy>(child)
                    ? world.EntityManager.GetComponent<Hierarchy>(child).nextSibling
                    : Entity.Invalid;
                ClearSubtreeMetadata(world, child);
                child = next;
            }
        }

        _bridge.RemoveEntityMetadata(entity);
    }
}
