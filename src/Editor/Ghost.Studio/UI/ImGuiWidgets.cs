using Ghost.Core;
using Ghost.Studio.Backend;
using Hexa.NET.ImGui;
using System.Numerics;

namespace Ghost.Studio.UI;

/// <summary>
/// Interaction result returned by <see cref="ImGuiWidgets.TreeRow"/>.
/// </summary>
internal readonly struct TreeRowResult
{
    public readonly bool Clicked;
    public readonly bool DoubleClicked;
    public readonly bool ToggledExpand;

    public TreeRowResult(bool clicked, bool doubleClicked, bool toggledExpand)
    {
        Clicked = clicked;
        DoubleClicked = doubleClicked;
        ToggledExpand = toggledExpand;
    }
}

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
            if (ImGui.Button($"{EditorIcons.CLOSE}##{id}_clr", new Vector2(ImGui.GetFrameHeight(), ImGui.GetFrameHeight())))
            {
                query = string.Empty;
                changed = true;
            }
            ImGui.PopStyleColor();
        }

        return changed;
    }

    public static bool DrawTreeIndentAndChevron(int depth, float indentWidth, bool hasChildren, bool isExpanded, in EditorColorTokens tokens)
    {
        var rowHeight = ImGui.GetFrameHeight();
        var chevronWidth = rowHeight * 0.85f;
        var spacingX = ImGui.GetStyle().ItemSpacing.X;

        if (depth > 0)
        {
            ImGui.Dummy(new Vector2(depth * indentWidth, rowHeight));
            ImGui.SameLine(0.0f, 0.0f);
        }

        var toggled = false;
        if (hasChildren)
        {
            if (ImGui.InvisibleButton("##exp", new Vector2(chevronWidth, rowHeight)))
            {
                toggled = true;
            }

            var min = ImGui.GetItemRectMin();
            var max = ImGui.GetItemRectMax();
            var hovered = ImGui.IsItemHovered();
            var glyph = isExpanded ? EditorIcons.CHEVRON_DOWN : EditorIcons.CHEVRON_RIGHT;
            var glyphSize = ImGui.CalcTextSize(glyph);
            var glyphPos = new Vector2(min.X + (chevronWidth - glyphSize.X) * 0.5f, min.Y + (rowHeight - glyphSize.Y) * 0.5f);

            var drawList = ImGui.GetWindowDrawList();
            if (hovered)
            {
                drawList.AddRectFilled(min, max, ImGui.GetColorU32(tokens.ControlHovered), 3.0f);
            }

            var col = hovered ? tokens.TextPrimary : tokens.TextSecondary;
            drawList.AddText(glyphPos, ImGui.GetColorU32(col), glyph);
        }
        else
        {
            ImGui.Dummy(new Vector2(chevronWidth, rowHeight));
        }

        ImGui.SameLine(0.0f, MathF.Min(spacingX, 4.0f));
        return toggled;
    }

    public static TreeRowResult TreeRow(string rowId, string label, int depth, float indentWidth, bool hasChildren, bool isExpanded, bool isSelected, string? iconGlyph, Vector4 iconColor, in EditorColorTokens tokens)
    {
        var toggled = DrawTreeIndentAndChevron(depth, indentWidth, hasChildren, isExpanded, in tokens);
        var clicked = IconSelectableRow(rowId, label, iconGlyph, iconColor, isSelected, in tokens, out var doubleClicked);
        return new TreeRowResult(clicked, doubleClicked, toggled);
    }

    public static bool IconSelectableRow(string rowId, string label, string? iconGlyph, Vector4 iconColor, bool isSelected, in EditorColorTokens tokens, out bool doubleClicked)
    {
        var rowHeight = ImGui.GetFrameHeight();
        var clicked = ImGui.Selectable($"##{rowId}", isSelected, ImGuiSelectableFlags.AllowDoubleClick, new Vector2(0.0f, rowHeight));
        doubleClicked = clicked && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left);

        var rowMin = ImGui.GetItemRectMin();
        var rowMax = ImGui.GetItemRectMax();
        var fontSize = ImGui.GetFontSize();
        var textY = rowMin.Y + (rowHeight - fontSize) * 0.5f;
        var cursorX = rowMin.X + 4.0f;

        var drawList = ImGui.GetWindowDrawList();
        drawList.PushClipRect(rowMin, rowMax, true);

        if (!string.IsNullOrEmpty(iconGlyph))
        {
            var iconSlotWidth = MathF.Ceiling(fontSize * 1.15f);
            var iconSize = ImGui.CalcTextSize(iconGlyph);
            var iconX = cursorX + (iconSlotWidth - iconSize.X) * 0.5f;
            var iconY = rowMin.Y + (rowHeight - iconSize.Y) * 0.5f;
            drawList.AddText(new Vector2(iconX, iconY), ImGui.GetColorU32(iconColor), iconGlyph);
            cursorX += iconSlotWidth + 6.0f;
        }

        drawList.AddText(new Vector2(cursorX, textY), ImGui.GetColorU32(tokens.TextPrimary), label);
        drawList.PopClipRect();

        return clicked;
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
