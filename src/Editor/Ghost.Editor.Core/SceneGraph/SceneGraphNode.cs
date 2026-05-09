using CommunityToolkit.Mvvm.ComponentModel;
using Ghost.Editor.Core.Contracts;
using Ghost.Entities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace Ghost.Editor.Core.SceneGraph;

public abstract partial class SceneGraphNode : ObservableObject, IInspectable
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

    public abstract IconSource? CreateIcon();
    public abstract UIElement? CreateHeader();
    public abstract UIElement? CreateInspector();
}
