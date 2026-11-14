using Ghost.Core;
using Ghost.Studio.Backend;
using Hexa.NET.ImGui;
using System.Numerics;

namespace Ghost.Studio.UI;

/// <summary>
/// Shared ImGui helper widgets styled with <see cref="EditorColorTokens"/>.
/// </summary>
internal static class ImGuiWidgets
{
    public static bool ToggleButton(string label, bool isActive, in EditorColorTokens tokens, Vector2 size = default)
    {
        if (isActive)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, tokens.Accent);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, tokens.AccentHovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, tokens.AccentActive);
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.96f, 0.96f, 0.96f, 1.0f));
        }

        var clicked = size == default ? ImGui.Button(label) : ImGui.Button(label, size);

        if (isActive)
        {
            ImGui.PopStyleColor(4);
        }

        return clicked;
    }

    public static bool SearchField(string id, string hint, ref string query, float width, in EditorColorTokens tokens)
    {
        var changed = false;
        var hasText = !string.IsNullOrEmpty(query);
        var clearBtnWidth = hasText ? ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.X : 0.0f;
        var inputWidth = width > 0.0f ? MathF.Max(40.0f, width - clearBtnWidth) : -clearBtnWidth;

        ImGui.SetNextItemWidth(inputWidth);
        if (ImGui.InputTextWithHint(id, hint, ref query, 256))
        {
            changed = true;
        }

        if (hasText)
        {
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, tokens.TextSecondary);
            if (ImGui.Button($"X##{id}_clr", new Vector2(ImGui.GetFrameHeight(), ImGui.GetFrameHeight())))
            {
                query = string.Empty;
                changed = true;
            }
            ImGui.PopStyleColor();
        }

        return changed;
    }

    public static void StatusBadge(string text, in Vector4 color)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.TextUnformatted(text);
        ImGui.PopStyleColor();
    }

    public static Vector4 GetLogColor(LogLevel level, in EditorColorTokens tokens)
    {
        return level switch
        {
            LogLevel.Error or LogLevel.Fatal => tokens.SemanticError,
            LogLevel.Warning => tokens.SemanticWarning,
            LogLevel.Debug => tokens.TextDisabled,
            _ => tokens.TextPrimary,
        };
    }

    public static string GetLogTag(LogLevel level)
    {
        return level switch
        {
            LogLevel.Fatal => "[FTL]",
            LogLevel.Error => "[ERR]",
            LogLevel.Warning => "[WRN]",
            LogLevel.Debug => "[DBG]",
            _ => "[INF]",
        };
    }
}
