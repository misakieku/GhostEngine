using Ghost.Editor.Core.Contracts;
using Ghost.Editor.Core.SceneGraph;
using Ghost.Editor.Core.Services;
using Ghost.Core;
using Ghost.Engine;
using Ghost.Entities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Ghost.Editor.Views.Controls;

public sealed partial class Hierarchy : UserControl
{
    private readonly IInspectorService _inspectorService;
    private readonly IEditorWorldService _worldService;
    private readonly SceneGraphSyncService _syncService;
    private EntityNode? _draggedNode;

    public Hierarchy()
    {
        InitializeComponent();

        _inspectorService = App.GetService<IInspectorService>();

        // We resolve SceneGraphSyncService here to force the DI container to instantiate it. 
        // This ensures the singleton hooks into EditorWorldService events and starts populating RootNodes.
        _syncService = App.GetService<SceneGraphSyncService>();

        _worldService = App.GetService<IEditorWorldService>();

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

    private void OnTreeViewDragItemsCompleted(TreeView sender, TreeViewDragItemsCompletedEventArgs args)
    {
        var entityNode = args.Items.Count > 0 ? args.Items[0] as EntityNode : _draggedNode;
        _draggedNode = null;

        if (entityNode == null)
        {
            return;
        }

        if (args.DropResult != global::Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move)
        {
            RebuildSceneGraphFromECS();
            return;
        }

        if (args.NewParentItem is not SceneGraphNode newParent)
        {
            RebuildSceneGraphFromECS();
            return;
        }

        if (newParent == entityNode)
        {
            RebuildSceneGraphFromECS();
            return;
        }

        var result = Error.None;

        if (newParent is EntityNode parentEntityNode)
        {
            if (HierarchyUtility.IsAncestor(_worldService.EditorWorld, parentEntityNode.Entity, entityNode.Entity))
            {
                RebuildSceneGraphFromECS();
                return;
            }

            var currentParent = GetCurrentParent(entityNode);
            if (currentParent == parentEntityNode.Entity)
            {
                RebuildSceneGraphFromECS();
                return;
            }

            result = _worldService.SetParent(entityNode.Entity, parentEntityNode.Entity);
        }
        else if (newParent is SceneNode sceneNode)
        {
            var currentParent = GetCurrentParent(entityNode);
            var sceneChanged = _worldService.GetEntitySceneID(entityNode.Entity) != sceneNode.Scene.ID;
            if (!currentParent.IsValid && !sceneChanged)
            {
                RebuildSceneGraphFromECS();
                return;
            }

            if (currentParent.IsValid)
            {
                result = _worldService.RemoveParent(entityNode.Entity);
                if (result != Error.None)
                {
                    RebuildSceneGraphFromECS();
                    return;
                }
            }

            if (sceneChanged)
            {
                _worldService.ChangeEntityScene(entityNode.Entity, sceneNode.Scene.ID);
            }
        }
        else
        {
            RebuildSceneGraphFromECS();
            return;
        }

        if (result != Error.None)
        {
            RebuildSceneGraphFromECS();
        }
    }

    private void OnCreateEntityClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem menuItem && menuItem.DataContext is SceneNode sceneNode)
        {
            _worldService.CreateEntity("Entity", sceneNode.Scene.ID);
        }
    }

    private async void OnSaveSceneClick(object sender, RoutedEventArgs e)
    {
        var assetRegistry = App.GetService<IAssetRegistry>();
        await assetRegistry.SaveDirtyAssetsAsync();
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

    private Entity GetCurrentParent(EntityNode entityNode)
    {
        if (!_worldService.EditorWorld.EntityManager.HasComponent<Ghost.Engine.Components.Hierarchy>(entityNode.Entity))
        {
            return Entity.Invalid;
        }

        return _worldService.EditorWorld.EntityManager.GetComponent<Ghost.Engine.Components.Hierarchy>(entityNode.Entity).parent;
    }

    private void RebuildSceneGraphFromECS()
    {
        var names = new Dictionary<Entity, string>();
        foreach (var sceneNode in _worldService.RootNodes)
        {
            CaptureEntityNames(sceneNode, names);
        }

        _worldService.RebuildSceneGraph(names);
    }

    private static void CaptureEntityNames(SceneGraphNode node, Dictionary<Entity, string> names)
    {
        if (node is EntityNode entityNode)
        {
            names[entityNode.Entity] = entityNode.Name;
        }

        foreach (var child in node.Children)
        {
            CaptureEntityNames(child, names);
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        SceneTreeView.ItemInvoked -= OnTreeViewItemInvoked;
        SceneTreeView.SelectionChanged -= OnTreeViewSelectionChanged;
        Unloaded -= OnUnloaded;
    }
}
