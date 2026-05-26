using Ghost.Editor.Core.Contracts;
using Ghost.Editor.Core.SceneGraph;
using Ghost.Editor.Core.Services;
using Ghost.Engine;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Ghost.Editor.Views.Controls;

public sealed partial class Hierarchy : UserControl
{
    private readonly IInspectorService _inspectorService;
    private readonly SceneGraphSyncService _syncService;
    private readonly EditorWorldService _worldService;
    private EntityNode? _draggedNode;

    public Hierarchy()
    {
        InitializeComponent();

        _inspectorService = App.GetService<IInspectorService>();

        // We resolve SceneGraphSyncService here to force the DI container to instantiate it. 
        // This ensures the singleton hooks into EditorWorldService events and starts populating RootNodes.
        _syncService = App.GetService<SceneGraphSyncService>();

        _worldService = App.GetService<EditorWorldService>();

        SceneTreeView.ItemsSource = _worldService.RootNodes;

        SceneTreeView.ItemInvoked += OnTreeViewItemInvoked;
        SceneTreeView.SelectionChanged += OnTreeViewSelectionChanged;

        Unloaded += OnUnloaded;
    }

    private void OnTreeViewItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        if (args.InvokedItem is IInspectable inspectable)
        {
            _inspectorService.SetSelected(inspectable, this);
        }
    }

    private void OnTreeViewSelectionChanged(object sender, TreeViewSelectionChangedEventArgs args)
    {
    }

    private void OnTreeViewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == global::Windows.System.VirtualKey.Delete)
        {
            if (SceneTreeView.SelectedItem is EntityNode entityNode)
            {
                _worldService.DestroyEntity(entityNode.Entity);
                e.Handled = true;
            }
        }
    }

    private void OnTreeViewDragItemsStarting(TreeView sender, TreeViewDragItemsStartingEventArgs args)
    {
        if (args.Items.Count > 0 && args.Items[0] is EntityNode entityNode)
        {
            _draggedNode = entityNode;
        }
        else
        {
            _draggedNode = null;
        }
    }

    private void OnTreeViewDragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = global::Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;

        if (_draggedNode == null)
        {
            return;
        }

        var targetItem = GetAncestorTreeViewItem(e.OriginalSource as DependencyObject);
        if (targetItem == null)
        {
            return;
        }

        var targetNode = targetItem.DataContext as SceneGraphNode;
        if (targetNode == null)
        {
            return;
        }

        // 1. Can't drag onto itself
        if (_draggedNode == targetNode)
        {
            return;
        }

        // 2. Can't drag onto a child of itself (cycle checking)
        if (targetNode is EntityNode targetEntityNode)
        {
            if (HierarchyUtility.IsAncestor(_worldService.EditorWorld, targetEntityNode.Entity, _draggedNode.Entity))
            {
                return;
            }
        }

        e.AcceptedOperation = global::Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
    }

    private void OnTreeViewDrop(object sender, DragEventArgs e)
    {
        if (_draggedNode == null)
        {
            return;
        }

        var targetItem = GetAncestorTreeViewItem(e.OriginalSource as DependencyObject);
        if (targetItem == null)
        {
            return;
        }

        var targetNode = targetItem.DataContext as SceneGraphNode;
        if (targetNode == null)
        {
            return;
        }

        if (_draggedNode == targetNode)
        {
            return;
        }

        if (targetNode is EntityNode targetEntityNode)
        {
            if (!HierarchyUtility.IsAncestor(_worldService.EditorWorld, targetEntityNode.Entity, _draggedNode.Entity))
            {
                _worldService.SetParent(_draggedNode.Entity, targetEntityNode.Entity);
            }
        }
        else if (targetNode is SceneNode sceneNode)
        {
            _worldService.RemoveParent(_draggedNode.Entity);
            _worldService.ChangeEntityScene(_draggedNode.Entity, sceneNode.Scene.ID);
        }
    }

    private void OnCreateEntityClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem menuItem && menuItem.DataContext is SceneNode sceneNode)
        {
            _worldService.CreateEntity("Entity", sceneNode.Scene.ID);
        }
    }

    private void OnCreateChildClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem menuItem && menuItem.DataContext is EntityNode entityNode)
        {
            var sceneID = _worldService.GetEntitySceneID(entityNode.Entity);
            if (sceneID != Engine.Core.Scene.INVALID_ID)
            {
                _worldService.CreateEntity("Entity", sceneID, parent: entityNode.Entity);
            }
        }
    }

    private void OnDeleteEntityClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem menuItem && menuItem.DataContext is EntityNode entityNode)
        {
            _worldService.DestroyEntity(entityNode.Entity);
        }
    }

    private TreeViewItem? GetAncestorTreeViewItem(DependencyObject? current)
    {
        while (current != null)
        {
            if (current is TreeViewItem item)
            {
                return item;
            }
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        SceneTreeView.ItemInvoked -= OnTreeViewItemInvoked;
        SceneTreeView.SelectionChanged -= OnTreeViewSelectionChanged;
        Unloaded -= OnUnloaded;
    }
}
