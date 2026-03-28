using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace Ghost.Editor.View.Controls.Docking;

[TemplatePart(Name = PART_TAB_VIEW, Type = typeof(TabView))]
public partial class DockGroup : DockContainer
{
    private const string PART_TAB_VIEW = "PART_TabView";
    private TabView? _tabView;

    public DockGroup()
    {
        DefaultStyleKey = typeof(DockGroup);
    }

    public override void AddChild(DockModule module)
    {
        ArgumentNullException.ThrowIfNull(module);

        if (module is not DockDocument)
        {
            throw new ArgumentException($"{nameof(DockGroup)} only accepts {nameof(DockDocument)} children.", nameof(module));
        }

        base.AddChild(module);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _tabView = GetTemplateChild(PART_TAB_VIEW) as TabView;
        UpdateTabs();
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
    }
}
