using Ghost.Editor.Serializer;
using Ghost.Engine.Components;
using Ghost.Entities;
using System.Text.Json.Serialization;

namespace Ghost.Editor.SceneGraph;

[JsonConverter(typeof(WorldNodeSerializer))]
public partial class WorldNode : SceneGraphNode
{
    private readonly World _world;
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

    private EntityNode BuildNodeRecursive(Entity entity, World world)
    {
        // TODO: Node serialization.
        if (!_entityNodeLookup.TryGetValue(entity, out var node))
        {
            node = new EntityNode(entity, "New Entity");
            _entityNodeLookup[entity] = node;
        }

        var hc = world.EntityManager.GetComponent<Hierarchy>(entity);
        var child = hc.ValueRO.firstChild;

        while (child != Entity.Invalid)
        {
            node.AddChild(BuildNodeRecursive(child, world));
            var childHC = world.EntityManager.GetComponent<Hierarchy>(child);
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
                var node = BuildNodeRecursive(entity, _world);
                AddChild(node);
            }
        }
    }

    public void Load()
    {
        BuildGraph();
    }
}