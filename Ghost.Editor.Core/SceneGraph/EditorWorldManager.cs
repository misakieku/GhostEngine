using Ghost.Engine.Core;
using Ghost.Entities;
using System.Collections.ObjectModel;

namespace Ghost.Editor.Core.SceneGraph;

/// <summary>
/// Manages the editor world and scene graph hierarchy.
/// Provides functionality to load/unload scenes and maintain the editor-side scene graph.
/// </summary>
public class EditorWorldManager
{
    private readonly World _editorWorld;
    private readonly SceneManager _sceneManager;
    private readonly ObservableCollection<SceneNode> _loadedScenes;
    private readonly Dictionary<short, SceneNode> _sceneIdToNode;
    private readonly Dictionary<Entity, EntityNode> _entityToNode;

    /// <summary>
    /// Gets the editor world instance.
    /// </summary>
    public World EditorWorld => _editorWorld;

    /// <summary>
    /// Gets the runtime scene manager.
    /// </summary>
    public SceneManager SceneManager => _sceneManager;

    /// <summary>
    /// Gets the collection of loaded scenes in the editor.
    /// </summary>
    public ReadOnlyObservableCollection<SceneNode> LoadedScenes { get; }

    /// <summary>
    /// Event raised when a scene is loaded.
    /// </summary>
    public event Action<SceneNode>? OnSceneLoaded;

    /// <summary>
    /// Event raised when a scene is unloaded.
    /// </summary>
    public event Action<SceneNode>? OnSceneUnloaded;

    /// <summary>
    /// Event raised when an entity node is created.
    /// </summary>
    public event Action<EntityNode>? OnEntityNodeCreated;

    /// <summary>
    /// Event raised when an entity node is destroyed.
    /// </summary>
    public event Action<EntityNode>? OnEntityNodeDestroyed;

    public EditorWorldManager(World editorWorld, SceneManager sceneManager)
    {
        _editorWorld = editorWorld;
        _sceneManager = sceneManager;
        _loadedScenes = [];
        _sceneIdToNode = [];
        _entityToNode = [];

        LoadedScenes = new ReadOnlyObservableCollection<SceneNode>(_loadedScenes);
    }

    /// <summary>
    /// Creates a new empty scene in the editor.
    /// </summary>
    /// <param name="name">The name of the scene.</param>
    /// <returns>The created scene node.</returns>
    public SceneNode CreateNewScene(string name = "New Scene")
    {
        var runtimeScene = _sceneManager.CreateScene();
        var sceneNode = new SceneNode(runtimeScene, name)
        {
            IsLoaded = true
        };

        _loadedScenes.Add(sceneNode);
        _sceneIdToNode[runtimeScene.ID] = sceneNode;

        OnSceneLoaded?.Invoke(sceneNode);

        return sceneNode;
    }

    /// <summary>
    /// Unloads a scene from the editor.
    /// </summary>
    /// <param name="sceneNode">The scene to unload.</param>
    public void UnloadScene(SceneNode sceneNode)
    {
        if (!_loadedScenes.Contains(sceneNode))
        {
            return;
        }

        // Remove all entity nodes from tracking
        foreach (var entityNode in sceneNode.GetAllEntities().ToList())
        {
            _entityToNode.Remove(entityNode.Entity);
            OnEntityNodeDestroyed?.Invoke(entityNode);
        }

        // Unload runtime scene
        _sceneManager.UnloadScene(sceneNode.Scene);

        // Remove from loaded scenes
        _loadedScenes.Remove(sceneNode);
        _sceneIdToNode.Remove(sceneNode.Scene.ID);

        sceneNode.IsLoaded = false;

        OnSceneUnloaded?.Invoke(sceneNode);
    }

    /// <summary>
    /// Creates an entity in the specified scene.
    /// </summary>
    /// <param name="sceneNode">The scene to create the entity in.</param>
    /// <param name="name">The display name of the entity.</param>
    /// <param name="parent">Optional parent entity node.</param>
    /// <returns>The created entity node.</returns>
    public EntityNode CreateEntity(SceneNode sceneNode, string name = "Entity", EntityNode? parent = null)
    {
        // Create runtime entity with SceneID component
        var entity = _editorWorld.EntityManager.CreateEntity();
        _editorWorld.EntityManager.AddComponent(entity, new Components.SceneID { id = sceneNode.Scene.ID });

        // Create entity node
        var entityNode = new EntityNode(entity, name);

        // Add to scene graph
        if (parent != null)
        {
            parent.AddChild(entityNode);

            // Add Hierarchy component
            _editorWorld.EntityManager.AddComponent(entity, new Components.Hierarchy
            {
                parent = parent.Entity,
                firstChild = Entity.Invalid,
                nextSibling = Entity.Invalid
            });
        }
        else
        {
            sceneNode.AddRootEntity(entityNode);

            // Add root hierarchy component
            _editorWorld.EntityManager.AddComponent(entity, Components.Hierarchy.Root);
        }

        // Track entity node
        _entityToNode[entity] = entityNode;

        OnEntityNodeCreated?.Invoke(entityNode);

        return entityNode;
    }

