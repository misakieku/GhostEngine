using Ghost.Editor.Core.Commands;
using Ghost.Studio.DesignSystem;
using Ghost.Studio.Panels.TitleBar;
using Ghost.Studio.Workspace;
using Microsoft.UI.Input;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Core;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Studio;

public sealed class StudioApp : Component
{
    public override Element Render()
    {
        return Grid(
            columns: [GridSize.Star()],
            rows: [GridSize.Auto, GridSize.Star()],

            Component<EditorTitleBar>().Grid(row: 0, column: 0),
            Component<StudioWorkspace>().Grid(row: 1, column: 0)
        )
        .Background(GhostColors.RootBackground)
        .OnKeyDown((_, e) =>
        {
            var isCtrl = (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
            if (isCtrl && e.Key == VirtualKey.Z)
            {
                CommandHistory.Instance.Undo();
                e.Handled = true;
            }
            else if (isCtrl && e.Key == VirtualKey.Y)
            {
                CommandHistory.Instance.Redo();
                e.Handled = true;
            }
        });
    }
}
