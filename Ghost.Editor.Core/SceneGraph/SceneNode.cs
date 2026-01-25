using Ghost.Editor.Core.AssetHandle;
using Ghost.Editor.Core.Inspector;
using Ghost.Editor.Core.Resources;
using Ghost.Engine.Components;
using Ghost.Engine.Core;
using Ghost.Entities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Core.SceneGraph;

public partial class SceneNode : SceneGraphNode, IEquatable<SceneNode>
{
    private Scene _scene;
    private Dictionary<Entity, EntityNode> _entityNodeLookup = new();

    public Scene Scene => _scene;
    public Dictionary<Entity, EntityNode> EntityNodeLookup => _entityNodeLookup;

    public override SceneGraphNodeType NodeType => SceneGraphNodeType.Scene;

    public SceneNode(Scene scene, string name)
    {
        _scene = scene;
        Name = name;
    }

    private void UpdateLookup(Entity key, EntityNode value)
    {
        _entityNodeLookup[key] = value;
        if (value.Children == null)
        {
            return;
        }

        foreach (var child in value.Children)
        {
            if (child is EntityNode entityChild)
            {
                UpdateLookup(entityChild.Entity, entityChild);
            }
        }
    }

    public override void AddChild(SceneGraphNode child)
    {
        if (child is not EntityNode entityNode)
        {
            throw new ArgumentException("Child must be of type EntityNode.", nameof(child));
        }

        base.AddChild(entityNode);
        UpdateLookup(entityNode.Entity, entityNode);
    }

    public override bool RemoveChild(SceneGraphNode child)
    {
        if (child is not EntityNode entityNode)
        {
            throw new ArgumentException("Child must be of type EntityNode.", nameof(child));
        }

        var result = base.RemoveChild(child);
        if (result)
        {
            _entityNodeLookup.Remove(entityNode.Entity);
        }

        return result;
    }

    private EntityNode BuildNodeRecursive(Entity entity)
    {
        if (!_entityNodeLookup.TryGetValue(entity, out var node))
        {
            node = new EntityNode(this, entity, "New Entity");
            _entityNodeLookup[entity] = node;
        }

        var hc = _scene.World.EntityManager.GetComponent<Hierarchy>(entity);
        var child = hc.firstChild;

        while (child != Entity.Invalid)
        {
            node.AddChild(BuildNodeRecursive(child));
            var childHC = _scene.World.EntityManager.GetComponent<Hierarchy>(child);
            child = childHC.nextSibling;
        }

        return node;
    }

    private void BuildGraph()
    {
        var queryID = new QueryBuilder()
            .WithAll<Hierarchy>()
            .Build(_scene.World);

        _scene.World.ComponentManager.GetEntityQueryReference(queryID).ForEach<Hierarchy>((entity, ref hierarchy) =>
        {
            if (hierarchy.parent == Entity.Invalid)
            {
                var node = BuildNodeRecursive(entity);
                AddChild(node);
            }
        });
    }

    public Task LoadAsync()
    {
        return Task.Run(BuildGraph);
    }

    public void Unload()
    {
        _scene = null!;

        Children?.Clear();
        _entityNodeLookup.Clear();
    }

    public override string ToString()
    {
        return $"WorldNode: {Name} (World ID: {_scene.ID})";
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_scene, Name);
    }

    public override bool Equals(object? obj)
    {
        return obj is SceneNode other && Equals(other);
    }

    public bool Equals(SceneNode? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return _scene.Equals(other._scene) && Name == other.Name;
    }

    public static bool operator ==(SceneNode? left, SceneNode? right)
    {
        if (left is null)
        {
            return right is null;
        }
        return left.Equals(right);
    }

    public static bool operator !=(SceneNode? left, SceneNode? right)
    {
        return !(left == right);
    }
}

public partial class SceneNode : IInspectable
{
    public IconSource? Icon => EditorIconSource.scene_24;

    [AssetOpenHandler(FileExtensions.SCENE_FILE_EXTENSION)]
    public static async void Open(string path)
    {
        await EditorSceneManager.LoadSceneAsync(path);
    }

    public UIElement? HeaderContent => null;

    public UIElement? InspectorContent => null;
}
