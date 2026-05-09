using Ghost.Entities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Core.SceneGraph;

public sealed partial class EntityNode : SceneGraphNode
{
    public Entity Entity
    {
        get;
    }

    public EntityNode(World world, Entity entity, string name)
        : base(world, name)
    {
        Entity = entity;
    }

    public override IconSource? CreateIcon()
    {
        return new FontIconSource
        {
            Glyph = "\uF158"
        };
    }

    public override UIElement? CreateHeader()
    {
        return null;
    }

    public override UIElement? CreateInspector()
    {
        throw new NotImplementedException();
    }
}
