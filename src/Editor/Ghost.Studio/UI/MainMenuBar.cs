using Ghost.AssetForge.Core.Services;
using Ghost.Editor.Core.Commands;
using Ghost.Editor.Core.Menus;
using Ghost.Editor.Core.Selection;
using Ghost.Editor.Core.Sync;
using Ghost.Engine;
using Ghost.Studio.Backend;
using Ghost.Studio.Platform;
using Ghost.Studio.Services;
using Hexa.NET.ImGui;

namespace Ghost.Studio.UI;

/// <summary>
/// Studio top menu bar wiring File, Edit (Undo/Redo/Duplicate/Delete), dynamic Entity menus, Theme, Window toggles, and live FPS telemetry.
/// </summary>
internal sealed class MainMenuBar
{
    public void Draw(ImGuiContextManager contextManager, StudioDockspace dockspace, EditorWindowManager windowManager, ProjectService projectService, float deltaTime, EditorSceneSession? sceneSession = null, Action? onSceneSaved = null, Action? requestExit = null, nint hwndOwner = 0)
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
                if (sceneSession != null)
                {
                    if (ImGui.MenuItem("New Scene"))
                    {
                        if (ConfirmDiscardOrSaveDirtyScenes(sceneSession, projectService, onSceneSaved, hwndOwner))
                        {
                            sceneSession.NewScene();
                        }
                    }

                    if (ImGui.MenuItem("Open Scene..."))
                    {
                        if (ConfirmDiscardOrSaveDirtyScenes(sceneSession, projectService, onSceneSaved, hwndOwner))
                        {
                            var initialDir = projectService.AssetDirectories.FirstOrDefault();
                            var selected = NativeProjectDialogs.ShowOpenSceneFileDialog(initialDir, hwndOwner);
                            if (!string.IsNullOrEmpty(selected))
                            {
                                sceneSession.OpenScene(selected, SceneLoadingType.Single);
                            }
                        }
                    }

                    if (ImGui.MenuItem("Open Scene Additive..."))
                    {
                        var initialDir = projectService.AssetDirectories.FirstOrDefault();
                        var selected = NativeProjectDialogs.ShowOpenSceneFileDialog(initialDir, hwndOwner);
                        if (!string.IsNullOrEmpty(selected))
                        {
                            sceneSession.OpenScene(selected, SceneLoadingType.Additive);
                        }
                    }

                    if (ImGui.MenuItem("Save Scene", "Ctrl+S"))
                    {
                        SaveSceneWithFallbackDialog(sceneSession, projectService, EditorSyncBridge.Instance.ActiveSceneId, forceDialog: false, onSceneSaved, hwndOwner);
                    }

                    if (ImGui.MenuItem("Save Scene As..."))
                    {
                        SaveSceneWithFallbackDialog(sceneSession, projectService, EditorSyncBridge.Instance.ActiveSceneId, forceDialog: true, onSceneSaved, hwndOwner);
                    }

                    ImGui.Separator();
                }

                if (ImGui.MenuItem("New Project..."))
                {
                    if (ConfirmDiscardOrSaveAllDirty(sceneSession, projectService, windowManager, onSceneSaved, hwndOwner))
                    {
                        NativeProjectDialogs.TryCreateProject(projectService, hwndOwner);
                    }
                }

                if (ImGui.MenuItem("Open Project..."))
                {
                    if (ConfirmDiscardOrSaveAllDirty(sceneSession, projectService, windowManager, onSceneSaved, hwndOwner))
                    {
                        NativeProjectDialogs.TryOpenProject(projectService, hwndOwner);
                    }
                }

                if (ImGui.MenuItem("Save Project", "Ctrl+Shift+S", false, projectService.CurrentProject != null))
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
                requestExit?.Invoke();
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

        DrawRightStatusStrip(contextManager, deltaTime, sceneSession);

