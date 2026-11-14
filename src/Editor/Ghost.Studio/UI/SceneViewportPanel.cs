using Ghost.Editor.Core.Sync;
using Ghost.Entities;
using Ghost.Studio.Backend;
using Ghost.Studio.Rendering;
using Hexa.NET.ImGui;
using System.Numerics;

namespace Ghost.Studio.UI;

/// <summary>
/// Displays the offscreen Scene View render target and captures viewport hover/focus and resize requests.
/// </summary>
internal sealed class SceneViewportPanel
{
    private readonly SceneViewportTarget _viewportTarget;
    private readonly EditorViewportInputState _inputState;

    public SceneViewportPanel(SceneViewportTarget viewportTarget, EditorViewportInputState inputState)
    {
        _viewportTarget = viewportTarget;
        _inputState = inputState;
    }

    public unsafe bool Draw(ImGuiContextManager contextManager, EntityManager em, ref bool isOpen)
    {
        var usedViewportTexture = false;

        ImGui.SetNextWindowPos(new Vector2(360.0f, 52.0f), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(1180.0f, 780.0f), ImGuiCond.FirstUseEver);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        var visible = ImGui.Begin("Scene", ref isOpen, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        ImGui.PopStyleVar();

        if (!visible)
        {
            _inputState.IsHovered = false;
            _inputState.IsFocused = false;
            ImGui.End();
            return false;
        }

        _inputState.IsHovered = ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
        _inputState.IsFocused = ImGui.IsWindowFocused();

        var avail = ImGui.GetContentRegionAvail();
        var cursorScreen = ImGui.GetCursorScreenPos();
        _inputState.ViewportMin = cursorScreen;
        _inputState.ViewportSize = avail;

        var width = (uint)MathF.Max(0.0f, MathF.Floor(avail.X));
        var height = (uint)MathF.Max(0.0f, MathF.Floor(avail.Y));
        if (width >= 64 && height >= 64)
        {
            _viewportTarget.RequestResize(width, height);
        }

        if (_viewportTarget.IsReady && avail.X >= 1.0f && avail.Y >= 1.0f)
        {
            var texRef = new ImTextureRef(null, new ImTextureID((ulong)_viewportTarget.BindlessSrvIndex));
            ImGui.Image(texRef, avail);
            usedViewportTexture = true;
        }
        else
        {
            ref readonly var tokens = ref contextManager.Tokens;
            ImGui.SetCursorPos(new Vector2(16.0f, 16.0f));
            ImGui.TextColored(tokens.TextSecondary, "Initializing Scene Viewport...");
        }

        DrawOverlayHud(contextManager, em, cursorScreen);

        ImGui.End();
        return usedViewportTexture;
    }

    private unsafe void DrawOverlayHud(ImGuiContextManager contextManager, EntityManager em, Vector2 viewportOrigin)
    {
        ref readonly var tokens = ref contextManager.Tokens;
        var dpi = contextManager.DpiScale;
        var pad = 8.0f * dpi;

        var overlayPos = new Vector2(viewportOrigin.X + pad, viewportOrigin.Y + pad);
        var drawList = ImGui.GetWindowDrawList();

        ref var cam = ref em.GetOrCreateSingleton(EditorCamera.Default);
        var entityCount = EditorSyncBridge.Instance.LatestSnapshot?.EntityCount ?? 0;

        ImGui.PushFont(contextManager.MonospaceFont, EditorTheme.FONT_SIZE_MONO);
        var label = $"{_viewportTarget.Width}x{_viewportTarget.Height}  |  Entities: {entityCount}  |  Speed: {cam.moveSpeed:0.0}  |  RMB+WASD Fly  Alt+LMB Orbit  MMB Pan  F Frame";
        var textSize = ImGui.CalcTextSize(label);
        var bgMin = overlayPos;
        var bgMax = new Vector2(bgMin.X + textSize.X + 14.0f * dpi, bgMin.Y + textSize.Y + 8.0f * dpi);

        var bgCol = ImGui.ColorConvertFloat4ToU32(EditorTheme.WithAlpha(tokens.HeaderBar, 0.78f));
        var borderCol = ImGui.ColorConvertFloat4ToU32(tokens.BorderSubtle);
        var textCol = ImGui.ColorConvertFloat4ToU32(tokens.TextSecondary);

        drawList.AddRectFilled(bgMin, bgMax, bgCol, 4.0f * dpi);
        drawList.AddRect(bgMin, bgMax, borderCol, 4.0f * dpi);
        drawList.AddText(new Vector2(bgMin.X + 7.0f * dpi, bgMin.Y + 4.0f * dpi), textCol, label);
        ImGui.PopFont();
    }
}
