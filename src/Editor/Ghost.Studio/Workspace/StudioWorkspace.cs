using Ghost.Studio.DesignSystem;
using Ghost.Studio.Panels.Console;
using Ghost.Studio.Panels.Hierarchy;
using Ghost.Studio.Panels.Inspector;
using Ghost.Studio.Panels.Profiler;
using Ghost.Studio.Panels.Project;
using Ghost.Studio.Panels.Viewport;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Studio.Workspace;

/// <summary>
/// Root studio workspace organizing Hierarchy, Scene Viewport, Bottom Utility Tabs, and Inspector using Flex layout.
/// </summary>
public sealed class StudioWorkspace : Component
{
    private static readonly StudioTabItem[] s_hierarchyTabs =
    {
        new("Hierarchy", "Hierarchy", "\uE8FD", static () => Component<HierarchyPanel>())
    };

    private static readonly StudioTabItem[] s_viewportTabs =
    {
        new("Scene", "Scene", "\uE7C5", static () => Component<ViewportPanel>())
    };

    private static readonly StudioTabItem[] s_bottomTabs =
    {
        new("Project", "Project", "\uED25", static () => Component<ProjectBrowserPanel>()),
        new("Console", "Console", "\uE756", static () => Component<ConsolePanel>()),
        new("Profiler", "Profiler", "\uE9D9", static () => Component<ProfilerPanel>())
    };

    private static readonly StudioTabItem[] s_inspectorTabs =
    {
        new("Inspector", "Inspector", "\uE713", static () => Component<InspectorPanel>())
    };

    private static readonly Action<string> s_noopTabSelect = static _ => { };

    public override Element Render()
    {
        var (layout, setLayout) = UseState(UseMemo(WorkspaceLayoutState.Load, Array.Empty<object>()));
        var latestRef = UseRef(layout);
        latestRef.Current = layout;

        return BuildWorkspaceElement(
            layout,
            updated =>
            {
                var clamped = updated.Clamped();
                latestRef.Current = clamped;
                setLayout(clamped);
            },
            () => latestRef.Current.Save()
        );
    }

    /// <summary>
    /// Builds the workspace layout tree cleanly without requiring a live reconciler context.
    /// </summary>
    internal static Element BuildWorkspaceElement(WorkspaceLayoutState layout, Action<WorkspaceLayoutState> onLayoutChange, Action onCommitLayout)
    {
        var current = layout.Clamped();

        var hierarchyPane = Component<StudioTabPane, StudioTabPaneProps>(new StudioTabPaneProps(s_hierarchyTabs, "Hierarchy", s_noopTabSelect))
            .Flex(basis: current.LeftWidth, shrink: 0, minWidth: 0);

        var leftSplitter = Component<StudioSplitter, StudioSplitterProps>(
            new StudioSplitterProps(StudioSplitterOrientation.VerticalBar, current.LeftWidth, 1.0, width => onLayoutChange(current with { LeftWidth = width }), onCommitLayout)
        );

        var viewportPane = Component<StudioTabPane, StudioTabPaneProps>(new StudioTabPaneProps(s_viewportTabs, "Scene", s_noopTabSelect))
            .Flex(grow: 1, basis: 0, minHeight: 0, minWidth: 0);

        var centerSplitter = Component<StudioSplitter, StudioSplitterProps>(
            new StudioSplitterProps(StudioSplitterOrientation.HorizontalBar, current.BottomHeight, -1.0, height => onLayoutChange(current with { BottomHeight = height }), onCommitLayout)
        );

        var bottomPane = Component<StudioTabPane, StudioTabPaneProps>(
            new StudioTabPaneProps(
                s_bottomTabs,
                current.BottomActiveTabKey,
                key =>
                {
                    var next = (current with { BottomActiveTabKey = key }).Clamped();
                    onLayoutChange(next);
                    next.Save();
                }
            )
        ).Flex(basis: current.BottomHeight, shrink: 0, minHeight: 0, minWidth: 0);

        var centerColumn = FlexColumn(viewportPane, centerSplitter, bottomPane)
            .Flex(grow: 1, basis: 0, minWidth: 0, minHeight: 0);

        var rightSplitter = Component<StudioSplitter, StudioSplitterProps>(
            new StudioSplitterProps(StudioSplitterOrientation.VerticalBar, current.RightWidth, -1.0, width => onLayoutChange(current with { RightWidth = width }), onCommitLayout)
        );

        var inspectorPane = Component<StudioTabPane, StudioTabPaneProps>(new StudioTabPaneProps(s_inspectorTabs, "Inspector", s_noopTabSelect))
            .Flex(basis: current.RightWidth, shrink: 0, minWidth: 0);

        return Border(
            FlexRow(hierarchyPane, leftSplitter, centerColumn, rightSplitter, inspectorPane) with
            {
                FlexPadding = new Thickness(4)
            }
        ).Background(GhostColors.RootBackground);
    }
}
