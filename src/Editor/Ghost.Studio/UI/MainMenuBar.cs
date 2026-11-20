using Ghost.AssetForge.Core.Services;
using Ghost.Editor.Core.Commands;
using Ghost.Editor.Core.Menus;
using Ghost.Editor.Core.Selection;
using Ghost.Editor.Core.Sync;
using Ghost.Studio.Backend;
using Ghost.Studio.Platform;
using Hexa.NET.ImGui;

namespace Ghost.Studio.UI;

/// <summary>
/// Studio top menu bar wiring File, Edit (Undo/Redo/Duplicate/Delete), dynamic Entity menus, Theme, Window toggles, and live FPS telemetry.
/// </summary>
internal sealed class MainMenuBar
{
    public void Draw(ImGuiContextManager contextManager, StudioDockspace dockspace, EditorWindowManager windowManager, ProjectService projectService, float deltaTime)
    {
        if (!ImGui.BeginMainMenuBar())
        {
            return;
        }

        var history = CommandHistory.Instance;

        if (ImGui.BeginMenu("File"))
        {
            if (OperatingSystem.IsWindows())
            {
                if (ImGui.MenuItem("New Project..."))
                {
                    NativeProjectDialogs.TryCreateProject(projectService);
                }

                if (ImGui.MenuItem("Open Project..."))
                {
                    NativeProjectDialogs.TryOpenProject(projectService);
                }

                if (ImGui.MenuItem("Save Project", "Ctrl+S", false, projectService.CurrentProject != null))
                {
                    projectService.SaveProject();
                }

                ImGui.Separator();
            }

            if (ImGui.MenuItem("Save Layout"))
            {
                dockspace.SaveLayout();
            }

            if (ImGui.MenuItem("Reset Default Layout"))
            {
                dockspace.RequestResetLayout();
            }

            ImGui.Separator();

            if (ImGui.MenuItem("Exit", "Alt+F4"))
            {
                Environment.Exit(0);
            }

            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu("Edit"))
        {
            var undoLabel = history.CanUndo ? $"Undo {history.NextUndoName}" : "Undo";
            if (ImGui.MenuItem(undoLabel, "Ctrl+Z", false, history.CanUndo))
            {
                history.Undo();
            }

            var redoLabel = history.CanRedo ? $"Redo {history.NextRedoName}" : "Redo";
            if (ImGui.MenuItem(redoLabel, "Ctrl+Y", false, history.CanRedo))
            {
                history.Redo();
            }

            ImGui.Separator();

            var activeEntity = EditorSelection.Instance.ActiveEntity;
            if (ImGui.MenuItem("Duplicate", "Ctrl+D", false, activeEntity != null))
            {
                EntityMenuActions.DuplicateEntity(new MenuContext(activeEntity));
            }

            if (ImGui.MenuItem("Rename", "F2", false, activeEntity != null))
            {
                EditorSelection.Instance.RequestRename(activeEntity);
            }

            if (ImGui.MenuItem("Delete", "Del", false, activeEntity != null))
            {
                EntityMenuActions.DeleteEntity(new MenuContext(activeEntity));
            }

            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu("Entity"))
        {
            var menu = EditorMenuRegistry.Default.BuildMenu("Entity", new MenuContext(EditorSelection.Instance.ActiveEntity));
            ImGuiMenuBuilder.DrawNodes(menu);
            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu("View"))
        {
            if (ImGui.BeginMenu("Theme"))
            {
                if (ImGui.MenuItem("Dark (Unity Default)", string.Empty, contextManager.ThemePreset == EditorThemePreset.Dark))
                {
                    contextManager.SetTheme(EditorThemePreset.Dark);
                }

                if (ImGui.MenuItem("Light", string.Empty, contextManager.ThemePreset == EditorThemePreset.Light))
                {
                    contextManager.SetTheme(EditorThemePreset.Light);
                }

                ImGui.EndMenu();
            }

            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu("Window"))
        {
            var windows = windowManager.RegisteredWindows;
            for (var i = 0; i < windows.Count; i++)
            {
                var win = windows[i];
                if (win.ShowInWindowMenu)
                {
                    ImGui.MenuItem(win.Title, string.Empty, ref win.IsOpen);
                }
            }
            ImGui.Separator();
            if (ImGui.MenuItem("Reset Default Layout"))
            {
                dockspace.RequestResetLayout();
            }

            ImGui.Separator();
            ImGui.MenuItem("Dear ImGui Demo", string.Empty, ref dockspace.ShowImGuiDemo);
            ImGui.MenuItem("ImPlot Demo", string.Empty, ref dockspace.ShowImPlotDemo);
            ImGui.EndMenu();
        }

        DrawRightStatusStrip(contextManager, deltaTime);

        ImGui.EndMainMenuBar();
    }

    private static void DrawRightStatusStrip(ImGuiContextManager contextManager, float deltaTime)
    {
        ref readonly var tokens = ref contextManager.Tokens;
        var fps = deltaTime > 0.00001f ? 1.0f / deltaTime : 0.0f;
        var entityCount = EditorSyncBridge.Instance.LatestSnapshot?.EntityCount ?? 0;
        var text = $"{fps:0} FPS ({deltaTime * 1000.0f:0.00} ms)  |  Entities: {entityCount}  |  DPI {contextManager.DpiScale:0.00}x";

        ImGui.PushFont(contextManager.MonospaceFont, EditorTheme.FONT_SIZE_MONO);
        var textWidth = ImGui.CalcTextSize(text).X;
        var availRight = ImGui.GetWindowWidth() - textWidth - ImGui.GetStyle().WindowPadding.X;
        if (availRight > ImGui.GetCursorPosX() + 24.0f)
        {
            ImGui.SetCursorPosX(availRight);
            ImGui.TextColored(tokens.TextSecondary, text);
        }
        ImGui.PopFont();
    }
}
