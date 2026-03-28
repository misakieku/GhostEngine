using Ghost.Core;
using Ghost.Editor.Core.Controls.Internal.Docking;
using Ghost.Editor.View.Controls;
using WinUIEx;

namespace Ghost.Editor.View.Windows;

internal sealed partial class DockWindow : WindowEx
{
    private long _rootPropertyToken;
    private DockGroupNode? _currentRoot;

    public DockWindow(object initialTabContent)
    {
        InitializeComponent();
        
        // Setup initial single panel layout
        var rootGroup = new DockGroupNode();
        var panel = new DockPanelNode();
        panel.Items.Add(initialTabContent);
        rootGroup.AddChild(panel);
        
        PART_DockLayout.Root = rootGroup;
        PART_DockLayout.TabTornOff += OnTabTornOff;

        RegisterCloseHandler();
        
        this.Closed += (s, e) => 
        {
            if (_rootPropertyToken != 0)
            {
                PART_DockLayout.UnregisterPropertyChangedCallback(DockLayout.RootProperty, _rootPropertyToken);
                _rootPropertyToken = 0;
            }
            UnsubscribeFromRoot(_currentRoot);
        };
    }

    private void RegisterCloseHandler()
    {
        _rootPropertyToken = PART_DockLayout.RegisterPropertyChangedCallback(DockLayout.RootProperty, (s, dp) =>
        {
            UnsubscribeFromRoot(_currentRoot);
            SubscribeToRoot(PART_DockLayout.Root);
        });

        SubscribeToRoot(PART_DockLayout.Root);
    }

    private void OnRootChildrenChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (PART_DockLayout.Root?.Children.Count == 0)
        {
            this.Close();
        }
    }

    private void SubscribeToRoot(DockGroupNode? root)
    {
        _currentRoot = root;
        if (_currentRoot != null)
        {
            ((System.Collections.Specialized.INotifyCollectionChanged)_currentRoot.Children).CollectionChanged += OnRootChildrenChanged;
        }
    }

    private void UnsubscribeFromRoot(DockGroupNode? root)
    {
        if (root != null)
        {
            ((System.Collections.Specialized.INotifyCollectionChanged)root.Children).CollectionChanged -= OnRootChildrenChanged;
        }
    }

    private void OnTabTornOff(object? sender, TabTornOffEventArgs e)
    {
        App.CreateAndShowDockWindow(e.TabContent);
    }
}
