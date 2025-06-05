using Ghost.Entities;

namespace Ghost.Editor.Infrastructures.SceneGraph;

public partial class EntityNode : SceneGraphNode
{
    private readonly Entity _entity;

    public Entity Entity => _entity;
    public override NodeType Type => NodeType.Entity;

    public EntityNode(Entity entity, string name)
    {
        _entity = entity;
        Name = name;
    }
}