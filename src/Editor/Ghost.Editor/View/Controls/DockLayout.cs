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
        if (Root != null)
        {
            UnsubscribeFromNode(Root);
        }
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

            if (e.NewValue is DockGroupNode newRoot && layout.IsLoaded)
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
        // Filter to relevant property names
        if (e.PropertyName == nameof(DockGroupNode.Orientation) ||
            e.PropertyName == nameof(DockPanelNode.SelectedIndex) ||
            e.PropertyName == nameof(DockPanelNode.SelectedItem))
        {
            RenderTree();
        }
    }

    private void OnChildrenCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            // On Reset, we don't know what was removed. 
            // Simplest is to unsubscribe from everything and resubscribe from Root.
            if (Root != null)
            {
                UnsubscribeFromNode(Root);
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
                    Grid.SetColumn(childUI as FrameworkElement, i);
                }
                else
                {
                    Grid.SetRow(childUI as FrameworkElement, i);
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
