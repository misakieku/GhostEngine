using Ghost.Entities;

namespace Ghost.Editor.Core.SceneGraph;

public sealed partial class EntityNode : SceneGraphNode
{
    private readonly Entity _entity;

    public Entity Entity => _entity;
}
