using Ghost.Editor.Core.Selection;
using Ghost.Editor.Core.Sync;
using Ghost.Engine.Systems;
using Ghost.Entities;
using Ghost.Studio.Backend;
using Ghost.Studio.Rendering;
using Ghost.Studio.UI;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using Hexa.NET.ImPlot;
using System.Numerics;

namespace Ghost.Studio.Systems;

[UpdateAfter<EditorEcsSyncSystem>]
[UpdateBefore<EditorCameraControllerSystem>]
[UpdateBefore<RenderSystemGroup>]
internal sealed class EditorImGuiSystem : SystemBase
{
    private readonly ImGuiContextManager _contextManager;
    private readonly ImGuiSdlInputBridge _inputBridge;
    private readonly ImGuiPostRenderCommand _postRenderCommand;
    private readonly SceneViewportTarget _viewportTarget;
    private readonly SceneViewportPanel _sceneViewportPanel;

    private bool _showSceneViewport = true;
    private bool _showThemePreview = true;
    private bool _showDemoWindow;
    private bool _showPlotDemo;

    public EditorImGuiSystem(ImGuiContextManager contextManager, ImGuiSdlInputBridge inputBridge, ImGuiPostRenderCommand postRenderCommand, SceneViewportTarget viewportTarget, EditorViewportInputState viewportInputState)
    {
        _contextManager = contextManager;
        _inputBridge = inputBridge;
        _postRenderCommand = postRenderCommand;
        _viewportTarget = viewportTarget;
        _sceneViewportPanel = new SceneViewportPanel(viewportTarget, viewportInputState);
    }

    protected override void OnUpdate(scoped in SystemUpdateAPI systemAPI)
    {
        _contextManager.MakeCurrent();
        _inputBridge.BeginFrame(systemAPI.Time.DeltaTime);

        ImGui.NewFrame();
        ImGuizmo.BeginFrame();

        DrawMainMenuBar();

        ImGui.DockSpaceOverViewport(0, ImGui.GetMainViewport(), ImGuiDockNodeFlags.PassthruCentralNode);

        var renderedSceneTexture = false;
        if (_showSceneViewport)
        {
            renderedSceneTexture = _sceneViewportPanel.Draw(_contextManager, systemAPI.World.EntityManager, ref _showSceneViewport);
        }

        if (_showThemePreview)
        {
            DrawDesignSystemPreview(systemAPI.Time.DeltaTime);
        }

        if (_showDemoWindow)
        {
            ImGui.ShowDemoWindow(ref _showDemoWindow);
        }

        if (_showPlotDemo)
        {
            ImPlot.ShowDemoWindow(ref _showPlotDemo);
        }

        ImGui.Render();

        var externalTex = renderedSceneTexture ? _viewportTarget.TextureHandle : default;
        var slot = _postRenderCommand.GetSnapshotSlot(systemAPI.Time.FrameIndex);
        slot.Capture(ImGui.GetDrawData(), externalTex);
    }

    private void DrawMainMenuBar()
    {
        if (!ImGui.BeginMainMenuBar())
        {
            return;
        }

        if (ImGui.BeginMenu("View"))
        {
            if (ImGui.BeginMenu("Theme"))
            {
                if (ImGui.MenuItem("Dark (Unity Default)", string.Empty, _contextManager.ThemePreset == EditorThemePreset.Dark))
                {
                    _contextManager.SetTheme(EditorThemePreset.Dark);
                }

                if (ImGui.MenuItem("Light", string.Empty, _contextManager.ThemePreset == EditorThemePreset.Light))
                {
                    _contextManager.SetTheme(EditorThemePreset.Light);
                }

                ImGui.EndMenu();
            }

            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu("Windows"))
        {
            ImGui.MenuItem("Scene", string.Empty, ref _showSceneViewport);
            ImGui.MenuItem("Scene Entities", string.Empty, ref _showThemePreview);
            ImGui.Separator();
            ImGui.MenuItem("Dear ImGui Demo", string.Empty, ref _showDemoWindow);
            ImGui.MenuItem("ImPlot Demo", string.Empty, ref _showPlotDemo);
            ImGui.EndMenu();
        }

        ImGui.EndMainMenuBar();
    }

    private void DrawDesignSystemPreview(float deltaTime)
    {
        ImGui.SetNextWindowPos(new Vector2(24.0f, 52.0f), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(320.0f, 420.0f), ImGuiCond.FirstUseEver);

        if (!ImGui.Begin("Scene Entities", ref _showThemePreview))
        {
            ImGui.End();
            return;
        }

        ref readonly var tokens = ref _contextManager.Tokens;
        var snapshot = EditorSyncBridge.Instance.LatestSnapshot;

        ImGui.PushFont(_contextManager.HeadingFont, EditorTheme.FONT_SIZE_HEADING);
        ImGui.TextUnformatted("Editor World Snapshot");
        ImGui.PopFont();

        ImGui.PushFont(_contextManager.MonospaceFont, EditorTheme.FONT_SIZE_MONO);
        var fps = deltaTime > 0.00001f ? 1.0f / deltaTime : 0.0f;
        ImGui.TextColored(tokens.TextSecondary, $"{deltaTime * 1000.0f:0.00} ms  |  {fps:0} FPS  |  DPI {_contextManager.DpiScale:0.00}x");
        ImGui.PopFont();

        ImGui.Separator();

        if (snapshot != null)
        {
            var activeId = EditorSelection.Instance.ActiveEntity?.Id ?? -1;
            for (var i = 0; i < snapshot.Entities.Count; i++)
            {
                var entity = snapshot.Entities[i];
                var selected = entity.Id == activeId;
                if (ImGui.Selectable($"{entity.Name}##{entity.Id}", selected))
                {
                    EditorSelection.Instance.Select(entity);
                }
            }
        }

        ImGui.End();
    }
}
