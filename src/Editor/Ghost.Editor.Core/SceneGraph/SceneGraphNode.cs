using Ghost.Editor.Core.Contracts;
using Ghost.Editor.Core.Services;
using Ghost.Entities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace Ghost.Editor.Core.SceneGraph;

public abstract partial class SceneGraphNode : GhostObject, IInspectable
{
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

    public virtual SceneNode? GetOwningSceneNode()
    {
        return null;
    }

    public override void Modify()
    {
        base.Modify(); // Marks this node dirty via base GhostObject logic

        var sceneNode = GetOwningSceneNode();
        if (sceneNode != null)
        {
            var worldService = EditorApplication.GetService<IEditorWorldService>();
            var sceneAsset = worldService.GetAssetForScene(sceneNode.Scene.ID);
            if (sceneAsset != null)
            {
                EditorApplication.GetService<IDirtyTrackerService>().MarkDirty(sceneAsset);
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
