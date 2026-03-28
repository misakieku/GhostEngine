using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.View.Controls.Docking;

/// <summary>
/// The root control for the docking system layout.
/// </summary>
[TemplatePart(Name = PART_OVERLAY_CANVAS, Type = typeof(Canvas))]
[TemplatePart(Name = PART_HIGHLIGHT, Type = typeof(DockRegionHighlight))]
public class DockingLayout : Control
{
    private const string PART_OVERLAY_CANVAS = "PART_OverlayCanvas";
    private const string PART_HIGHLIGHT = "PART_Highlight";

    /// <summary>
    /// Gets or sets the root panel of the docking layout.
    /// </summary>
    public static readonly DependencyProperty RootPanelProperty = DependencyProperty.Register(
        nameof(RootPanel), typeof(DockPanel), typeof(DockingLayout), new PropertyMetadata(null, OnRootPanelChanged));

    /// <summary>
    /// Gets or sets the root panel of the docking layout.
    /// </summary>
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
        _overlayCanvas = GetTemplateChild(PART_OVERLAY_CANVAS) as Canvas;
        _highlight = GetTemplateChild(PART_HIGHLIGHT) as DockRegionHighlight;
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
                if (newPanel.Root != null && newPanel.Root != layout)
                {
                    throw new InvalidOperationException("Panel is already owned by another DockingLayout");
                }

                newPanel.Root = layout;
            }
        }
    }

    /// <summary>
    /// Adds a document to the docking layout.
    /// </summary>
    /// <param name="document">The document to add.</param>
    /// <param name="target">The docking target position.</param>
    /// <param name="targetGroup">The target group to add the document to. If null, a suitable group will be found or created.</param>
    public void AddDocument(DockDocument document, DockTarget target, DockGroup? targetGroup = null)
    {
        if (target != DockTarget.Center)
        {
            throw new NotImplementedException("Target docking will be implemented in Task 5");
        }

        if (targetGroup != null && targetGroup.Root != this)
        {
            throw new ArgumentException("targetGroup does not belong to this DockingLayout");
        }

        if (RootPanel == null)
        {
            RootPanel = new DockPanel();
        }

        if (targetGroup == null)
        {
            targetGroup = FindFirstDockGroup(RootPanel);

            if (targetGroup == null)
            {
                targetGroup = new DockGroup();
                RootPanel.AddChild(targetGroup);
            }
        }

        targetGroup.AddChild(document);
    }

    private static DockGroup? FindFirstDockGroup(DockContainer container)
    {
        if (container is DockGroup group)
        {
            return group;
        }

        foreach (var child in container.Children)
        {
            if (child is DockContainer childContainer)
            {
                var result = FindFirstDockGroup(childContainer);
                if (result != null)
                {
                    return result;
                }
            }
        }

        return null;
    }
}
