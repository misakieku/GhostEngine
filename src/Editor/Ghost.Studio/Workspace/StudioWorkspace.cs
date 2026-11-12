using Ghost.Studio.DesignSystem;
using Ghost.Studio.Panels.Console;
using Ghost.Studio.Panels.Hierarchy;
using Ghost.Studio.Panels.Inspector;
using Ghost.Studio.Panels.Profiler;
using Ghost.Studio.Panels.Project;
using Ghost.Studio.Panels.Viewport;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Docking;
using Microsoft.UI.Xaml.Controls;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Studio.Workspace;

public sealed class StudioWorkspace : Component
{
    public override Element Render()
    {
        return new DockManager
        {
            PersistenceId = "GhostEngineStudio_Workspace",
            Layout = new DockSplit(
                Orientation.Horizontal,
                [
                    new ToolWindow
                    {
                        Title = "Hierarchy",
                        Key = "Hierarchy",
                        Content = Component<HierarchyPanel>()
                    },
                    new DockSplit(
                        Orientation.Vertical,
                        [
                            new DockTabGroup(
                                [
                                    new Document
                                    {
                                        Title = "Scene",
                                        Key = "Scene",
                                        Content = Component<ViewportPanel>()
                                    }
                                ],
                                Role: DockGroupRole.DocumentArea
                            ),
                            new DockTabGroup(
                                [
                                    new ToolWindow
                                    {
                                        Title = "Project",
                                        Key = "Project",
                                        Content = Component<ProjectBrowserPanel>()
                                    },
                                    new ToolWindow
                                    {
                                        Title = "Console",
                                        Key = "Console",
                                        Content = Component<ConsolePanel>()
                                    },
                                    new ToolWindow
                                    {
                                        Title = "Profiler",
                                        Key = "Profiler",
                                        Content = Component<ProfilerPanel>()
                                    }
                                ],
                                CompactTabs: true
                            )
                        ]
                    ),
                    new ToolWindow
                    {
                        Title = "Inspector",
                        Key = "Inspector",
                        Content = Component<InspectorPanel>()
                    }
                ]
            )
        };
    }
}
