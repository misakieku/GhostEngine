using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.View.Controls.Docking;

[TemplatePart(Name = "PART_OverlayCanvas", Type = typeof(Canvas))]
[TemplatePart(Name = "PART_Highlight", Type = typeof(DockRegionHighlight))]
public class DockingLayout : Control
{
    public static readonly DependencyProperty RootPanelProperty = DependencyProperty.Register(
        nameof(RootPanel), typeof(DockPanel), typeof(DockingLayout), new PropertyMetadata(null, OnRootPanelChanged));

    public DockPanel? RootPanel
    {
        get => (DockPanel?)GetValue(RootPanelProperty);
        set => SetValue(RootPanelProperty, value);
    }

    // Used in Task 5 for drag and drop highlight
    private Canvas? _overlayCanvas;
    // Used in Task 5 for drag and drop highlight
    private DockRegionHighlight? _highlight;

    public DockingLayout()
    {
        DefaultStyleKey = typeof(DockingLayout);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _overlayCanvas = GetTemplateChild("PART_OverlayCanvas") as Canvas;
        _highlight = GetTemplateChild("PART_Highlight") as DockRegionHighlight;
    }

    private static void OnRootPanelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockingLayout layout)
        {
            if (e.OldValue is DockPanel oldPanel)
            {
                oldPanel.Root = null;
            }

            if (e.NewValue is DockPanel newPanel)
            {
                newPanel.Root = layout;
            }
        }
    }

    public void AddDocument(DockDocument document, DockTarget target, DockGroup? targetGroup = null)
    {
        if (target != DockTarget.Center)
        {
            throw new NotImplementedException("Target docking will be implemented in Task 5");
        }

        if (RootPanel == null)
        {
            RootPanel = new DockPanel();
        }

        if (targetGroup == null)
        {
            if (RootPanel.Children.Count == 0)
            {
                var group = new DockGroup();
                group.AddChild(document);
                RootPanel.AddChild(group);
                return;
            }

            if (RootPanel.Children[0] is DockGroup existingGroup)
            {
                targetGroup = existingGroup;
            }
            else
            {
                var group = new DockGroup();
                group.AddChild(document);
                RootPanel.AddChild(group);
                return;
            }
        }

        if (targetGroup != null)
        {
            targetGroup.AddChild(document);
        }
    }
}
