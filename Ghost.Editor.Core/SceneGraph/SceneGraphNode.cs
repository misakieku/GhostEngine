using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Ghost.Editor.Core.SceneGraph;

public abstract partial class SceneGraphNode : ObservableObject
{
    [ObservableProperty]
    public partial string Name
    {
        get; set;
    }

    public ObservableCollection<SceneGraphNode> Children
    {
        get;
    } = new();
}
