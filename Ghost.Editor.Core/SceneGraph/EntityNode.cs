using Ghost.Entities;
using System.Collections.ObjectModel;

namespace Ghost.Editor.Core.SceneGraph;

/// <summary>
/// Represents an entity node in the editor scene graph hierarchy.
/// Contains editor-only metadata like display name and hierarchy information.
/// </summary>
public class EntityNode
{
    private string _name;
    private readonly ObservableCollection<EntityNode> _children;

    /// <summary>
    /// Gets or sets the entity this node represents.
    /// </summary>
    public Entity Entity { get; set; }

    /// <summary>
    /// Gets or sets the display name for this entity in the editor.
    /// This is NOT stored in runtime components.
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
    /// Gets the parent node of this entity node.
    /// </summary>
    public EntityNode? Parent { get; internal set; }

    /// <summary>
    /// Gets the scene node that contains this entity.
    /// </summary>
    public SceneNode? OwnerScene { get; internal set; }

    /// <summary>
    /// Gets the collection of child entity nodes.
    /// </summary>
    public ObservableCollection<EntityNode> Children => _children;

    /// <summary>
    /// Event raised when the name property changes.
    /// </summary>
    public event Action<EntityNode>? OnNameChanged;

    /// <summary>
    /// Event raised when children collection changes.
    /// </summary>
    public event Action<EntityNode>? OnChildrenChanged;

    public EntityNode(Entity entity, string name = "Entity")
    {
        Entity = entity;
        _name = name;
        _children = [];
        _children.CollectionChanged += (s, e) => OnChildrenChanged?.Invoke(this);
    }

    /// <summary>
    /// Adds a child entity node to this node.
    /// </summary>
    /// <param name="child">The child node to add.</param>
    public void AddChild(EntityNode child)
    {
        if (child.Parent != null)
        {
            child.Parent.RemoveChild(child);
        }

        child.Parent = this;
        child.OwnerScene = OwnerScene;
        _children.Add(child);
    }

    /// <summary>
    /// Removes a child entity node from this node.
    /// </summary>
    /// <param name="child">The child node to remove.</param>
    /// <returns>True if the child was removed, false otherwise.</returns>
    public bool RemoveChild(EntityNode child)
    {
        if (_children.Remove(child))
        {
            child.Parent = null;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets all descendants of this node (children, grandchildren, etc.) in depth-first order.
    /// </summary>
    /// <returns>An enumerable of all descendant nodes.</returns>
    public IEnumerable<EntityNode> GetAllDescendants()
    {
        foreach (var child in _children)
        {
            yield return child;

            foreach (var descendant in child.GetAllDescendants())
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
        if (Entity.Equals(entity))
        {
            return this;
        }

        foreach (var child in _children)
        {
            var found = child.FindNode(entity);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    public override string ToString()
    {
        return $"{Name} (Entity: {Entity})";
    }
}
