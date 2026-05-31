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

    public SceneGraphNode? Parent
    {
        get; internal set;
    }

    public ObservableCollection<SceneGraphNode> Children
    {
        get;
    } = new();

    protected SceneGraphNode(World world, string name)
    {
        World = world;
        Name = name;
        Children.CollectionChanged += OnChildrenChanged;
    }

    private void OnChildrenChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (SceneGraphNode oldItem in e.OldItems)
            {
                if (oldItem.Parent == this)
                {
                    oldItem.Parent = null;
                }
            }
        }
        if (e.NewItems != null)
        {
            foreach (SceneGraphNode newItem in e.NewItems)
            {
                newItem.Parent = this;
            }
        }
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
