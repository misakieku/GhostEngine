using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Ghost.Editor.Core.SceneGraph;

public enum SceneGraphNodeType
{
    Scene,
    Entity,
}

public abstract partial class SceneGraphNode : ObservableObject
{
    public ObservableCollection<SceneGraphNode>? Children
    {
        get;
        private set;
    }

    [ObservableProperty]
    public partial string Name
    {
        get;
        set;
    }

    public abstract SceneGraphNodeType NodeType
    {
        get;
    }

    public int ChildCount => Children?.Count ?? 0;

    public virtual void AddChild(SceneGraphNode child)
    {
        Children ??= new();
        Children.Add(child);
    }

    public virtual bool RemoveChild(SceneGraphNode child)
    {
        return Children?.Remove(child) ?? false;
    }

    public SceneGraphNode GetChild(int index)
    {
        if (Children == null || index < 0 || index >= Children.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
        }

        return Children[index];
    }
}
