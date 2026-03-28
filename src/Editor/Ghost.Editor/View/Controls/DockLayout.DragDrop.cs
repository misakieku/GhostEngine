using Ghost.Core;
using Ghost.Editor.Core.Controls.Internal.Docking;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.View.Controls;

public sealed partial class DockLayout
{
    private DockPosition _currentDropPosition = DockPosition.None;
    private FrameworkElement? _lastTargetElement;

    private record DockDragPayload(object Item, DockPanelNode SourceNode);

    public event EventHandler<TabTornOffEventArgs>? TabTornOff;

    private void TabView_TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
    {
        if (args.Item != null && sender.Tag is DockPanelNode sourceNode)
        {
            var payload = new DockDragPayload(args.Item, sourceNode);
            args.Data.Properties.Add(DRAG_PROPERTY_DOCK_TAB, payload); // Identify our drag
        }
    }

    private void TabView_DragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Properties.TryGetValue(DRAG_PROPERTY_DOCK_TAB, out var payloadObj) && 
            payloadObj is DockDragPayload &&
            sender is FrameworkElement targetElement)
        {
            e.AcceptedOperation = global::Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;

            var position = e.GetPosition(targetElement);
            var newPosition = DockMath.CalculateDockPosition(targetElement.ActualWidth, targetElement.ActualHeight, position.X, position.Y, DROP_EDGE_THRESHOLD);

            if (newPosition != _currentDropPosition || targetElement != _lastTargetElement)
            {
                _currentDropPosition = newPosition;
                _lastTargetElement = targetElement;
                UpdateDropOverlay(targetElement, _currentDropPosition);
            }
        }
    }

    private void TabView_DragLeave(object sender, DragEventArgs e)
    {
        _lastTargetElement = null;
        ClearOverlayState();
    }

    private void ClearOverlayState()
    {
        if (_dropTargetOverlay != null)
        {
            _dropTargetOverlay.Visibility = Visibility.Collapsed;
        }
        _currentDropPosition = DockPosition.None;
    }

    private void ClearDragOperationState()
    {
        _lastTargetElement = null;
        ClearOverlayState();
    }

    private void UpdateDropOverlay(FrameworkElement targetElement, DockPosition position)
    {
        if (_dropTargetOverlay == null) return;
        if (position == DockPosition.None)
        {
            _dropTargetOverlay.Visibility = Visibility.Collapsed;
            return;
        }

        var transform = targetElement.TransformToVisual(this);
        var bounds = transform.TransformBounds(new global::Windows.Foundation.Rect(0, 0, targetElement.ActualWidth, targetElement.ActualHeight));

        _dropTargetOverlay.Visibility = Visibility.Visible;
        _dropTargetOverlay.Width = double.NaN;
        _dropTargetOverlay.Height = double.NaN;

        switch (position)
        {
            case DockPosition.Center:
                _dropTargetOverlay.Margin = new Thickness(bounds.Left, bounds.Top, ActualWidth - bounds.Right, ActualHeight - bounds.Bottom);
                break;
            case DockPosition.Left:
                _dropTargetOverlay.Margin = new Thickness(bounds.Left, bounds.Top, ActualWidth - (bounds.Left + bounds.Width / 2), ActualHeight - bounds.Bottom);
                break;
            case DockPosition.Right:
                _dropTargetOverlay.Margin = new Thickness(bounds.Left + bounds.Width / 2, bounds.Top, ActualWidth - bounds.Right, ActualHeight - bounds.Bottom);
                break;
            case DockPosition.Top:
                _dropTargetOverlay.Margin = new Thickness(bounds.Left, bounds.Top, ActualWidth - bounds.Right, ActualHeight - (bounds.Top + bounds.Height / 2));
                break;
            case DockPosition.Bottom:
                _dropTargetOverlay.Margin = new Thickness(bounds.Left, bounds.Top + bounds.Height / 2, ActualWidth - bounds.Right, ActualHeight - bounds.Bottom);
                break;
        }
    }

    private void TabView_Drop(object sender, DragEventArgs e)
    {
        if (_dropTargetOverlay != null) _dropTargetOverlay.Visibility = Visibility.Collapsed;

        if (!e.DataView.Properties.TryGetValue(DRAG_PROPERTY_DOCK_TAB, out var payloadObj) ||
            payloadObj is not DockDragPayload payload ||
            !(sender is FrameworkElement targetElement) || 
            !(targetElement.Tag is DockPanelNode targetNode))
        {
            ClearDragOperationState();
            return;
        }

        if (_currentDropPosition == DockPosition.None)
        {
            ClearDragOperationState();
            return;
        }

        if (payload.SourceNode == targetNode && _currentDropPosition == DockPosition.Center)
        {
            ClearDragOperationState();
            return; // Reordering within same tab is handled natively by TabView
        }

        if (Root == null)
        {
            ClearDragOperationState();
            return;
        }

        // 1. Execute mutation
        if (DockMutationEngine.TryApplyDropMutation(Root, targetNode, payload.SourceNode, payload.Item, _currentDropPosition))
        {
            DockMutationEngine.CleanupEmptyNodes(payload.SourceNode);
        }

        ClearDragOperationState();
    }

    private void TabView_TabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
    {
        try
        {
            if (sender.Tag is DockPanelNode sourceNode && args.Item != null)
            {
                // Validate that the item actually belongs to this source node before attempting tear-off
                if (sourceNode.Items.Contains(args.Item))
                {
                    var handler = TabTornOff;
                    if (handler == null)
                    {
                        Logger.LogWarning("Tab dropped outside but no TabTornOff subscribers found.");
                        return;
                    }

                    var result = TabTearOffService.TryTearOffTab(sourceNode.Items, args.Item, (tab) =>
                    {
                        // Raise event to let the host handle window creation
                        handler.Invoke(this, new TabTornOffEventArgs(tab, sourceNode));
                    }, sourceNode);

                    if (result.IsSuccess)
                    {
                        DockMutationEngine.CleanupEmptyNodes(sourceNode);
                    }
                    else
                    {
                        Logger.LogWarning($"Tab tear-off failed: {result.Message}");
                    }
                }
                else
                {
                    string itemInfo = args.Item is FrameworkElement fe ? fe.GetType().Name : args.Item.ToString() ?? "unknown";
                    Logger.LogWarning($"TabDroppedOutside: Item '{itemInfo}' not found in source node (Items count: {sourceNode.Items.Count}).");
                }
            }
        }
        finally
        {
            ClearDragOperationState();
        }
    }
}
