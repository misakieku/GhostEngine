using Ghost.Entities;

namespace Ghost.Editor.SceneGraph;

public partial class EntityNode : SceneGraphNode
{
    private readonly Entity _entity;

    public Entity Entity => _entity;
    public override SceneGraphNodeType NodeType => SceneGraphNodeType.Entity;

    public EntityNode(Entity entity, string name)
    {
        _entity = entity;
        Name = name;
    }

    internal EntityNode()
    {
    }
}