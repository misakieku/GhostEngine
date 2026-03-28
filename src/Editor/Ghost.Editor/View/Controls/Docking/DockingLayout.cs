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

    private Canvas? _overlayCanvas;
    private DockRegionHighlight? _highlight;
    private readonly List<FloatingWindow> _floatingWindows = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DockingLayout"/> class.
    /// </summary>
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

                if (newPanel.Owner != null)
                {
                    newPanel.Owner.RemoveChild(newPanel);
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
        ArgumentNullException.ThrowIfNull(document);

        if (targetGroup != null && targetGroup.Root != this)
        {
            throw new ArgumentException("targetGroup does not belong to this DockingLayout", nameof(targetGroup));
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

        if (target == DockTarget.Center || targetGroup.Children.Count == 0)
        {
            targetGroup.AddChild(document);
        }
        else
        {
            SplitGroup(targetGroup, document, target);
        }
    }

    private void SplitGroup(DockGroup targetGroup, DockDocument doc, DockTarget target)
    {
        doc.Owner?.RemoveChild(doc);
        var parentPanel = targetGroup.Owner as DockPanel;

        if (parentPanel == null)
        {
            throw new InvalidOperationException("targetGroup must be owned by a DockPanel to be split.");
        }

        var newGroup = new DockGroup();
        newGroup.AddChild(doc);

        var orientation = (target == DockTarget.Left || target == DockTarget.Right) ? Orientation.Horizontal : Orientation.Vertical;

        int index = parentPanel.Children.IndexOf(targetGroup);

        if (index < 0)
        {
            throw new InvalidOperationException("targetGroup not found in parentPanel");
        }

        if (parentPanel.Orientation == orientation)
        {
            // Same orientation, just insert
            if (target == DockTarget.Left || target == DockTarget.Top)
            {
                parentPanel.InsertChild(index, newGroup);
            }
            else
            {
                parentPanel.InsertChild(index + 1, newGroup);
            }
        }
        else
        {
            // Different orientation, need a new sub-panel
            var newPanel = new DockPanel { Orientation = orientation };
            parentPanel.ReplaceChild(targetGroup, newPanel);

            if (target == DockTarget.Left || target == DockTarget.Top)
            {
                newPanel.AddChild(newGroup);
                newPanel.AddChild(targetGroup);
            }
            else
            {
                newPanel.AddChild(targetGroup);
                newPanel.AddChild(newGroup);
            }
        }
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

    internal void ShowHighlight(DockGroup targetGroup, global::Windows.Foundation.Point position)
    {
        if (_highlight == null || _overlayCanvas == null) return;

        _highlight.Visibility = Visibility.Visible;
        var target = CalculateDockTarget(targetGroup, position);

        // Calculate rect based on target
        double width = targetGroup.ActualWidth;
        double height = targetGroup.ActualHeight;
        double x = 0, y = 0;

        switch (target)
        {
            case DockTarget.Left: width /= 2; break;
            case DockTarget.Right: width /= 2; x = width; break;
            case DockTarget.Top: height /= 2; break;
            case DockTarget.Bottom: height /= 2; y = height; break;
            case DockTarget.Center: break;
        }

        var transform = targetGroup.TransformToVisual(_overlayCanvas);
        var point = transform.TransformPoint(new global::Windows.Foundation.Point(x, y));

        Microsoft.UI.Xaml.Controls.Canvas.SetLeft(_highlight, point.X);
        Microsoft.UI.Xaml.Controls.Canvas.SetTop(_highlight, point.Y);
        _highlight.Width = width;
        _highlight.Height = height;
    }

    internal void HideHighlight()
    {
        if (_highlight != null) _highlight.Visibility = Visibility.Collapsed;
    }

    internal void HandleDrop(DockDocument doc, DockGroup targetGroup, global::Windows.Foundation.Point position)
    {
        HideHighlight();
        var target = CalculateDockTarget(targetGroup, position);

        if (target == DockTarget.Center)
        {
            if (doc.Owner == targetGroup) return;
            targetGroup.AddChild(doc);
        }
        else
        {
            if (doc.Owner == targetGroup && targetGroup.Children.Count == 1) return;
            SplitGroup(targetGroup, doc, target);
        }
    }

    private DockTarget CalculateDockTarget(DockGroup group, global::Windows.Foundation.Point position)
    {
        double w = group.ActualWidth;
        double h = group.ActualHeight;
        double x = position.X;
        double y = position.Y;

        if (x < w * 0.25) return DockTarget.Left;
        if (x > w * 0.75) return DockTarget.Right;
        if (y < h * 0.25) return DockTarget.Top;
        if (y > h * 0.75) return DockTarget.Bottom;
        return DockTarget.Center;
    }

    internal void CreateFloatingWindow(DockDocument doc)
    {
        ArgumentNullException.ThrowIfNull(doc);
        var window = new FloatingWindow(doc);
        _floatingWindows.Add(window);
        window.Closed += (s, e) => _floatingWindows.Remove(window);
        window.Activate();
    }
}