    /// <summary>
    /// Destroys an entity and its node from the scene.
    /// </summary>
    /// <param name="entityNode">The entity node to destroy.</param>
    public void DestroyEntity(EntityNode entityNode)
    {
        // Remove from parent or scene root
        if (entityNode.Parent != null)
        {
            entityNode.Parent.RemoveChild(entityNode);
        }
        else if (entityNode.OwnerScene != null)
        {
            entityNode.OwnerScene.RemoveRootEntity(entityNode);
        }

        // Destroy all children recursively
        var childrenCopy = entityNode.Children.ToList();
        foreach (var child in childrenCopy)
        {
            DestroyEntity(child);
        }

        // Destroy runtime entity
        _editorWorld.EntityManager.DestroyEntity(entityNode.Entity);

        // Remove from tracking
        _entityToNode.Remove(entityNode.Entity);

        OnEntityNodeDestroyed?.Invoke(entityNode);
    }

    /// <summary>
    /// Gets the scene node for a runtime scene ID.
    /// </summary>
    /// <param name="sceneId">The scene ID.</param>
    /// <returns>The scene node if found, null otherwise.</returns>
    public SceneNode? GetSceneNode(short sceneId)
    {
        _sceneIdToNode.TryGetValue(sceneId, out var sceneNode);
        return sceneNode;
    }

    /// <summary>
    /// Gets the entity node for a runtime entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>The entity node if found, null otherwise.</returns>
    public EntityNode? GetEntityNode(Entity entity)
    {
        _entityToNode.TryGetValue(entity, out var entityNode);
        return entityNode;
    }

    /// <summary>
    /// Rebuilds the scene graph from the current world state.
    /// Useful after loading a scene from disk.
    /// </summary>
    /// <param name="sceneNode">The scene node to rebuild.</param>
    public void RebuildSceneGraph(SceneNode sceneNode)
    {
        // Clear existing nodes
        sceneNode.RootEntities.Clear();

        // Build query for entities in this scene
        var builder = new QueryBuilder();
        builder.WithAll([ComponentTypeID<Components.SceneID>.Value, ComponentTypeID<Components.Hierarchy>.Value]);
        var queryID = builder.Build(_editorWorld);
        ref var query = ref _editorWorld.ComponentManager.GetEntityQueryReference(queryID);

        // First pass: Create all entity nodes
        var entityNodes = new Dictionary<Entity, EntityNode>();

        foreach (var chunk in query.GetChunkIterator())
        {
            var entities = chunk.GetEntities();
            var sceneIDs = chunk.GetComponentData<Components.SceneID>();

            for (int i = 0; i < chunk.Count; i++)
            {
                if (sceneIDs[i].id == sceneNode.Scene.ID)
                {
                    var entity = entities[i];

                    // Try to get existing node name or use default
                    var name = _entityToNode.TryGetValue(entity, out var existing)
                        ? existing.Name
                        : "Entity";

                    var entityNode = new EntityNode(entity, name);
                    entityNodes[entity] = entityNode;
                    _entityToNode[entity] = entityNode;
                }
            }
        }

        // Second pass: Build hierarchy
        foreach (var chunk in query.GetChunkIterator())
        {
            var entities = chunk.GetEntities();
            var sceneIDs = chunk.GetComponentData<Components.SceneID>();
            var hierarchies = chunk.GetComponentData<Components.Hierarchy>();

            for (int i = 0; i < chunk.Count; i++)
            {
                if (sceneIDs[i].id == sceneNode.Scene.ID)
                {
                    var entity = entities[i];
                    var hierarchy = hierarchies[i];
                    var entityNode = entityNodes[entity];

                    if (hierarchy.parent.IsValid && entityNodes.TryGetValue(hierarchy.parent, out var parentNode))
                    {
                        parentNode.AddChild(entityNode);
                    }
                    else
                    {
                        sceneNode.AddRootEntity(entityNode);
                    }
                }
            }
        }
    }
}
