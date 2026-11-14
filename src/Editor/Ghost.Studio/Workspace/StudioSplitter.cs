using Ghost.Studio.DesignSystem;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Layout;
using Microsoft.UI.Xaml;
using Windows.Foundation;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Studio.Workspace;

/// <summary>
/// Orientation for a <see cref="StudioSplitter"/> drag handle.
/// </summary>
public enum StudioSplitterOrientation
{
    VerticalBar,
    HorizontalBar
}

/// <summary>
/// Input properties for <see cref="StudioSplitter"/>.
/// </summary>
public sealed record StudioSplitterProps(StudioSplitterOrientation Orientation, double CurrentSize, double DeltaSign, Action<double> OnResize, Action? OnResizeCompleted = null);

/// <summary>
/// Pointer-captured drag handle that resizes adjacent workspace panes without visual feedback oscillation.
/// </summary>
public sealed class StudioSplitter : Component<StudioSplitterProps>
{
    private const double HIT_THICKNESS = 5;
    private const double LINE_THICKNESS = 1;

    private sealed record DragSession(uint PointerId, Point StartRootPosition, double StartSize);

    public override Element Render()
    {
        var (isHovered, setIsHovered) = UseState(false);
        var (isDragging, setIsDragging) = UseState(false);
        var dragRef = UseRef<DragSession?>(null);

        var isVerticalBar = Props.Orientation == StudioSplitterOrientation.VerticalBar;
        var lineBrush = (isHovered || isDragging) ? GhostColors.AccentPrimary : GhostColors.TransparentHitTarget;

        var indicatorLine = isVerticalBar
            ? Border(Empty()).Width(isDragging ? 2 : LINE_THICKNESS).Background(lineBrush).VAlign(VerticalAlignment.Stretch).HAlign(HorizontalAlignment.Center)
            : Border(Empty()).Height(isDragging ? 2 : LINE_THICKNESS).Background(lineBrush).HAlign(HorizontalAlignment.Stretch).VAlign(VerticalAlignment.Center);

        var container = Border(indicatorLine)
            .Background(GhostColors.TransparentHitTarget)
            .OnPointerEntered((_, _) => setIsHovered(true))
            .OnPointerExited((_, _) => setIsHovered(false))
            .OnPointerPressed((sender, e) =>
            {
                if (sender is not UIElement el)
                {
                    return;
                }

                var point = e.GetCurrentPoint(el);
                if (!point.Properties.IsLeftButtonPressed || !el.CapturePointer(e.Pointer))
                {
                    return;
                }

                dragRef.Current = new DragSession(e.Pointer.PointerId, e.GetCurrentPoint(null).Position, Props.CurrentSize);
                setIsDragging(true);
                e.Handled = true;
            })
            .OnPointerMoved((_, e) =>
            {
                var session = dragRef.Current;
                if (session == null || e.Pointer.PointerId != session.PointerId)
                {
                    return;
                }

                var currentPos = e.GetCurrentPoint(null).Position;
                var delta = isVerticalBar
                    ? currentPos.X - session.StartRootPosition.X
                    : currentPos.Y - session.StartRootPosition.Y;

                Props.OnResize(session.StartSize + delta * Props.DeltaSign);
                e.Handled = true;
            })
            .OnPointerReleased((sender, e) =>
            {
                var session = dragRef.Current;
                if (session == null || e.Pointer.PointerId != session.PointerId)
                {
                    return;
                }

                dragRef.Current = null;
                if (sender is UIElement el)
                {
                    el.ReleasePointerCapture(e.Pointer);
                }

                setIsDragging(false);
                Props.OnResizeCompleted?.Invoke();
                e.Handled = true;
            })
            .OnPointerCaptureLost((_, _) =>
            {
                if (dragRef.Current == null)
                {
                    return;
                }

                dragRef.Current = null;
                setIsDragging(false);
                Props.OnResizeCompleted?.Invoke();
            });

        return isVerticalBar
            ? container.Width(HIT_THICKNESS).Flex(shrink: 0)
            : container.Height(HIT_THICKNESS).Flex(shrink: 0);
    }
}
