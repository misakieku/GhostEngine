using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.View.Controls.Docking;

[TemplatePart(Name = "PART_TabView", Type = typeof(TabView))]
public class DockGroup : DockContainer
{
    private TabView? _tabView;

    public DockGroup()
    {
        DefaultStyleKey = typeof(DockGroup);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _tabView = GetTemplateChild("PART_TabView") as TabView;
        UpdateTabs();
    }

    protected override void OnChildrenUpdated()
    {
        UpdateTabs();
    }

    private void UpdateTabs()
    {
        if (_tabView == null) return;

        _tabView.TabItems.Clear();
        foreach (var child in Children)
        {
            if (child is DockDocument doc)
            {
                var tabItem = new TabViewItem
                {
                    Header = doc.Title,
                    Content = doc.Content,
                    Tag = doc
                };
                _tabView.TabItems.Add(tabItem);
            }
        }
    }
}
