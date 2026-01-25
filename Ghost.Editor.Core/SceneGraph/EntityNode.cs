using Ghost.Entities;
using System.Collections.ObjectModel;

namespace Ghost.Editor.Core.SceneGraph;

/// <summary>
/// Represents an Entity node in the editor hierarchy.
/// Contains editor-only metadata like name and selection state.
/// References the actual entity data in the ECS world via EntityId.
/// </summary>
public class EntityNode
{
    public string Name { get; set; }
    public Entity EntityId { get; private set; }
    
    /// <summary>
    /// File-local ID within the scene (used for serialization).
    /// Only set when loaded from a scene file; may be -1 if not yet assigned.
    /// </summary>
    public int FileLocalId { get; set; } = -1;

    /// <summary>
    /// Child entity nodes (parent-child relationships in hierarchy).
    /// </summary>
    public ObservableCollection<EntityNode> Children { get; }

    /// <summary>
    /// Reference to parent entity node, if any.
    /// </summary>
    public EntityNode? ParentNode { get; set; }

    /// <summary>
    /// Whether this node is expanded in the editor UI.
    /// </summary>
    public bool IsExpanded { get; set; }

    /// <summary>
    /// Whether this node is selected in the editor UI.
    /// </summary>
    public bool IsSelected { get; set; }

    public EntityNode(string name, Entity entityId)
    {
        Name = name;
        EntityId = entityId;
        Children = new ObservableCollection<EntityNode>();
        IsExpanded = false;
        IsSelected = false;
    }

    /// <summary>
    /// Finds a child entity node recursively by its global entity ID.
    /// </summary>
    public EntityNode? FindRecursive(Entity entityId)
    {
        foreach (var child in Children)
        {
            if (child.EntityId == entityId)
                return child;

            var found = child.FindRecursive(entityId);
            if (found != null)
                return found;
        }

        return null;
    }

    /// <summary>
    /// Gets the depth of this node in the hierarchy.
    /// Root nodes have depth 0.
    /// </summary>
    public int GetDepth()
    {
        int depth = 0;
        var current = ParentNode;
        while (current != null)
        {
            depth++;
            current = current.ParentNode;
        }
        return depth;
    }

    /// <summary>
    /// Gets all descendant nodes in breadth-first order.
    /// </summary>
    public IEnumerable<EntityNode> GetAllDescendants()
    {
        var queue = new Queue<EntityNode>();
        queue.Enqueue(this);

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            foreach (var child in node.Children)
            {
                yield return child;
                queue.Enqueue(child);
            }
        }
    }

    public override string ToString() => $"Entity: {Name} (ID: {EntityId})";
}
