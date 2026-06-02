using Ghost.Editor.Core.Contracts;
using Ghost.Engine.Core;
using Ghost.Entities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Core.SceneGraph;

public sealed partial class SceneNode : SceneGraphNode
{
    public Scene Scene
    {
        get;
    }

    internal SceneNode(World world, Scene scene, string name)
        : base(world, name)
    {
        Scene = scene;
    }

    public override SceneNode? GetOwningSceneNode() => this;

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

    public override IInspectorModel CreateInspectorModel()
    {
        return null!;
    }
}