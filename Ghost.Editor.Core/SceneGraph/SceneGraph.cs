using System.Collections.ObjectModel;
using Ghost.Entities;

namespace Ghost.Editor.Core.SceneGraph;

/// <summary>
/// SceneGraph is the editor's view-model over the ECS runtime data.
/// It provides a hierarchical representation of scenes and entities for UI rendering.
/// 
/// This is editor-only and does not exist at runtime.
/// </summary>
public class SceneGraph
{
    /// <summary>
    /// All scenes currently loaded in the editor world.
    /// </summary>
    public ObservableCollection<SceneNode> Scenes { get; }

    /// <summary>
    /// Reference to the editor world containing ECS data.
    /// </summary>
    private readonly World _editorWorld;

    /// <summary>
    /// Cache: map from global entity ID to entity node for O(1) lookups.
    /// </summary>
    private Dictionary<Entity, EntityNode> _entityNodeMap;

    /// <summary>
    /// Cache: map from scene ID to scene node for O(1) lookups.
    /// </summary>
    private Dictionary<short, SceneNode> _sceneNodeMap;

    public SceneGraph(World editorWorld)
    {
        _editorWorld = editorWorld ?? throw new ArgumentNullException(nameof(editorWorld));
        Scenes = new ObservableCollection<SceneNode>();
        _entityNodeMap = new Dictionary<Entity, EntityNode>();
        _sceneNodeMap = new Dictionary<short, SceneNode>();
    }

    /// <summary>
    /// Adds a scene to the scene graph.
    /// </summary>
    public SceneNode AddScene(string name, short sceneId, Guid? sceneGuid = null)
    {
        if (_sceneNodeMap.ContainsKey(sceneId))
        {
            throw new InvalidOperationException($"Scene with ID {sceneId} already exists in the graph.");
        }

        var sceneNode = new SceneNode(name, sceneId, sceneGuid);
        Scenes.Add(sceneNode);
        _sceneNodeMap[sceneId] = sceneNode;

        return sceneNode;
    }

    /// <summary>
    /// Removes a scene from the scene graph.
    /// </summary>
    public bool RemoveScene(short sceneId)
    {
        if (!_sceneNodeMap.TryGetValue(sceneId, out var sceneNode))
        {
            return false;
        }

        // Remove all entity nodes in this scene
        foreach (var entityNode in sceneNode.GetAllDescendants())
        {
            _entityNodeMap.Remove(entityNode.EntityId);
        }

        Scenes.Remove(sceneNode);
        _sceneNodeMap.Remove(sceneId);

        return true;
    }

    /// <summary>
    /// Gets a scene node by its scene ID.
    /// </summary>
    public SceneNode? GetSceneNode(short sceneId)
    {
        _sceneNodeMap.TryGetValue(sceneId, out var sceneNode);
        return sceneNode;
    }

    /// <summary>
    /// Adds an entity node to a scene.
    /// If parentEntityId is valid, adds it as a child of that entity.
    /// Otherwise, adds it as a root entity in the scene.
    /// </summary>
    public EntityNode AddEntity(short sceneId, string name, Entity entityId, Entity parentEntityId = default)
    {
        var sceneNode = GetSceneNode(sceneId);
        if (sceneNode == null)
        {
            throw new InvalidOperationException($"Scene with ID {sceneId} not found.");
        }

        var entityNode = new EntityNode(name, entityId);
        _entityNodeMap[entityId] = entityNode;

        // Add as child of parent or as root in scene
        if (parentEntityId.IsValid && _entityNodeMap.TryGetValue(parentEntityId, out var parentNode))
        {
            parentNode.Children.Add(entityNode);
            entityNode.ParentNode = parentNode;
        }
        else
        {
            sceneNode.Children.Add(entityNode);
            entityNode.ParentNode = null;
        }

        return entityNode;
    }

    /// <summary>
    /// Removes an entity node from the graph.
    /// Also removes all its children recursively.
    /// </summary>
    public bool RemoveEntity(Entity entityId)
    {
        if (!_entityNodeMap.TryGetValue(entityId, out var entityNode))
        {
            return false;
        }

        // Remove all descendants
        foreach (var descendant in entityNode.GetAllDescendants())
        {
            _entityNodeMap.Remove(descendant.EntityId);
        }

        // Remove from parent or scene
        if (entityNode.ParentNode != null)
        {
            entityNode.ParentNode.Children.Remove(entityNode);
        }
        else
        {
            // Find and remove from scene
            foreach (var sceneNode in Scenes)
            {
                if (sceneNode.Children.Contains(entityNode))
                {
                    sceneNode.Children.Remove(entityNode);
                    break;
                }
            }
        }

        _entityNodeMap.Remove(entityId);
        return true;
    }

    /// <summary>
    /// Gets an entity node by its global entity ID.
    /// </summary>
    public EntityNode? GetEntityNode(Entity entityId)
    {
        _entityNodeMap.TryGetValue(entityId, out var entityNode);
        return entityNode;
    }

    /// <summary>
    /// Sets the parent of an entity node.
    /// </summary>
    public void SetEntityParent(Entity childEntityId, Entity newParentEntityId)
    {
        if (!_entityNodeMap.TryGetValue(childEntityId, out var childNode))
        {
            throw new InvalidOperationException($"Entity {childEntityId} not found in scene graph.");
        }

        // Remove from current parent/scene
        if (childNode.ParentNode != null)
        {
            childNode.ParentNode.Children.Remove(childNode);
        }
        else
        {
            // Find and remove from scene
            foreach (var sceneNode in Scenes)
            {
                if (sceneNode.Children.Contains(childNode))
                {
                    sceneNode.Children.Remove(childNode);
                    break;
                }
            }
        }

        // Add to new parent
        if (newParentEntityId.IsValid && _entityNodeMap.TryGetValue(newParentEntityId, out var newParentNode))
        {
            newParentNode.Children.Add(childNode);
            childNode.ParentNode = newParentNode;
        }
        else
        {
            throw new InvalidOperationException($"New parent entity {newParentEntityId} not found.");
        }
    }

    /// <summary>
    /// Rebuilds the scene graph from the editor world's ECS data.
    /// Queries entities with SceneID and Hierarchy components.
    /// </summary>
    public void RebuildFromWorld()
    {
        Scenes.Clear();
        _entityNodeMap.Clear();
        _sceneNodeMap.Clear();

        // TODO: Query entities with SceneID and Hierarchy components
        // For now, this is a placeholder that will be implemented once we have the full integration
    }

    /// <summary>
    /// Gets all entities in a scene.
    /// </summary>
    public IEnumerable<EntityNode> GetEntitiesInScene(short sceneId)
    {
        var sceneNode = GetSceneNode(sceneId);
        if (sceneNode == null)
        {
            return Enumerable.Empty<EntityNode>();
        }

        var allEntities = new List<EntityNode>();
        foreach (var child in sceneNode.Children)
        {
            allEntities.Add(child);
            allEntities.AddRange(child.GetAllDescendants());
        }

        return allEntities;
    }
}
