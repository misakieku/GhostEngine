using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using Ghost.Editor.Core.Controls.Internal.Docking;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;

namespace Ghost.Editor.View.Controls;

/// <summary>
/// A control that renders a docking layout tree.
/// </summary>
[TemplatePart(Name = PART_ROOT_GRID, Type = typeof(Grid))]
public sealed partial class DockLayout : Control
{
    private const string PART_ROOT_GRID = "PART_RootGrid";

    public DockLayout()
    {
        DefaultStyleKey = typeof(DockLayout);
    }

    public DockGroupNode? Root
    {
        get => (DockGroupNode?)GetValue(RootProperty);
        set => SetValue(RootProperty, value);
    }

    public static readonly DependencyProperty RootProperty =
        DependencyProperty.Register("Root", typeof(DockGroupNode), typeof(DockLayout), new PropertyMetadata(null, OnRootChanged));

    private static void OnRootChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockLayout layout)
        {
            if (e.OldValue is DockGroupNode oldRoot)
            {
                layout.UnsubscribeFromNode(oldRoot);
            }

            if (e.NewValue is DockGroupNode newRoot)
            {
                layout.SubscribeToNode(newRoot);
            }

            layout.RenderTree();
        }
    }

    private void SubscribeToNode(DockNode node)
    {
        node.PropertyChanged += OnNodePropertyChanged;

        if (node is DockGroupNode groupNode)
        {
            ((INotifyCollectionChanged)groupNode.Children).CollectionChanged += OnChildrenCollectionChanged;
            foreach (var child in groupNode.Children)
            {
                SubscribeToNode(child);
            }
        }
    }

    private void UnsubscribeFromNode(DockNode node)
    {
        node.PropertyChanged -= OnNodePropertyChanged;

        if (node is DockGroupNode groupNode)
        {
            ((INotifyCollectionChanged)groupNode.Children).CollectionChanged -= OnChildrenCollectionChanged;
            foreach (var child in groupNode.Children)
            {
                UnsubscribeFromNode(child);
            }
        }
    }

    private void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Re-render on relevant property changes (e.g. Orientation)
        RenderTree();
    }

    private void OnChildrenCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Handle subscriptions for new/removed children
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

        RenderTree();
    }

    private void RenderTree()
    {
        if (GetTemplateChild(PART_ROOT_GRID) is Grid rootGrid)
        {
            rootGrid.Children.Clear();
            if (Root != null)
            {
                var ui = CreateUIForNode(Root);
                rootGrid.Children.Add(ui);
            }
        }
    }

    private UIElement CreateUIForNode(DockNode node)
    {
        if (node is DockGroupNode groupNode)
        {
            // Simple visualizer for now, full grid logic in next step
            var grid = new Grid();
            foreach (var child in groupNode.Children)
            {
                grid.Children.Add(CreateUIForNode(child));
            }
            return grid;
        }
        else if (node is DockPanelNode panelNode)
        {
            var tabView = new Ghost.Editor.Controls.NavigationTabView 
            { 
                TabItemsSource = panelNode.Items,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            // Bind selection state
            tabView.SetBinding(Selector.SelectedIndexProperty, new Binding
            {
                Source = panelNode,
                Path = new PropertyPath(nameof(DockPanelNode.SelectedIndex)),
                Mode = BindingMode.TwoWay
            });

            tabView.SetBinding(Selector.SelectedItemProperty, new Binding
            {
                Source = panelNode,
                Path = new PropertyPath(nameof(DockPanelNode.SelectedItem)),
                Mode = BindingMode.TwoWay
            });

            return tabView;
        }

        Debug.Fail($"Unsupported node type: {node.GetType().Name}");
        return new Grid(); // Fallback
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        RenderTree();
    }
}
