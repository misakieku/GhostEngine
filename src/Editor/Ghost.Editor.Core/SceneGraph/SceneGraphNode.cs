using CommunityToolkit.Mvvm.ComponentModel;
using Ghost.Core;
using Ghost.Editor.Core.Contracts;
using Ghost.Entities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace Ghost.Editor.Core.SceneGraph;

[ObservableObject]
public abstract partial class SceneGraphNode : GhostObject, IInspectable
{
    [ObservableProperty]
    public partial string Name
    {
        get; set;
    }

    public World World
    {
        get;
    }

    public ObservableCollection<SceneGraphNode> Children
    {
        get;
    } = new();

    protected SceneGraphNode(World world, string name)
    {
        World = world;
        Name = name;
    }

    public override void SerializeState(BinaryWriter writer)
    {
        writer.Write(Name);
    }

    public override void DeserializeState(BinaryReader reader)
    {
        Name = reader.ReadString();
    }

    public virtual IconSource? CreateIcon()
    {
        return null;
    }

    public virtual UIElement? CreateHeader()
    {
        return null;
    }

    public abstract IInspectorModel CreateInspectorModel();
}
