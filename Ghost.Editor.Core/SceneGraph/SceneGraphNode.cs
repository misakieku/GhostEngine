using CommunityToolkit.Mvvm.ComponentModel;
using Ghost.Editor.Core.Inspector;
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

    public ObservableCollection<SceneGraphNode> Children
    {
        get;
    } = new();

    public abstract IconSource? CreateIcon();
    public abstract UIElement? CreateHeader();
    public abstract UIElement? CreateInspector();

    public abstract DataTemplate GetSceneHierarchyTemplate();
}
