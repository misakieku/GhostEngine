using Ghost.Editor.Core.Selection;
using Ghost.Editor.Core.Sync;
using Ghost.Engine.Streaming;
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

    private Entity _spawnedRoot = Entity.Invalid;
    private object? _previousSelection;

    public string Name => $"Instantiate {_displayName}";

    public InstantiatePrefabCommand(Guid prefabAssetGuid, string displayName, float3 position, quaternion rotation, float3 scale, int? parentNodeId = null, EditorSyncBridge? bridge = null, EditorSelection? selection = null)
    {
        _prefabAssetGuid = prefabAssetGuid;
        _displayName = string.IsNullOrWhiteSpace(displayName) ? "Prefab" : displayName;
        _position = position;
        _rotation = rotation;
        _scale = scale;
        _parentNodeId = parentNodeId;
        _bridge = bridge ?? EditorSyncBridge.Instance;
        _selection = selection ?? EditorSelection.Instance;
    }

    public void Execute()
    {
        _previousSelection = _selection.ActiveObject;
        _bridge.EnqueueWorldAction(world =>
        {
            var assetManager = _bridge.AssetManager;
            if (assetManager == null || !assetManager.TryResolveAsset(_prefabAssetGuid, out var prefabAsset))
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
                    RootName = _displayName
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

                _bridge.HasUnsavedSceneChanges = true;
            }
            finally
            {
                if (prefabAsset.State == AssetState.Ready)
                {
                    prefabAsset.Release();
                }
            }
        });
    }

    public void Undo()
    {
        var rootToDestroy = _spawnedRoot;
        _bridge.EnqueueWorldAction(world =>
        {
            if (rootToDestroy.IsValid && world.EntityManager.Exists(rootToDestroy))
            {
                ClearSubtreeMetadata(world, rootToDestroy);
                HierarchyUtility.DestroyEntityWithChildren(world, rootToDestroy);
            }

            _spawnedRoot = Entity.Invalid;
            _bridge.HasUnsavedSceneChanges = true;
        });

        _selection.Select(_previousSelection);
    }

    public void Redo() => Execute();

    private void ClearSubtreeMetadata(World world, Entity entity)
    {
        if (!entity.IsValid || !world.EntityManager.Exists(entity))
        {
            return;
        }

        if (world.EntityManager.HasComponent<Ghost.Engine.Components.Hierarchy>(entity))
        {
            var child = world.EntityManager.GetComponent<Ghost.Engine.Components.Hierarchy>(entity).firstChild;
            while (child.IsValid && world.EntityManager.Exists(child))
            {
                var next = world.EntityManager.HasComponent<Ghost.Engine.Components.Hierarchy>(child)
                    ? world.EntityManager.GetComponent<Ghost.Engine.Components.Hierarchy>(child).nextSibling
                    : Entity.Invalid;
                ClearSubtreeMetadata(world, child);
                child = next;
            }
        }

        _bridge.RemoveEntityMetadata(entity);
    }
}
