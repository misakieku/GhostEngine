using System.Collections.Specialized;
using Ghost.Editor.Core.Controls.Internal.Docking;

namespace Ghost.Editor.View.Controls;

public sealed partial class DockLayout
{
    private readonly HashSet<DockNode> _subscribedNodes = new();

    private void SubscribeToNode(DockNode node)
    {
        if (!_subscribedNodes.Add(node))
        {
            return;
        }

        node.PropertyChanged += OnNodePropertyChanged;

        if (node is DockGroupNode groupNode)
        {
            ((INotifyCollectionChanged)groupNode.Children).CollectionChanged += OnChildrenCollectionChanged;
            groupNode.Sizes.CollectionChanged += OnSizesCollectionChanged;
            foreach (var child in groupNode.Children)
            {
                SubscribeToNode(child);
            }
        }
    }

    private void UnsubscribeFromNode(DockNode node)
    {
        if (!_subscribedNodes.Remove(node))
        {
            return;
        }

        node.PropertyChanged -= OnNodePropertyChanged;

        if (node is DockGroupNode groupNode)
        {
            ((INotifyCollectionChanged)groupNode.Children).CollectionChanged -= OnChildrenCollectionChanged;
            groupNode.Sizes.CollectionChanged -= OnSizesCollectionChanged;
            foreach (var child in groupNode.Children)
            {
                UnsubscribeFromNode(child);
            }
        }
    }

    private void UnsubscribeFromAll()
    {
        // Copy to array to avoid modification during enumeration
        var nodes = _subscribedNodes.ToArray();
        foreach (var node in nodes)
        {
            node.PropertyChanged -= OnNodePropertyChanged;
            if (node is DockGroupNode groupNode)
            {
                ((INotifyCollectionChanged)groupNode.Children).CollectionChanged -= OnChildrenCollectionChanged;
                groupNode.Sizes.CollectionChanged -= OnSizesCollectionChanged;
            }
        }
        _subscribedNodes.Clear();
    }

    private void OnSizesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RenderTree();
    }

    private void OnNodePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Filter to structural property names
        if (e.PropertyName == nameof(DockGroupNode.Orientation))
        {
            RenderTree();
        }
    }

    private void OnChildrenCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            // On Reset, we don't know what was removed. 
            // We must unsubscribe from everything and resubscribe from Root to avoid leaks.
            UnsubscribeFromAll();
            if (Root != null)
            {
                SubscribeToNode(Root);
            }
        }
        else
        {
            if (e.OldItems != null)
            {
                foreach (DockNode oldNode in e.OldItems)
                {
                    UnsubscribeFromNode(oldNode);
                }
            }

            if (e.NewItems != null)
            {
                foreach (DockNode newNode in e.NewItems)
                {
                    SubscribeToNode(newNode);
                }
            }
        }

        RenderTree();
    }
}
