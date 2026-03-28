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

    private readonly HashSet<DockNode> _subscribedNodes = new();

    public DockLayout()
    {
        DefaultStyleKey = typeof(DockLayout);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Root != null)
        {
            SubscribeToNode(Root);
        }
        RenderTree();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        UnsubscribeFromAll();
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
            layout.UnsubscribeFromAll();

            if (e.NewValue is DockGroupNode newRoot && layout.IsLoaded)
            {
                layout.SubscribeToNode(newRoot);
            }

            layout.RenderTree();
        }
    }

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
            }
        }
        _subscribedNodes.Clear();
    }

    private void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
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
            var grid = new Grid();
            var children = groupNode.Children;

            for (int i = 0; i < children.Count; i++)
            {
                if (groupNode.Orientation == Orientation.Horizontal)
                {
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                }
                else
                {
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                }

                var childUI = CreateUIForNode(children[i]);
                if (groupNode.Orientation == Orientation.Horizontal)
                {
                    Grid.SetColumn((FrameworkElement)childUI, i);
                }
                else
                {
                    Grid.SetRow((FrameworkElement)childUI, i);
                }
                grid.Children.Add(childUI);
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

            // Bind selection state using TabView DPs
            tabView.SetBinding(TabView.SelectedIndexProperty, new Binding
            {
                Source = panelNode,
                Path = new PropertyPath(nameof(DockPanelNode.SelectedIndex)),
                Mode = BindingMode.TwoWay
            });

            tabView.SetBinding(TabView.SelectedItemProperty, new Binding
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
