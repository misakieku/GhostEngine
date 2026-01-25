using Ghost.Engine.Core;
using Ghost.Entities;
using System.Collections.ObjectModel;

namespace Ghost.Editor.Core.SceneGraph;

/// <summary>
/// Represents a scene node in the editor scene graph hierarchy.
/// Contains editor-only metadata like display name and root entities.
/// </summary>
public class SceneNode
{
    private string _name;
    private readonly ObservableCollection<EntityNode> _rootEntities;

    /// <summary>
    /// Gets or sets the runtime scene this node represents.
    /// </summary>
    public Scene Scene { get; set; }

    /// <summary>
    /// Gets or sets the display name for this scene in the editor.
    /// This is NOT stored in runtime data.
    /// </summary>
    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            OnNameChanged?.Invoke(this);
        }
    }

    /// <summary>
    /// Gets or sets the file path where this scene is saved.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Gets or sets whether this scene is currently loaded in the editor.
    /// </summary>
    public bool IsLoaded { get; set; }

    /// <summary>
    /// Gets or sets whether this scene has unsaved changes.
    /// </summary>
    public bool IsDirty { get; set; }

    /// <summary>
    /// Gets the collection of root entity nodes in this scene.
    /// </summary>
    public ObservableCollection<EntityNode> RootEntities => _rootEntities;

    /// <summary>
    /// Event raised when the name property changes.
    /// </summary>
    public event Action<SceneNode>? OnNameChanged;

    /// <summary>
    /// Event raised when root entities collection changes.
    /// </summary>
    public event Action<SceneNode>? OnRootEntitiesChanged;

    public SceneNode(Scene scene, string name = "New Scene")
    {
        Scene = scene;
        _name = name;
        _rootEntities = [];
        _rootEntities.CollectionChanged += (s, e) => OnRootEntitiesChanged?.Invoke(this);
    }

    /// <summary>
    /// Adds a root entity node to this scene.
    /// </summary>
    /// <param name="entityNode">The entity node to add.</param>
    public void AddRootEntity(EntityNode entityNode)
    {
        // Remove from previous parent if any
        if (entityNode.Parent != null)
        {
            entityNode.Parent.RemoveChild(entityNode);
        }

        entityNode.Parent = null;
        entityNode.OwnerScene = this;
        _rootEntities.Add(entityNode);
    }

    /// <summary>
    /// Removes a root entity node from this scene.
    /// </summary>
    /// <param name="entityNode">The entity node to remove.</param>
    /// <returns>True if the entity was removed, false otherwise.</returns>
    public bool RemoveRootEntity(EntityNode entityNode)
    {
        if (_rootEntities.Remove(entityNode))
        {
            entityNode.OwnerScene = null;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets all entity nodes in this scene (root and descendants) in depth-first order.
    /// </summary>
    /// <returns>An enumerable of all entity nodes in the scene.</returns>
    public IEnumerable<EntityNode> GetAllEntities()
    {
        foreach (var root in _rootEntities)
        {
            yield return root;

            foreach (var descendant in root.GetAllDescendants())
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// Finds an entity node by its entity reference.
    /// </summary>
    /// <param name="entity">The entity to search for.</param>
    /// <returns>The entity node if found, null otherwise.</returns>
    public EntityNode? FindNode(Entity entity)
    {
        foreach (var root in _rootEntities)
        {
            var found = root.FindNode(entity);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>
    /// Marks this scene as dirty (has unsaved changes).
    /// </summary>
    public void MarkDirty()
    {
        IsDirty = true;
    }

    public override string ToString()
    {
        return $"{Name} (Scene ID: {Scene.ID}, Entities: {_rootEntities.Count})";
    }
}
