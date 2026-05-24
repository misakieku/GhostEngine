using Ghost.Engine.Core;
using Ghost.Entities;
using Ghost.Editor.Core.Contracts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Core.SceneGraph;

public sealed partial class SceneNode : SceneGraphNode
{
    public Scene Scene
    {
        get;
    }

    public SceneNode(World world, Scene scene, string name)
        : base(world, name)
    {
        Scene = scene;
    }

    public override IconSource? CreateIcon()
    {
        return new FontIconSource
        {
            Glyph = "\uF156"
        };
    }

    public override UIElement? CreateHeader()
    {
        return null;
    }

    public override InspectorDescriptor CreateInspectorDescriptor()
    {
        return null!;
    }
}