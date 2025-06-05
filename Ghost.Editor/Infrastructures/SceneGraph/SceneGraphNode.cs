using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Ghost.Editor.Infrastructures.SceneGraph;

public abstract partial class SceneGraphNode : ObservableObject
{
    public enum NodeType
    {
        Scene,
        Entity,
    }

    public abstract NodeType Type
    {
        get;
    }

    [ObservableProperty]
    public partial string Name
    {
        get;
        set;
    }

    // Will the new collection allocated if ui bind to this property?
    private ObservableCollection<EntityNode>? _children;
    public ObservableCollection<EntityNode> Children
    {
        get => _children ??= new();
    }
}