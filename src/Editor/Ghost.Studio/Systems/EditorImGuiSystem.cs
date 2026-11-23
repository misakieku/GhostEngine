using Ghost.AssetForge.Core.Models;
using Ghost.AssetForge.Core.Services;
using Ghost.Editor.Core.Commands;
using Ghost.Editor.Core.Menus;
using Ghost.Editor.Core.Selection;
using Ghost.Editor.Core.Sync;
using Ghost.Engine;
using Ghost.Engine.Streaming;
using Ghost.Engine.Systems;
using Ghost.Entities;
using Ghost.Graphics;
using Ghost.Studio.Assets;
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
    private readonly EditorContentProvider? _contentProvider;
    private readonly AssetImportService _assetImportService;
    private readonly EditorAssetCatalog _assetCatalog;
    private readonly EditorSceneSession _sceneSession;

    private readonly StudioDockspace _dockspace = new();
    private readonly MainMenuBar _mainMenuBar = new();
    private readonly EditorWindowManager _windowManager = new();
    private readonly SceneViewportPanel _sceneViewportPanel;
    private readonly HierarchyPanel _hierarchyPanel;
    private readonly InspectorPanel _inspectorPanel;
    private readonly ProjectPanel _projectPanel;
    private readonly AssetImportSettingsWindow _importSettingsWindow;
    private readonly ConsolePanel _consolePanel = new();
    private readonly ProfilerPanel _profilerPanel = new();
    private readonly NodeGraphPanel _nodeGraphPanel = new();
    private int _lastObservedImportRevision;

    public EditorImGuiSystem(ImGuiContextManager contextManager, ImGuiSdlInputBridge inputBridge, ImGuiPostRenderCommand postRenderCommand, SceneViewportTarget viewportTarget, EditorViewportInputState viewportInputState, ProjectService projectService, EditorContentProvider? contentProvider = null, EditorAssetCatalog? sharedCatalog = null, EditorSceneSession? sharedSceneSession = null)
    {
        _contextManager = contextManager;
        _inputBridge = inputBridge;
        _postRenderCommand = postRenderCommand;
        _viewportTarget = viewportTarget;
        _projectService = projectService;
        _contentProvider = contentProvider;
        _assetImportService = new AssetImportService(projectService);
        _assetCatalog = sharedCatalog ?? new EditorAssetCatalog(projectService, contentProvider);
        _sceneSession = sharedSceneSession ?? new EditorSceneSession(projectService, _assetCatalog);
        if (_contentProvider != null)
        {
            _assetImportService.ProjectCacheUpdated += _contentProvider.RefreshProjectIndex;
        }

        _assetImportService.ProjectAssetsBaked += HandleProjectAssetsBaked;
        _importSettingsWindow = new AssetImportSettingsWindow(_projectService, _assetImportService, _assetCatalog);
        _sceneViewportPanel = new SceneViewportPanel(viewportTarget, viewportInputState);
        _projectPanel = new ProjectPanel(_projectService.AssetDirectories.FirstOrDefault());
        _hierarchyPanel = new HierarchyPanel(viewportInputState, _assetCatalog, _projectPanel.SelectAndRevealAsset);
        _inspectorPanel = new InspectorPanel(_projectService, _assetCatalog, _projectPanel.SelectAndRevealAsset, HandleOpenAsset);
        _projectPanel.BindAssetActions(_projectService.BakerRegistry.HasBaker, HandleOpenAsset, _importSettingsWindow.OpenForAsset,
            path => _assetImportService.EnqueueReimport(path, forceRebake: true), HandleCreateScene);

        _windowManager.RegisterWindow(_sceneViewportPanel);
        _windowManager.RegisterWindow(_hierarchyPanel);
        _windowManager.RegisterWindow(_inspectorPanel);
        _windowManager.RegisterWindow(_projectPanel);
        _windowManager.RegisterWindow(_consolePanel);
        _windowManager.RegisterWindow(_profilerPanel);
        _windowManager.RegisterWindow(_nodeGraphPanel);
        _windowManager.RegisterWindow(_importSettingsWindow);
        _windowManager.RegisterAssetEditor(".gmat", path => new MaterialEditorWindow(path, _projectService, _assetImportService, _assetCatalog, _projectPanel.SelectAndRevealAsset));

        _projectService.OnProjectLoaded += HandleProjectLoaded;
        CommandHistory.Instance.HistoryChanged += HandleCommandHistoryChanged;
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
            _assetCatalog.MarkDirty();
        }

        _contextManager.MakeCurrent();
        _dockspace.EnsureIniLoaded();
        _inputBridge.BeginFrame(systemAPI.Time.DeltaTime);

        _profilerPanel.RecordSample(systemAPI.Time.DeltaTime);

        ImGui.NewFrame();
        ImGuizmo.BeginFrame();

        HandleGlobalShortcuts();

        _mainMenuBar.Draw(_contextManager, _dockspace, _windowManager, _projectService, systemAPI.Time.DeltaTime, _sceneSession, RefreshAfterSceneWrite);
        _dockspace.BeginDockSpace();

        _sceneViewportPanel.ResetFrameState();
        _sceneViewportPanel.CurrentEntityManager = systemAPI.World.EntityManager;
        _sceneViewportPanel.CurrentRenderEngine = systemAPI.World.TryGetService<RenderEngine>(out var renderEngine) ? renderEngine : null;
        _sceneViewportPanel.CurrentFrameIndex = systemAPI.Time.FrameIndex;
        _projectPanel.CurrentImportProgress = _assetImportService.GetProgress();
        _windowManager.DrawAll(_contextManager);

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

        var externalTex = _sceneViewportPanel.RenderedSceneTexture ? _viewportTarget.GetTextureHandle(systemAPI.Time.FrameIndex) : default;
        var slot = _postRenderCommand.GetSnapshotSlot(systemAPI.Time.FrameIndex);
        slot.Capture(ImGui.GetDrawData(), externalTex);
    }

    protected override void OnCleanup(scoped in SystemAPI systemAPI)
    {
        CommandHistory.Instance.HistoryChanged -= HandleCommandHistoryChanged;
        _projectService.OnProjectLoaded -= HandleProjectLoaded;
        _assetImportService.ProjectAssetsBaked -= HandleProjectAssetsBaked;
        if (_contentProvider != null)
        {
            _assetImportService.ProjectCacheUpdated -= _contentProvider.RefreshProjectIndex;
        }
        _assetImportService.Dispose();
        _dockspace.SaveLayout();
        _windowManager.Dispose();
    }

    private static void HandleProjectAssetsBaked(IReadOnlyList<Guid> bakedGuids)
    {
        if (bakedGuids.Count == 0)
        {
            return;
        }

        var guidsCopy = bakedGuids is Guid[] arr ? arr : bakedGuids.ToArray();
        EditorSyncBridge.Instance.EnqueueWorldAction(world =>
        {
            if (!world.TryGetService<AssetManager>(out var assetManager))
            {
                return;
            }

            for (var i = 0; i < guidsCopy.Length; i++)
            {
                assetManager.ReimportAsset(guidsCopy[i]);
            }
        });
    }

    private void HandleProjectLoaded()
    {
        var primaryAssetDir = _projectService.AssetDirectories.FirstOrDefault();
        if (!string.IsNullOrEmpty(primaryAssetDir))
        {
            _projectPanel.SetRootDirectory(primaryAssetDir);
        }
    }

    private void HandleOpenAsset(string fullPath)
    {
        if (fullPath.EndsWith(SceneAssetDocument.EXTENSION, StringComparison.OrdinalIgnoreCase))
        {
            if (MainMenuBar.ConfirmDiscardOrSaveDirtyScene(_sceneSession, _projectService, RefreshAfterSceneWrite))
            {
                _sceneSession.OpenScene(fullPath, SceneLoadingType.Single);
            }

            return;
        }

        _windowManager.OpenAsset(fullPath);
    }

    private void HandleCreateScene(string targetDirectory)
    {
        var created = _sceneSession.CreateEmptySceneAsset(targetDirectory);
        _projectPanel.SelectAndRevealAsset(created);
    }

    private void RefreshAfterSceneWrite()
    {
        _projectPanel.MarkDirty();
        _assetCatalog.MarkDirty();
    }

    private static void HandleCommandHistoryChanged()
    {
        EditorSyncBridge.Instance.HasUnsavedSceneChanges = true;
    }

    private void HandleGlobalShortcuts()
    {
        var io = ImGui.GetIO();
        if (io.WantTextInput)
        {
            return;
        }

        if (io.KeyCtrl)
        {
            if (ImGui.IsKeyPressed(ImGuiKey.S))
            {
                if (io.KeyShift)
                {
                    _projectService.SaveProject();
                }
                else
                {
                    MainMenuBar.SaveSceneWithFallbackDialog(_sceneSession, _projectService, forceDialog: false, RefreshAfterSceneWrite);
                }

                return;
            }

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