        ImGui.EndMainMenuBar();
    }

    internal static bool ConfirmDiscardOrSaveAllDirty(EditorSceneSession? sceneSession, ProjectService projectService, EditorWindowManager? windowManager, Action? onSceneSaved, nint hwndOwner = 0)
    {
        if (!OperatingSystem.IsWindows())
        {
            return true;
        }

        if (windowManager != null)
        {
            var dirtyAssets = new List<AssetEditorWindow>();
            windowManager.CollectDirtyAssetWindows(dirtyAssets);
            for (var i = 0; i < dirtyAssets.Count; i++)
            {
                var assetWin = dirtyAssets[i];
                var choice = NativeProjectDialogs.PromptSaveDirtyAsset(assetWin.Title, hwndOwner);
                if (choice == NativeProjectDialogs.UnsavedPromptResult.Cancel)
                {
                    return false;
                }

                if (choice == NativeProjectDialogs.UnsavedPromptResult.Save && !assetWin.Save())
                {
                    return false;
                }
            }
        }

        return sceneSession == null || ConfirmDiscardOrSaveDirtyScenes(sceneSession, projectService, onSceneSaved, hwndOwner);
    }

    internal static bool ConfirmDiscardOrSaveDirtyScenes(EditorSceneSession sceneSession, ProjectService projectService, Action? onSceneSaved, nint hwndOwner = 0)
    {
        if (!sceneSession.IsDirty || !OperatingSystem.IsWindows())
        {
            return true;
        }

        var dirtyScenes = new List<SceneNode>();
        sceneSession.GetDirtyScenes(dirtyScenes);
        for (var i = 0; i < dirtyScenes.Count; i++)
        {
            var scene = dirtyScenes[i];
            if (!ConfirmDiscardOrSaveDirtyScene(sceneSession, projectService, scene.SceneId, scene.Name, onSceneSaved, hwndOwner))
            {
                return false;
            }
        }

        return true;
    }

    internal static bool ConfirmDiscardOrSaveDirtyScene(EditorSceneSession sceneSession, ProjectService projectService, ushort sceneId, string sceneDisplayName, Action? onSceneSaved, nint hwndOwner = 0)
    {
        if (!OperatingSystem.IsWindows())
        {
            return true;
        }

        var choice = NativeProjectDialogs.PromptSaveDirtyScene(sceneDisplayName, hwndOwner);
        return choice switch
        {
            NativeProjectDialogs.UnsavedPromptResult.Save => SaveSceneWithFallbackDialog(sceneSession, projectService, sceneId, forceDialog: false, onSceneSaved, hwndOwner),
            NativeProjectDialogs.UnsavedPromptResult.Discard => true,
            _ => false,
        };
    }

    internal static bool SaveSceneWithFallbackDialog(EditorSceneSession sceneSession, ProjectService projectService, bool forceDialog, Action? onSceneSaved, nint hwndOwner = 0)
        => SaveSceneWithFallbackDialog(sceneSession, projectService, EditorSyncBridge.Instance.ActiveSceneId, forceDialog, onSceneSaved, hwndOwner);

    internal static bool SaveSceneWithFallbackDialog(EditorSceneSession sceneSession, ProjectService projectService, ushort sceneId, bool forceDialog, Action? onSceneSaved, nint hwndOwner = 0)
    {
        var existingPath = sceneSession.GetScenePath(sceneId);
        if (!forceDialog && !string.IsNullOrWhiteSpace(existingPath))
        {
            var ok = sceneSession.SaveScene(sceneId);
            if (ok)
            {
                onSceneSaved?.Invoke();
            }

            return ok;
        }

        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        var initialDir = !string.IsNullOrWhiteSpace(existingPath)
            ? Path.GetDirectoryName(existingPath)
            : projectService.AssetDirectories.FirstOrDefault();
        var defaultFile = !string.IsNullOrWhiteSpace(existingPath)
            ? Path.GetFileName(existingPath)
            : "Main.gscene";
        var savePath = NativeProjectDialogs.ShowSaveSceneFileDialog(initialDir, defaultFile, hwndOwner);
        if (string.IsNullOrEmpty(savePath))
        {
            return false;
        }

        var saved = sceneSession.SaveScene(sceneId, savePath);
        if (saved)
        {
            onSceneSaved?.Invoke();
        }

        return saved;
    }

    private static void DrawRightStatusStrip(ImGuiContextManager contextManager, float deltaTime, EditorSceneSession? sceneSession)
    {
        ref readonly var tokens = ref contextManager.Tokens;
        var fps = deltaTime > 0.00001f ? 1.0f / deltaTime : 0.0f;
        var entityCount = EditorSyncBridge.Instance.LatestSnapshot?.EntityCount ?? 0;
        var sceneLabel = sceneSession != null ? $"{sceneSession.CurrentSceneDisplayName}{(sceneSession.IsDirty ? "*" : string.Empty)}  |  " : string.Empty;
        var text = $"{sceneLabel}{fps:0} FPS ({deltaTime * 1000.0f:0.00} ms)  |  Entities: {entityCount}  |  DPI {contextManager.DpiScale:0.00}x";

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
