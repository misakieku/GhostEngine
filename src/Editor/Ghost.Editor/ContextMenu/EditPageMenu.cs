using Ghost.Core;
using Ghost.Editor.Core;
using Ghost.Editor.Core.Services;
using Windows.System;

namespace Ghost.Editor.ContextMenu;

internal static class EditPageContextMenu
{
    [Shortcut(VirtualKey.S, VirtualKeyModifiers.Control)]
    [ContextMenuItem("edit-page-menu", "File/Save")]
    private static async void MenuBar_Save()
    {
        if (EditorApplication.State != EditorState.Idle)
        {
            Logger.Warning("Cannot save while the editor is busy.");
            return;
        }

        await App.GetService<Ghost.Editor.Core.Contracts.IAssetRegistry>().SaveDirtyAssetsAsync();
    }

    [ContextMenuItem("edit-page-menu", "Edit/Undo", priority: 1, group: 1)]
    private static void MenuBar_Undo()
    {
        App.GetService<IUndoService>().PerformUndo();
    }

    [ContextMenuItem("edit-page-menu", "Edit/Redo", priority: 0, group: 1)]
    private static void MenuBar_Redo()
    {
        App.GetService<IUndoService>().PerformRedo();
    }
}
