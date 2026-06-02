using Ghost.Editor.Core.SceneGraph;
using Ghost.Engine.Components;
using Ghost.Engine.Core;
using Ghost.Entities;

namespace Ghost.Editor.Core.Services;

internal class SceneGraphSyncService : IDisposable
{
    private readonly IEditorWorldService _worldService;
    private readonly Dictionary<Entity, EntityNode> _nodeMap = new();

    public SceneGraphSyncService(IEditorWorldService worldService)
    {
        _worldService = worldService;

        _worldService.EntityCreated += OnEntityCreated;
        _worldService.EntityDestroyed += OnEntityDestroyed;
        _worldService.EntityParentChanged += OnEntityParentChanged;
        _worldService.EntityNameChanged += OnEntityNameChanged;
        _worldService.SceneGraphRebuilt += OnSceneGraphRebuilt;

        // Initialize node map from current root nodes
        OnSceneGraphRebuilt();
    }

    public bool TryGetNode(Entity entity, out EntityNode node)
    {
        return _nodeMap.TryGetValue(entity, out node!);
    }

    public void Dispose()
    {
        _worldService.EntityCreated -= OnEntityCreated;
        _worldService.EntityDestroyed -= OnEntityDestroyed;
        _worldService.EntityParentChanged -= OnEntityParentChanged;
        _worldService.EntityNameChanged -= OnEntityNameChanged;
        _worldService.SceneGraphRebuilt -= OnSceneGraphRebuilt;
    }

    private void OnSceneGraphRebuilt()
    {
        _nodeMap.Clear();
        foreach (var sceneNode in _worldService.RootNodes)
        {
            PopulateNodeMapRecursive(sceneNode);
        }
    }

    private void PopulateNodeMapRecursive(SceneGraphNode node)
    {
        if (node is EntityNode entityNode)
        {
            _nodeMap[entityNode.Entity] = entityNode;
        }

        foreach (var child in node.Children)
        {
            PopulateNodeMapRecursive(child);
        }
    }

    private void OnEntityCreated(Entity entity, string name, ushort sceneID)
    {
        if (_nodeMap.ContainsKey(entity))
        {
            return;
        }

        // By default, add to the scene's root collection
        var sceneNode = FindOrCreateSceneNode(sceneID);

        var node = new EntityNode(_worldService.EditorWorld, entity, name, sceneNode);
        _nodeMap[entity] = node;

        sceneNode.Children.Add(node);
    }

    private void OnEntityDestroyed(Entity entity)
    {
        if (!_nodeMap.TryGetValue(entity, out var node))
        {
            return;
        }

        // Recursively remove from node map
        RemoveNodeAndDescendantsRecursive(node);

        // Remove from its parent's Children collection (or from RootNodes if it was a scene's root entity)
        RemoveNodeFromParent(node);
    }

    private void RemoveNodeFromParent(EntityNode node)
    {
        foreach (var sceneNode in _worldService.RootNodes)
        {
            if (sceneNode.Children.Remove(node))
            {
                return;
            }

            if (RemoveNodeFromChildrenRecursive(sceneNode.Children, node))
            {
                return;
            }
        }
    }

    private static bool RemoveNodeFromChildrenRecursive(System.Collections.ObjectModel.ObservableCollection<SceneGraphNode> children, EntityNode target)
    {
        foreach (var child in children)
        {
            if (child.Children.Remove(target))
            {
                return true;
            }

            if (RemoveNodeFromChildrenRecursive(child.Children, target))
            {
                return true;
            }
        }

        return false;
    }

    private void RemoveNodeAndDescendantsRecursive(EntityNode node)
    {
        _nodeMap.Remove(node.Entity);
        foreach (var child in node.Children)
        {
            if (child is EntityNode childEntityNode)
            {
                RemoveNodeAndDescendantsRecursive(childEntityNode);
            }
        }
    }

    private void OnEntityParentChanged(Entity child, Entity oldParent, Entity newParent)
    {
        if (!_nodeMap.TryGetValue(child, out var childNode))
        {
            return;
        }

        // Remove from the old parent collection (wherever it currently is)
        RemoveNodeFromParent(childNode);

        // Add to the new parent collection (prepend at index 0 to match HierarchyUtility firstChild behavior)
        if (newParent.IsValid && _nodeMap.TryGetValue(newParent, out var newParentNode))
        {
            newParentNode.Children.Insert(0, childNode);
        }
        else
        {
            // Add to the scene's root collection
            if (_worldService.EditorWorld.EntityManager.HasComponent<SceneID>(child))
            {
                var sceneID = _worldService.GetEntitySceneID(child);
                if (sceneID != Scene.INVALID_ID)
                {
                    var sceneNode = FindOrCreateSceneNode(sceneID);
                    sceneNode.Children.Insert(0, childNode);
                }
            }
        }
    }

    private void OnEntityNameChanged(Entity entity, string newName)
    {
        if (_nodeMap.TryGetValue(entity, out var node))
        {
            node.Name = newName;
        }
    }

    private SceneNode FindOrCreateSceneNode(ushort sceneID)
    {
        foreach (var existing in _worldService.RootNodes)
        {
            if (existing.Scene.ID == sceneID)
            {
                return existing;
            }
        }

        var sceneName = $"NewScene ({sceneID})";
        var newSceneNode = new SceneNode(_worldService.EditorWorld, new Scene(sceneID), sceneName);
        _worldService.RootNodes.Add(newSceneNode);
        return newSceneNode;
    }
}
