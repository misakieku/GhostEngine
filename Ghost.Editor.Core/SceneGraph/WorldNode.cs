using Ghost.Editor.Core.AssetHandle;
using Ghost.Editor.Core.Inspector;
using Ghost.Editor.Core.Resources;
using Ghost.Editor.Core.Serializer;
using Ghost.Engine.Components;
using Ghost.Entities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Text.Json.Serialization;

namespace Ghost.Editor.Core.SceneGraph;

[JsonConverter(typeof(WorldNodeSerializer))]
public partial class WorldNode : SceneGraphNode, IEquatable<WorldNode>
{
    private World _world;
    private Dictionary<Entity, EntityNode> _entityNodeLookup = new();

    public World World => _world;
    public Dictionary<Entity, EntityNode> EntityNodeLookup => _entityNodeLookup;

    public override SceneGraphNodeType NodeType => SceneGraphNodeType.Scene;

    public WorldNode(World world, string name)
    {
        _world = world;
        Name = name;
    }

    internal WorldNode()
    {
        _world = World.Create();
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

        var hc = _world.EntityManager.GetComponent<Hierarchy>(entity);
        var child = hc.ValueRO.firstChild;

        while (child != Entity.Invalid)
        {
            node.AddChild(BuildNodeRecursive(child));
            var childHC = _world.EntityManager.GetComponent<Hierarchy>(child);
            child = childHC.ValueRO.nextSibling;
        }

        return node;
    }

    private void BuildGraph()
    {
        foreach (var (entity, hierarchy) in _world.Query<Hierarchy>())
        {
            if (hierarchy.ValueRO.parent == Entity.Invalid)
            {
                var node = BuildNodeRecursive(entity);
                AddChild(node);
            }
        }
    }

    public Task LoadAsync()
    {
        return Task.Run(BuildGraph);
    }

    public void Unload()
    {
        _world.Dispose();
        _world = null!;

        Children?.Clear();
        _entityNodeLookup.Clear();
    }

    public override string ToString()
    {
        return $"WorldNode: {Name} (World ID: {_world.ID})";
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_world, Name);
    }

    public override bool Equals(object? obj)
    {
        return obj is WorldNode other && Equals(other);
    }

    public bool Equals(WorldNode? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return _world.Equals(other._world) && Name == other.Name;
    }

    public static bool operator ==(WorldNode? left, WorldNode? right)
    {
        if (left is null)
        {
            return right is null;
        }
        return left.Equals(right);
    }

    public static bool operator !=(WorldNode? left, WorldNode? right)
    {
        return !(left == right);
    }
}

public partial class WorldNode : IInspectable
{
    public IconSource? Icon => EditorIconSource.scene_24;

    [AssetOpenHandler(FileExtensions.SCENE_FILE_EXTENSION)]
    public static async void Open(string path)
    {
        await EditorWorldManager.LoadWorld(path);
    }

    public UIElement? HeaderContent => null;

    public UIElement? InspectorContent => null;
}