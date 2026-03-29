using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace Ghost.Editor.View.Controls.Docking;

/// <summary>
/// A container that displays its children (documents) as tabs.
/// </summary>
[TemplatePart(Name = PART_TAB_VIEW, Type = typeof(TabView))]
public partial class DockGroup : DockContainer
{
    private const string PART_TAB_VIEW = "PART_TabView";
    private const string DRAG_DOCUMENT_KEY = "DockDocument";
    private TabView? _tabView;

    public DockGroup()
    {
        DefaultStyleKey = typeof(DockGroup);
    }

    protected override void ValidateChild(DockModule module)
    {
        base.ValidateChild(module);

        if (module is not DockDocument)
        {
            throw new ArgumentException($"{nameof(DockGroup)} only accepts {nameof(DockDocument)} children.", nameof(module));
        }
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_tabView != null)
        {
            _tabView.TabDragStarting -= OnTabDragStarting;
            _tabView.TabDroppedOutside -= OnTabDroppedOutside;
            _tabView.DragOver -= OnDragOver;
            _tabView.Drop -= OnDrop;
            _tabView.DragLeave -= OnDragLeave;
            _tabView.TabCloseRequested -= OnTabCloseRequested;
        }

        _tabView = GetTemplateChild(PART_TAB_VIEW) as TabView;

        if (_tabView != null)
        {
            _tabView.TabDragStarting += OnTabDragStarting;
            _tabView.TabDroppedOutside += OnTabDroppedOutside;
            _tabView.DragOver += OnDragOver;
            _tabView.Drop += OnDrop;
            _tabView.DragLeave += OnDragLeave;
            _tabView.TabCloseRequested += OnTabCloseRequested;
        }

        UpdateTabs();
    }

    private void OnTabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
    {
        if (args.Tab.Tag is DockDocument doc)
        {
            args.Data.Properties.Add(DRAG_DOCUMENT_KEY, doc);
        }
    }

    private void OnTabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
    {
        if (args.Tab.Tag is DockDocument doc)
        {
            Root?.CreateFloatingWindow(doc);
        }
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Properties.ContainsKey(DRAG_DOCUMENT_KEY))
        {
            e.AcceptedOperation = global::Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
            Root?.ShowHighlight(this, e.GetPosition(this));
        }
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (e.DataView.Properties.TryGetValue(DRAG_DOCUMENT_KEY, out var obj) && obj is DockDocument doc)
        {
            Root?.HandleDrop(doc, this, e.GetPosition(this));
        }
    }

    private void OnDragLeave(object sender, DragEventArgs e)
    {
        Root?.HideHighlight();
    }

    private void OnTabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        if (args.Tab.Tag is DockDocument doc)
        {
            RemoveChild(doc);
        }
    }

    protected override void OnChildrenUpdated()
    {
        UpdateTabs();
    }

    private void UpdateTabs()
    {
        if (_tabView == null || Root == null) return;

        var selectedDoc = _tabView.SelectedItem is TabViewItem selectedItem ? selectedItem.Tag as DockDocument : null;

        // Remove tabs that are no longer in Children
        for (int i = _tabView.TabItems.Count - 1; i >= 0; i--)
        {
            if (_tabView.TabItems[i] is TabViewItem tabItem && tabItem.Tag is DockDocument doc)
            {
                if (!Children.Contains(doc))
                {
                    tabItem.Content = null;
                    _tabView.TabItems.RemoveAt(i);
                }
            }
        }

        // Add new tabs that aren't in TabItems yet, and ensure correct order
        for (int i = 0; i < Children.Count; i++)
        {
            if (Children[i] is DockDocument doc)
            {
                TabViewItem? existingTab = null;
                for (int j = 0; j < _tabView.TabItems.Count; j++)
                {
                    if (_tabView.TabItems[j] is TabViewItem tabItem && tabItem.Tag == doc)
                    {
                        existingTab = tabItem;
                        // Fix order if necessary
                        if (j != i)
                        {
                            _tabView.TabItems.RemoveAt(j);
                            _tabView.TabItems.Insert(i, existingTab);
                        }
                        break;
                    }
                }

                if (existingTab == null)
                {
                    existingTab = new TabViewItem
                    {
                        Tag = doc,
                        Content = doc
                    };

                    existingTab.SetBinding(TabViewItem.HeaderProperty, new Microsoft.UI.Xaml.Data.Binding
                    {
                        Source = doc,
                        Path = new PropertyPath(nameof(DockDocument.Title)),
                        Mode = Microsoft.UI.Xaml.Data.BindingMode.OneWay
                    });

                    _tabView.TabItems.Insert(i, existingTab);
                }
            }
        }

        // Restore selection
        if (selectedDoc != null && _tabView.TabItems.FirstOrDefault(t => t is TabViewItem item && item.Tag == selectedDoc) is TabViewItem newSelected)
        {
            _tabView.SelectedItem = newSelected;
        }
        else if (_tabView.TabItems.Count > 0)
        {
            _tabView.SelectedItem = _tabView.TabItems[0];
        }
    }
}
