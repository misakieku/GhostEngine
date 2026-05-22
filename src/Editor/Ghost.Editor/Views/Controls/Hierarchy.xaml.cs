using Ghost.Editor.Core.Contracts;
using Ghost.Editor.Core.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Views.Controls;

public sealed partial class Hierarchy : UserControl
{
    private readonly IInspectorService _inspectorService;
    private readonly SceneGraphSyncService _syncService;
    private readonly EditorWorldService _worldService;
    private DispatcherQueueTimer? _syncTimer;

    public Hierarchy()
    {
        InitializeComponent();

        _inspectorService = App.GetService<IInspectorService>();
        _syncService = App.GetService<SceneGraphSyncService>();
        _worldService = App.GetService<EditorWorldService>();

        SceneTreeView.ItemsSource = _worldService.RootNodes;

        SceneTreeView.ItemInvoked += OnTreeViewItemInvoked;
        SceneTreeView.SelectionChanged += OnTreeViewSelectionChanged;

        _syncTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _syncTimer.Interval = TimeSpan.FromMilliseconds(100);
        _syncTimer.Tick += OnSyncTick;
        _syncTimer.Start();

        Unloaded += OnUnloaded;
    }

    private void OnSyncTick(DispatcherQueueTimer sender, object args)
    {
        if (_syncService.Tick())
        {
        }
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

    private void OnUnloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _syncTimer?.Stop();
        SceneTreeView.ItemInvoked -= OnTreeViewItemInvoked;
        SceneTreeView.SelectionChanged -= OnTreeViewSelectionChanged;
        Unloaded -= OnUnloaded;
    }
}
