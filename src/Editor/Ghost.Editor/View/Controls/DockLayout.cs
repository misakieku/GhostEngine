using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using Ghost.Editor.Core.Controls.Internal.Docking;
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
            // Remove from current tree
            _sourceNode.Items.Remove(_draggedItem);
            DockMutationEngine.CleanupEmptyNodes(_sourceNode, Root);

            // Create new window
            var newWindow = new Ghost.Editor.View.Windows.DockWindow(_draggedItem);
            newWindow.Activate();

            ClearDragOperationState();
        }
    }

    private object? _draggedItem;
    private DockPanelNode? _sourceNode;
    private DockPosition _currentDropPosition = DockPosition.None;

    private void TabView_TabDragStarting(Microsoft.UI.Xaml.Controls.TabView sender, Microsoft.UI.Xaml.Controls.TabViewTabDragStartingEventArgs args)
    {
        _draggedItem = args.Item;
        _sourceNode = sender.Tag as DockPanelNode;
        args.Data.Properties.Add(DRAG_PROPERTY_DOCK_TAB, _draggedItem); // Identify our drag
    }

    private void TabView_DragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Properties.ContainsKey(DRAG_PROPERTY_DOCK_TAB) && sender is FrameworkElement targetElement)
        {
            e.AcceptedOperation = global::Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;

            var position = e.GetPosition(targetElement);
            var newPosition = DockMath.CalculateDockPosition(targetElement.ActualWidth, targetElement.ActualHeight, position.X, position.Y, DROP_EDGE_THRESHOLD);

            if (newPosition != _currentDropPosition)
            {
                _currentDropPosition = newPosition;
                UpdateDropOverlay(targetElement, _currentDropPosition);
            }
        }
    }

    private void TabView_DragLeave(object sender, DragEventArgs e)
    {
        ClearOverlayState();
    }

    private void ClearOverlayState()
    {
        if (_dropTargetOverlay != null)
        {
            _dropTargetOverlay.Visibility = Visibility.Collapsed;
        }
        _currentDropPosition = DockPosition.None;
    }

    private void ClearDragOperationState()
    {
        ClearOverlayState();
        _draggedItem = null;
        _sourceNode = null;
    }

    private void UpdateDropOverlay(FrameworkElement targetElement, DockPosition position)
    {
        if (_dropTargetOverlay == null) return;
        if (position == DockPosition.None)
        {
            _dropTargetOverlay.Visibility = Visibility.Collapsed;
            return;
        }

        var transform = targetElement.TransformToVisual(this);
        var bounds = transform.TransformBounds(new global::Windows.Foundation.Rect(0, 0, targetElement.ActualWidth, targetElement.ActualHeight));

        _dropTargetOverlay.Visibility = Visibility.Visible;
        _dropTargetOverlay.Width = double.NaN;
        _dropTargetOverlay.Height = double.NaN;

        switch (position)
        {
            case DockPosition.Center:
                _dropTargetOverlay.Margin = new Thickness(bounds.Left, bounds.Top, ActualWidth - bounds.Right, ActualHeight - bounds.Bottom);
                break;
            case DockPosition.Left:
                _dropTargetOverlay.Margin = new Thickness(bounds.Left, bounds.Top, ActualWidth - (bounds.Left + bounds.Width / 2), ActualHeight - bounds.Bottom);
                break;
            case DockPosition.Right:
                _dropTargetOverlay.Margin = new Thickness(bounds.Left + bounds.Width / 2, bounds.Top, ActualWidth - bounds.Right, ActualHeight - bounds.Bottom);
                break;
            case DockPosition.Top:
                _dropTargetOverlay.Margin = new Thickness(bounds.Left, bounds.Top, ActualWidth - bounds.Right, ActualHeight - (bounds.Top + bounds.Height / 2));
                break;
            case DockPosition.Bottom:
                _dropTargetOverlay.Margin = new Thickness(bounds.Left, bounds.Top + bounds.Height / 2, ActualWidth - bounds.Right, ActualHeight - bounds.Bottom);
                break;
        }
    }

    private void TabView_Drop(object sender, DragEventArgs e)
    {
        if (_dropTargetOverlay != null) _dropTargetOverlay.Visibility = Visibility.Collapsed;

        if (_draggedItem == null || _sourceNode == null || !(sender is FrameworkElement targetElement) || !(targetElement.Tag is DockPanelNode targetNode))
        {
            ClearDragOperationState();
            return;
        }

        if (_currentDropPosition == DockPosition.None)
        {
            ClearDragOperationState();
            return;
        }

        if (_sourceNode == targetNode && _currentDropPosition == DockPosition.Center)
        {
            ClearDragOperationState();
            return; // Reordering within same tab is handled natively by TabView
        }

        if (Root == null)
        {
            ClearDragOperationState();
            return;
        }

        // 1. Execute mutation
        if (DockMutationEngine.TryApplyDropMutation(Root, targetNode, _sourceNode, _draggedItem, _currentDropPosition))
        {
            DockMutationEngine.CleanupEmptyNodes(_sourceNode);
        }

        ClearDragOperationState();
    }


    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _dropTargetOverlay = GetTemplateChild(PART_DROP_TARGET_OVERLAY) as FrameworkElement;
        RenderTree();
    }
}
