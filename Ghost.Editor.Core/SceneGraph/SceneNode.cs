using System.Collections.ObjectModel;
using Ghost.Entities;

namespace Ghost.Editor.Core.SceneGraph;

/// <summary>
/// Represents a Scene node in the editor hierarchy.
/// Contains editor-only metadata like name and display state.
/// The actual scene data (entities, components) is stored as SceneID in the runtime ECS world.
/// </summary>
public class SceneNode
{
    public string Name { get; set; }
    public short SceneId { get; private set; }
    public Guid SceneGuid { get; private set; }
    
    /// <summary>
    /// Child entity nodes belonging to this scene.
    /// </summary>
    public ObservableCollection<EntityNode> Children { get; }

    public SceneNode(string name, short sceneId, Guid? sceneGuid = null)
    {
        Name = name;
        SceneId = sceneId;
        SceneGuid = sceneGuid ?? Guid.NewGuid();
        Children = new ObservableCollection<EntityNode>();
    }

    /// <summary>
    /// Finds an entity node by its global entity ID.
    /// Searches recursively through the hierarchy.
    /// </summary>
    public EntityNode? FindEntityNode(Entity entityId)
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

    public override string ToString() => $"Scene: {Name} (ID: {SceneId})";
}
