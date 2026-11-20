using Ghost.AssetForge.Core.Services;
using Ghost.Studio.Backend;
using Hexa.NET.ImGui;
using System.Numerics;

namespace Ghost.Studio.UI;

/// <summary>
/// Base class for file-backed asset editor tabs providing relative path headers, focused <c>Ctrl+S</c>
/// save handling, and standard scrollable body + action footer layout.
/// </summary>
internal abstract class AssetEditorWindow : EditorWindow
{
    public string AssetPath { get; }

    public string RelativePath { get; }

    protected string StatusMessage { get; set; } = string.Empty;

    public override bool ShowInWindowMenu => false;

    public override bool ScaleDefaultSizeByDpi => true;

    public override Vector2 DefaultSize => new(500.0f, 620.0f);

    protected AssetEditorWindow(string assetPath, ProjectService projectService, string windowTypePrefix)
        : base(Path.GetFileName(assetPath), $"{windowTypePrefix}:{Path.GetFullPath(assetPath)}", openByDefault: true)
    {
        AssetPath = Path.GetFullPath(assetPath);
        RelativePath = ComputeRelativePath(AssetPath, projectService);
    }

    protected override void OnDraw(ImGuiContextManager contextManager)
    {
        var io = ImGui.GetIO();
        if (IsFocused && !io.WantTextInput && io.KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.S))
        {
            Save();
        }

        OnDrawAssetEditor(contextManager);
    }

    protected abstract void OnDrawAssetEditor(ImGuiContextManager contextManager);

    public abstract bool Save();

    public virtual void Revert()
    {
    }

    protected void DrawStandardHeader(ImGuiContextManager contextManager, string subtitle, in EditorColorTokens tokens)
    {
        var fileName = Path.GetFileName(AssetPath);
        ImGui.PushFont(contextManager.HeadingFont, EditorTheme.FONT_SIZE_HEADING);
        ImGui.TextUnformatted(IsDirty ? $"{fileName} *" : fileName);
        ImGui.PopFont();

        if (!string.IsNullOrEmpty(RelativePath))
        {
            ImGui.TextColored(tokens.TextSecondary, RelativePath);
        }

        if (!string.IsNullOrEmpty(subtitle))
        {
            ImGui.PushFont(contextManager.MonospaceFont, EditorTheme.FONT_SIZE_MONO);
            ImGui.TextColored(tokens.TextDisabled, subtitle);
            ImGui.PopFont();
        }
    }

    private static string ComputeRelativePath(string fullPath, ProjectService projectService)
    {
        var primaryDir = projectService.AssetDirectories.LastOrDefault();
        if (!string.IsNullOrEmpty(primaryDir))
        {
            return Path.GetRelativePath(Path.GetFullPath(primaryDir), fullPath).Replace('\\', '/');
        }

        return fullPath;
    }
}
