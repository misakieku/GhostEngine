using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using Ghost.Core;
using Ghost.Editor.Core.Controls.Internal.Docking;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace Ghost.Editor.View.Controls;

/// <summary>
/// A control that renders a docking layout tree.
/// </summary>
[TemplatePart(Name = PART_ROOT_GRID, Type = typeof(Grid))]
[TemplatePart(Name = PART_DROP_TARGET_OVERLAY, Type = typeof(FrameworkElement))]
public sealed partial class DockLayout : Control
{
    private const string PART_ROOT_GRID = "PART_RootGrid";
    private const string PART_DROP_TARGET_OVERLAY = "PART_DropTargetOverlay";
    private const string DRAG_PROPERTY_DOCK_TAB = "Ghost.Editor.DockLayout.TabDragPayload";
    private const double MIN_PANE_SIZE = 100;
    private const double SPLITTER_THICKNESS = 4;
    private const double DROP_EDGE_THRESHOLD = 0.25;

    private FrameworkElement? _dropTargetOverlay;

    public DockLayout()
    {
        DefaultStyleKey = typeof(DockLayout);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Root != null)
        {
            SubscribeToNode(Root);
        }
        RenderTree();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        UnsubscribeFromAll();
    }

    public DockGroupNode? Root
    {
        get => (DockGroupNode?)GetValue(RootProperty);
        set => SetValue(RootProperty, value);
    }

    public static readonly DependencyProperty RootProperty =
        DependencyProperty.Register(nameof(Root), typeof(DockGroupNode), typeof(DockLayout), new PropertyMetadata(null, OnRootChanged));

    private static void OnRootChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockLayout layout)
        {
            layout.UnsubscribeFromAll();

            if (e.NewValue is DockGroupNode newRoot && layout.IsLoaded)
            {
                layout.SubscribeToNode(newRoot);
            }

            layout.RenderTree();
        }
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _dropTargetOverlay = GetTemplateChild(PART_DROP_TARGET_OVERLAY) as FrameworkElement;
        RenderTree();
    }
}
