using Ghost.Editor.Core.Menus;
using Hexa.NET.ImGui;

namespace Ghost.Studio.UI;

/// <summary>
/// Recursively renders dynamic <see cref="EditorMenuRegistry"/> trees into ImGui menus and popups.
/// </summary>
internal static class ImGuiMenuBuilder
{
    public static void DrawNodes(IReadOnlyList<MenuNode> nodes)
    {
        for (var i = 0; i < nodes.Count; i++)
        {
            switch (nodes[i])
            {
                case MenuSeparatorNode:
                    ImGui.Separator();
                    break;

                case MenuSubMenuNode sub:
                    if (ImGui.BeginMenu(sub.Label))
                    {
                        DrawNodes(sub.Children);
                        ImGui.EndMenu();
                    }
                    break;

                case MenuActionNode action:
                    var shortcut = action.Shortcut ?? string.Empty;
                    if (ImGui.MenuItem(action.Label, shortcut, false, action.IsEnabled))
                    {
                        action.Execute();
                    }
                    break;
            }
        }
    }
}
