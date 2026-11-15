using Ghost.AssetForge.Core.Services;
using Ghost.Editor.Core.Commands;
using Ghost.Editor.Core.Menus;
using Ghost.Editor.Core.Selection;
using Ghost.Engine.Systems;
using Ghost.Entities;
using Ghost.Studio.Backend;
using Ghost.Studio.Rendering;
using Ghost.Studio.Services;
using Ghost.Studio.UI;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using Hexa.NET.ImPlot;

namespace Ghost.Studio.Systems;

[UpdateAfter<EditorEcsSyncSystem>]
[UpdateBefore<EditorCameraControllerSystem>]
[UpdateBefore<EditorRenderSystemGroup>]
internal sealed class EditorImGuiSystem : SystemBase
{
    private readonly ImGuiContextManager _contextManager;
    private readonly ImGuiSdlInputBridge _inputBridge;
    private readonly ImGuiPostRenderCommand _postRenderCommand;
    private readonly SceneViewportTarget _viewportTarget;
    private readonly ProjectService _projectService;
    private readonly AssetImportService _assetImportService;

    private readonly StudioDockspace _dockspace = new();
    private readonly MainMenuBar _mainMenuBar = new();
    private readonly SceneViewportPanel _sceneViewportPanel;
    private readonly HierarchyPanel _hierarchyPanel;
    private readonly InspectorPanel _inspectorPanel = new();
    private readonly ProjectPanel _projectPanel;
    private readonly AssetImportSettingsWindow _importSettingsWindow;
    private readonly ConsolePanel _consolePanel = new();
    private readonly ProfilerPanel _profilerPanel = new();
    private readonly NodeGraphPanel _nodeGraphPanel = new();
    private int _lastObservedImportRevision;

    public EditorImGuiSystem(ImGuiContextManager contextManager, ImGuiSdlInputBridge inputBridge, ImGuiPostRenderCommand postRenderCommand, SceneViewportTarget viewportTarget, EditorViewportInputState viewportInputState, ProjectService projectService)
    {
        _contextManager = contextManager;
        _inputBridge = inputBridge;
        _postRenderCommand = postRenderCommand;
        _viewportTarget = viewportTarget;
        _projectService = projectService;
        _assetImportService = new AssetImportService(projectService);
        _importSettingsWindow = new AssetImportSettingsWindow(_projectService, _assetImportService);
        _sceneViewportPanel = new SceneViewportPanel(viewportTarget, viewportInputState);
        _hierarchyPanel = new HierarchyPanel(viewportInputState);
        _projectPanel = new ProjectPanel(_projectService.AssetDirectories.FirstOrDefault());
        _projectPanel.BindAssetActions(_projectService.BakerRegistry.HasBaker, _importSettingsWindow.OpenForAsset, path => _assetImportService.EnqueueReimport(path, forceRebake: true));
        _projectService.OnProjectLoaded += HandleProjectLoaded;
    }

    protected override void OnUpdate(scoped in SystemUpdateAPI systemAPI)
    {
        while (_inputBridge.TryDequeueDroppedPaths(out var droppedPaths))
        {
            _assetImportService.EnqueueImport(droppedPaths, _projectPanel.CurrentDirectory);
        }

        var importRevision = _assetImportService.ImportRevision;
        if (importRevision != _lastObservedImportRevision)
        {
            _lastObservedImportRevision = importRevision;
            _projectPanel.MarkDirty();
        }

        _contextManager.MakeCurrent();
        _dockspace.EnsureIniLoaded();
        _inputBridge.BeginFrame(systemAPI.Time.DeltaTime);

        _profilerPanel.RecordSample(systemAPI.Time.DeltaTime);

        ImGui.NewFrame();
        ImGuizmo.BeginFrame();

        HandleGlobalShortcuts();

        _mainMenuBar.Draw(_contextManager, _dockspace, _projectService, systemAPI.Time.DeltaTime);
        _dockspace.BeginDockSpace();

        var renderedSceneTexture = false;
        if (_dockspace.ShowSceneViewport)
        {
            renderedSceneTexture = _sceneViewportPanel.Draw(_contextManager, systemAPI.World.EntityManager, ref _dockspace.ShowSceneViewport);
        }

        if (_dockspace.ShowHierarchy)
        {
            _hierarchyPanel.Draw(_contextManager, ref _dockspace.ShowHierarchy);
        }

        if (_dockspace.ShowInspector)
        {
            _inspectorPanel.Draw(_contextManager, ref _dockspace.ShowInspector);
        }

        if (_dockspace.ShowProject)
        {
            var importProgress = _assetImportService.GetProgress();
            _projectPanel.Draw(_contextManager, ref _dockspace.ShowProject, in importProgress);
        }

        _importSettingsWindow.Draw(_contextManager);

        if (_dockspace.ShowConsole)
        {
            _consolePanel.Draw(_contextManager, ref _dockspace.ShowConsole);
        }

        if (_dockspace.ShowProfiler)
        {
            _profilerPanel.Draw(_contextManager, ref _dockspace.ShowProfiler);
        }

        if (_dockspace.ShowNodeGraph)
        {
            _nodeGraphPanel.Draw(_contextManager, ref _dockspace.ShowNodeGraph);
        }

        if (_dockspace.ShowImGuiDemo)
        {
            ImGui.ShowDemoWindow(ref _dockspace.ShowImGuiDemo);
        }

        if (_dockspace.ShowImPlotDemo)
        {
            ImPlot.ShowDemoWindow(ref _dockspace.ShowImPlotDemo);
        }

        ImGui.Render();
        _dockspace.SyncAutoSaveIni();

        var externalTex = renderedSceneTexture ? _viewportTarget.TextureHandle : default;
        var slot = _postRenderCommand.GetSnapshotSlot(systemAPI.Time.FrameIndex);
        slot.Capture(ImGui.GetDrawData(), externalTex);
    }

    protected override void OnCleanup(scoped in SystemAPI systemAPI)
    {
        _projectService.OnProjectLoaded -= HandleProjectLoaded;
        _assetImportService.Dispose();
        _dockspace.SaveLayout();
        _hierarchyPanel.Dispose();
        _consolePanel.Dispose();
    }

    private void HandleProjectLoaded()
    {
        var primaryAssetDir = _projectService.AssetDirectories.FirstOrDefault();
        if (!string.IsNullOrEmpty(primaryAssetDir))
        {
            _projectPanel.SetRootDirectory(primaryAssetDir);
        }
    }

    private static void HandleGlobalShortcuts()
    {
        var io = ImGui.GetIO();
        if (io.WantTextInput)
        {
            return;
        }

        if (io.KeyCtrl)
        {
            if (ImGui.IsKeyPressed(ImGuiKey.Z))
            {
                if (io.KeyShift)
                {
                    CommandHistory.Instance.Redo();
                }
                else
                {
                    CommandHistory.Instance.Undo();
                }

                return;
            }

            if (ImGui.IsKeyPressed(ImGuiKey.Y))
            {
                CommandHistory.Instance.Redo();
                return;
            }

            if (ImGui.IsKeyPressed(ImGuiKey.D) && EditorSelection.Instance.ActiveEntity != null)
            {
                EntityMenuActions.DuplicateEntity(new MenuContext(EditorSelection.Instance.ActiveEntity));
                return;
            }
        }

        if (ImGui.IsKeyPressed(ImGuiKey.Delete) && EditorSelection.Instance.ActiveEntity != null)
        {
            EntityMenuActions.DeleteEntity(new MenuContext(EditorSelection.Instance.ActiveEntity));
        }
    }
}
