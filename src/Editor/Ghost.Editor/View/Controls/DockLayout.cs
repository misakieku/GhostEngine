using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using Ghost.Core;
using Ghost.Editor.Core.Controls.Internal.Docking;
using Ghost.Editor.View.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace Ghost.Editor.View.Controls;

/// <summary>
/// A control that renders a docking layout tree.
/// </summary>
[TemplatePart(Name = PART_ROOT_GRID, Type = typeof(Grid))]
[TemplatePart(Name = PART_DROP_TARGET_OVERLAY, Type = typeof(FrameworkElement))]
public sealed partial class DockLayout : Control
{
    private const string PART_ROOT_GRID = "PART_RootGrid";
    private const string PART_DROP_TARGET_OVERLAY = "PART_DropTargetOverlay";
    private const string DRAG_PROPERTY_DOCK_TAB = "DockTab";
    private const double MIN_PANE_SIZE = 100;
    private const double SPLITTER_THICKNESS = 4;
    private const double DROP_EDGE_THRESHOLD = 0.25;

    private FrameworkElement? _dropTargetOverlay;
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
        DependencyProperty.Register(nameof(Root), typeof(DockGroupNode), typeof(DockLayout), new PropertyMetadata(null, OnRootChanged));

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
            return CreateGroupUI(groupNode);
        }
        else if (node is DockPanelNode panelNode)
        {
            return CreatePanelUI(panelNode);
        }

        Debug.Fail($"Unsupported node type: {node.GetType().Name}");
        return new Grid(); // Fallback
    }

    private UIElement CreateGroupUI(DockGroupNode groupNode)
    {
        var grid = new Grid();
        bool isHorizontal = groupNode.Orientation == Orientation.Horizontal;
        int childCount = groupNode.Children.Count;

        for (int i = 0; i < childCount; i++)
        {
            var childNode = groupNode.Children[i];
            var childUI = CreateUIForNode(childNode);

            if (isHorizontal)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star),
                    MinWidth = MIN_PANE_SIZE
                });
                Grid.SetColumn((FrameworkElement)childUI, i * 2);
            }
            else
            {
                grid.RowDefinitions.Add(new RowDefinition
                {
                    Height = new GridLength(1, GridUnitType.Star),
                    MinHeight = MIN_PANE_SIZE
                });
                Grid.SetRow((FrameworkElement)childUI, i * 2);
            }

            grid.Children.Add(childUI);

            // Add GridSplitter between children
            if (i < childCount - 1)
            {
                if (isHorizontal)
                {
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    var splitter = new CommunityToolkit.WinUI.Controls.GridSplitter
                    {
                        Width = SPLITTER_THICKNESS,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        ResizeDirection = CommunityToolkit.WinUI.Controls.GridSplitter.GridResizeDirection.Columns
                    };
                    Grid.SetColumn(splitter, (i * 2) + 1);
                    grid.Children.Add(splitter);
                }
                else
                {
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    var splitter = new CommunityToolkit.WinUI.Controls.GridSplitter
                    {
                        Height = SPLITTER_THICKNESS,
                        VerticalAlignment = VerticalAlignment.Center,
                        ResizeDirection = CommunityToolkit.WinUI.Controls.GridSplitter.GridResizeDirection.Rows
                    };
                    Grid.SetRow(splitter, (i * 2) + 1);
                    grid.Children.Add(splitter);
                }
            }
        }

        return grid;
    }

    private UIElement CreatePanelUI(DockPanelNode panelNode)
    {
        var tabView = new Ghost.Editor.Controls.NavigationTabView
        {
            TabItemsSource = panelNode.Items,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            CanDragTabs = true,
            AllowDrop = true,
            Tag = panelNode // Store reference to data node
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

        tabView.DragOver += TabView_DragOver;
        tabView.DragLeave += TabView_DragLeave;
        tabView.Drop += TabView_Drop;
        tabView.TabDragStarting += TabView_TabDragStarting;
        tabView.TabDroppedOutside += TabView_TabDroppedOutside;

        return tabView;
    }

    private void TabView_TabDroppedOutside(Microsoft.UI.Xaml.Controls.TabView sender, Microsoft.UI.Xaml.Controls.TabViewTabDroppedOutsideEventArgs args)
    {
        if (_sourceNode != null && _draggedItem != null)
        {
            var result = TabTearOffService.TryTearOffTab(_sourceNode.Items, _draggedItem, _sourceNode);
            
            if (result.IsSuccess)
            {
                DockMutationEngine.CleanupEmptyNodes(_sourceNode);
            }
            else
            {
                Logger.LogWarning($"Tab tear-off failed: {result.Message}");
            }

            ClearDragOperationState();
        }
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _dropTargetOverlay = GetTemplateChild(PART_DROP_TARGET_OVERLAY) as FrameworkElement;
        RenderTree();
    }
}
