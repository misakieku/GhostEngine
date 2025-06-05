using Ghost.Engine.Components;
using Ghost.Entities;
using System.Collections.Generic;

namespace Ghost.Editor.Infrastructures.SceneGraph;

public partial class SceneNode : SceneGraphNode
{
    private readonly World _world;
    private Dictionary<Entity, EntityNode> _entityNodeLookup = new();

    public World World => _world;
    public Dictionary<Entity, EntityNode> EntityNodeLookup => _entityNodeLookup;

    public override NodeType Type => NodeType.Scene;

    public SceneNode(World world, string name)
    {
        _world = world;
        Name = name;
    }

    private EntityNode BuildNodeRecursive(Entity entity, World world)
    {
        // TODO: Node serialization.
        var node = new EntityNode(entity, "New Entity");
        _entityNodeLookup[entity] = node;

        var hc = world.EntityManager.GetComponent<Hierarchy>(entity);
        var child = hc.ValueRO.firstChild;

        while (child != Entity.Invalid)
        {
            node.Children.Add(BuildNodeRecursive(child, world));
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
                Children.Add(node);
            }
        }
    }

    public void Load()
    {
        BuildGraph();
    }
}