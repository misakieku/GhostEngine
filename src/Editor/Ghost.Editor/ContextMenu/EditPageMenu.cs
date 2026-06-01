using Ghost.Editor.Core;
using Ghost.Editor.Core.Services;

namespace Ghost.Editor.ContextMenu;

internal static class EditPageContextMenu
{
    [ContextMenuItem("edit-page-menu", "Edit/Undo", priority: 1)]
    private static void MenuBar_Undo()
    {
        App.GetService<IUndoService>().PerformUndo();
    }

    [ContextMenuItem("edit-page-menu", "Edit/Redo", priority: 0)]
    private static void MenuBar_Redo()
    {
        App.GetService<IUndoService>().PerformRedo();
    }
}
