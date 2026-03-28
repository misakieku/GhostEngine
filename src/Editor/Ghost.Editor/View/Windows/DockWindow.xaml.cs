using Ghost.Core;
using Ghost.Editor.Core.Controls.Internal.Docking;
using Ghost.Editor.View.Controls;
using WinUIEx;

namespace Ghost.Editor.View.Windows;

internal sealed partial class DockWindow : WindowEx
{
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
    }

    private void RegisterCloseHandler()
    {
        // Subscribe to Root changes to ensure we always track the current tree
        var rootProperty = DockLayout.RootProperty;
        
        void OnRootChildrenChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (PART_DockLayout.Root?.Children.Count == 0)
            {
                this.Close();
            }
        }

        void SubscribeToRoot(DockGroupNode? root)
        {
            if (root != null)
            {
                ((System.Collections.Specialized.INotifyCollectionChanged)root.Children).CollectionChanged += OnRootChildrenChanged;
            }
        }

        void UnsubscribeFromRoot(DockGroupNode? root)
        {
            if (root != null)
            {
                ((System.Collections.Specialized.INotifyCollectionChanged)root.Children).CollectionChanged -= OnRootChildrenChanged;
            }
        }

        PART_DockLayout.RegisterPropertyChangedCallback(DockLayout.RootProperty, (s, dp) =>
        {
            // This is a bit tricky since we don't have the old value easily here in RegisterPropertyChangedCallback
            // But for DockWindow, the root is usually set once.
            SubscribeToRoot(PART_DockLayout.Root);
        });

        SubscribeToRoot(PART_DockLayout.Root);
    }

    private void OnTabTornOff(object? sender, TabTornOffEventArgs e)
    {
        App.CreateAndShowDockWindow(e.TabContent);
    }
}
