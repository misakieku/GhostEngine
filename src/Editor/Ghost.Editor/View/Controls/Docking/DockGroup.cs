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
        }

        _tabView = GetTemplateChild(PART_TAB_VIEW) as TabView;

        if (_tabView != null)
        {
            _tabView.TabDragStarting += OnTabDragStarting;
            _tabView.TabDroppedOutside += OnTabDroppedOutside;
            _tabView.DragOver += OnDragOver;
            _tabView.Drop += OnDrop;
            _tabView.DragLeave += OnDragLeave;
        }

        UpdateTabs();
    }

    private void OnTabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
    {
        if (args.Tab.Tag is DockDocument doc)
        {
            args.Data.Properties.Add("DockDocument", doc);
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
        if (e.DataView.Properties.ContainsKey("DockDocument"))
        {
            e.AcceptedOperation = global::Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
            Root?.ShowHighlight(this, e.GetPosition(this));
        }
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (e.DataView.Properties.TryGetValue("DockDocument", out var obj) && obj is DockDocument doc)
        {
            Root?.HandleDrop(doc, this, e.GetPosition(this));
        }
    }

    private void OnDragLeave(object sender, DragEventArgs e)
    {
        Root?.HideHighlight();
    }

    protected override void OnChildrenUpdated()
    {
        UpdateTabs();
    }

    private void UpdateTabs()
    {
        if (_tabView == null) return;

        var selectedDoc = _tabView.SelectedItem is TabViewItem selectedItem ? selectedItem.Tag as DockDocument : null;

        _tabView.TabItems.Clear();
        TabViewItem? newSelectedItem = null;

        foreach (var child in Children)
        {
            if (child is DockDocument doc)
            {
                var tabItem = new TabViewItem
                {
                    Tag = doc
                };

                tabItem.SetBinding(TabViewItem.HeaderProperty, new Binding
                {
                    Source = doc,
                    Path = new PropertyPath(nameof(DockDocument.Title)),
                    Mode = BindingMode.OneWay
                });

                tabItem.SetBinding(ContentControl.ContentProperty, new Binding
                {
                    Source = doc,
                    Path = new PropertyPath(nameof(DockDocument.Content)),
                    Mode = BindingMode.OneWay
                });

                _tabView.TabItems.Add(tabItem);

                if (doc == selectedDoc)
                {
                    newSelectedItem = tabItem;
                }
            }
        }

        if (newSelectedItem != null)
        {
            _tabView.SelectedItem = newSelectedItem;
        }
        else
        {
            _tabView.SelectedItem = _tabView.TabItems.FirstOrDefault();
        }
    }
}
