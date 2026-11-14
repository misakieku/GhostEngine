using Hexa.NET.ImGui;
using Hexa.NET.ImPlot;
using System.Numerics;

namespace Ghost.Studio.Backend;

internal enum EditorThemePreset : byte
{
    Dark = 0,
    Light = 1,
}

/// <summary>
/// Semantic color tokens inspired by Unity Editor Foundations (neutral grayscale chrome + reserved accent/status hues).
/// </summary>
internal readonly struct EditorColorTokens
{
    public readonly Vector4 Canvas;
    public readonly Vector4 Panel;
    public readonly Vector4 PanelElevated;
    public readonly Vector4 HeaderBar;
    public readonly Vector4 Recessed;
    public readonly Vector4 Control;
    public readonly Vector4 ControlHovered;
    public readonly Vector4 ControlActive;
    public readonly Vector4 Border;
    public readonly Vector4 BorderSubtle;
    public readonly Vector4 TextPrimary;
    public readonly Vector4 TextSecondary;
    public readonly Vector4 TextDisabled;
    public readonly Vector4 Accent;
    public readonly Vector4 AccentHovered;
    public readonly Vector4 AccentActive;
    public readonly Vector4 AccentSubtle;
    public readonly Vector4 SemanticError;
    public readonly Vector4 SemanticWarning;
    public readonly Vector4 SemanticSuccess;
    public readonly Vector4 PlayModeTint;
    public readonly Vector4 TableRowAlt;
    public readonly Vector4 ModalDim;

    public EditorColorTokens(Vector4 canvas, Vector4 panel, Vector4 panelElevated, Vector4 headerBar, Vector4 recessed, Vector4 control, Vector4 controlHovered, Vector4 controlActive, Vector4 border, Vector4 borderSubtle, Vector4 textPrimary, Vector4 textSecondary, Vector4 textDisabled, Vector4 accent, Vector4 accentHovered, Vector4 accentActive, Vector4 accentSubtle, Vector4 semanticError, Vector4 semanticWarning, Vector4 semanticSuccess, Vector4 playModeTint, Vector4 tableRowAlt, Vector4 modalDim)
    {
        Canvas = canvas;
        Panel = panel;
        PanelElevated = panelElevated;
        HeaderBar = headerBar;
        Recessed = recessed;
        Control = control;
        ControlHovered = controlHovered;
        ControlActive = controlActive;
        Border = border;
        BorderSubtle = borderSubtle;
        TextPrimary = textPrimary;
        TextSecondary = textSecondary;
        TextDisabled = textDisabled;
        Accent = accent;
        AccentHovered = accentHovered;
        AccentActive = accentActive;
        AccentSubtle = accentSubtle;
        SemanticError = semanticError;
        SemanticWarning = semanticWarning;
        SemanticSuccess = semanticSuccess;
        PlayModeTint = playModeTint;
        TableRowAlt = tableRowAlt;
        ModalDim = modalDim;
    }

    public static EditorColorTokens CreateDark()
    {
        return new EditorColorTokens(
            canvas: FromRgba(0x1C, 0x1C, 0x1C),
            panel: FromRgba(0x28, 0x28, 0x28),
            panelElevated: FromRgba(0x31, 0x31, 0x31),
            headerBar: FromRgba(0x22, 0x22, 0x22),
            recessed: FromRgba(0x1E, 0x1E, 0x1E),
            control: FromRgba(0x3C, 0x3C, 0x3C),
            controlHovered: FromRgba(0x48, 0x48, 0x48),
            controlActive: FromRgba(0x52, 0x52, 0x52),
            border: FromRgba(0x16, 0x16, 0x16),
            borderSubtle: FromRgba(0x3E, 0x3E, 0x3E),
            textPrimary: FromRgba(0xE4, 0xE4, 0xE4),
            textSecondary: FromRgba(0xA6, 0xA6, 0xA6),
            textDisabled: FromRgba(0x6E, 0x6E, 0x6E),
            accent: FromRgba(0x2F, 0x6F, 0xC8),
            accentHovered: FromRgba(0x3F, 0x82, 0xDF),
            accentActive: FromRgba(0x26, 0x5D, 0xA8),
            accentSubtle: FromRgba(0x2F, 0x6F, 0xC8, 0.28f),
            semanticError: FromRgba(0xE0, 0x52, 0x52),
            semanticWarning: FromRgba(0xE6, 0xB8, 0x3A),
            semanticSuccess: FromRgba(0x5A, 0xBF, 0x6B),
            playModeTint: FromRgba(0x25, 0x4A, 0x73),
            tableRowAlt: FromRgba(0xFF, 0xFF, 0xFF, 0.025f),
            modalDim: FromRgba(0x00, 0x00, 0x00, 0.52f)
        );
    }

    public static EditorColorTokens CreateLight()
    {
        return new EditorColorTokens(
            canvas: FromRgba(0xB8, 0xB8, 0xB8),
            panel: FromRgba(0xC8, 0xC8, 0xC8),
            panelElevated: FromRgba(0xD4, 0xD4, 0xD4),
            headerBar: FromRgba(0xC0, 0xC0, 0xC0),
            recessed: FromRgba(0xE2, 0xE2, 0xE2),
            control: FromRgba(0xDB, 0xDB, 0xDB),
            controlHovered: FromRgba(0xE7, 0xE7, 0xE7),
            controlActive: FromRgba(0xCE, 0xCE, 0xCE),
            border: FromRgba(0x9A, 0x9A, 0x9A),
            borderSubtle: FromRgba(0xAD, 0xAD, 0xAD),
            textPrimary: FromRgba(0x16, 0x16, 0x16),
            textSecondary: FromRgba(0x4A, 0x4A, 0x4A),
            textDisabled: FromRgba(0x7A, 0x7A, 0x7A),
            accent: FromRgba(0x2B, 0x67, 0xBE),
            accentHovered: FromRgba(0x3A, 0x7A, 0xD6),
            accentActive: FromRgba(0x21, 0x54, 0x9C),
            accentSubtle: FromRgba(0x2B, 0x67, 0xBE, 0.22f),
            semanticError: FromRgba(0xC6, 0x28, 0x28),
            semanticWarning: FromRgba(0xC4, 0x7F, 0x00),
            semanticSuccess: FromRgba(0x2E, 0x7D, 0x32),
            playModeTint: FromRgba(0x8E, 0xB0, 0xD6),
            tableRowAlt: FromRgba(0x00, 0x00, 0x00, 0.03f),
            modalDim: FromRgba(0x00, 0x00, 0x00, 0.35f)
        );
    }

    public static Vector4 FromRgba(byte r, byte g, byte b, float alpha = 1.0f)
    {
        return new Vector4(r / 255.0f, g / 255.0f, b / 255.0f, alpha);
    }
}

/// <summary>
/// Applies calibrated Unity Editor Foundations metrics, typography rhythm, and neutral color tokens to ImGui & ImPlot.
/// </summary>
internal static class EditorTheme
{
    public const float FONT_SIZE_REGULAR = 13.5f;
    public const float FONT_SIZE_HEADING = 14.5f;
    public const float FONT_SIZE_MONO = 13.0f;

    public static EditorColorTokens GetTokens(EditorThemePreset preset)
    {
        return preset == EditorThemePreset.Light ? EditorColorTokens.CreateLight() : EditorColorTokens.CreateDark();
    }

    public static void Apply(ImGuiStylePtr style, ImPlotStylePtr plotStyle, EditorThemePreset preset)
    {
        var t = GetTokens(preset);
        var isLight = preset == EditorThemePreset.Light;

        ApplyMetrics(style);
        ApplyImGuiColors(style, in t, isLight);
        ApplyImPlotStyle(plotStyle, in t);
    }

    private static void ApplyMetrics(ImGuiStylePtr style)
    {
        style.WindowPadding = new Vector2(8.0f, 8.0f);
        style.FramePadding = new Vector2(6.0f, 3.5f);
        style.CellPadding = new Vector2(6.0f, 3.0f);
        style.ItemSpacing = new Vector2(6.0f, 4.0f);
        style.ItemInnerSpacing = new Vector2(4.0f, 4.0f);
        style.IndentSpacing = 14.0f;
        style.ColumnsMinSpacing = 6.0f;

        style.ScrollbarSize = 12.0f;
        style.GrabMinSize = 8.0f;

        style.WindowBorderSize = 1.0f;
        style.ChildBorderSize = 1.0f;
        style.PopupBorderSize = 1.0f;
        style.FrameBorderSize = 1.0f;
        style.TabBorderSize = 0.0f;
        style.TabBarBorderSize = 1.0f;
        style.TabBarOverlineSize = 2.0f;
        style.DockingSeparatorSize = 2.0f;
        style.SeparatorTextBorderSize = 1.0f;

        style.WindowRounding = 3.0f;
        style.ChildRounding = 2.0f;
        style.PopupRounding = 3.0f;
        style.FrameRounding = 3.0f;
        style.ScrollbarRounding = 6.0f;
        style.GrabRounding = 2.0f;
        style.TabRounding = 3.0f;

        style.WindowTitleAlign = new Vector2(0.0f, 0.5f);
        style.WindowMenuButtonPosition = ImGuiDir.Right;
        style.ColorButtonPosition = ImGuiDir.Right;
        style.ButtonTextAlign = new Vector2(0.5f, 0.5f);
        style.SelectableTextAlign = new Vector2(0.0f, 0.5f);
        style.SeparatorTextPadding = new Vector2(8.0f, 3.0f);

        style.AntiAliasedLines = true;
        style.AntiAliasedLinesUseTex = true;
        style.AntiAliasedFill = true;
    }

    private static void ApplyImGuiColors(ImGuiStylePtr style, in EditorColorTokens t, bool isLight)
    {
        var colors = style.Colors;
        var accentSoftHover = WithAlpha(t.Accent, 0.36f);
        var accentActiveFill = WithAlpha(t.Accent, 0.45f);
        var hoverNeutral = WithAlpha(t.ControlHovered, isLight ? 0.65f : 0.70f);
        var scrollGrab = WithAlpha(t.TextSecondary, isLight ? 0.45f : 0.35f);
        var scrollGrabHover = WithAlpha(t.TextSecondary, isLight ? 0.65f : 0.55f);
        var scrollGrabActive = WithAlpha(t.TextPrimary, 0.65f);

        colors[(int)ImGuiCol.Text] = t.TextPrimary;
        colors[(int)ImGuiCol.TextDisabled] = t.TextDisabled;
        colors[(int)ImGuiCol.WindowBg] = t.Panel;
        colors[(int)ImGuiCol.ChildBg] = t.Panel;
        colors[(int)ImGuiCol.PopupBg] = t.PanelElevated;
        colors[(int)ImGuiCol.Border] = t.BorderSubtle;
        colors[(int)ImGuiCol.BorderShadow] = Vector4.Zero;

        colors[(int)ImGuiCol.FrameBg] = t.Recessed;
        colors[(int)ImGuiCol.FrameBgHovered] = t.PanelElevated;
        colors[(int)ImGuiCol.FrameBgActive] = t.Control;

        colors[(int)ImGuiCol.TitleBg] = t.HeaderBar;
        colors[(int)ImGuiCol.TitleBgActive] = t.HeaderBar;
        colors[(int)ImGuiCol.TitleBgCollapsed] = t.HeaderBar;
        colors[(int)ImGuiCol.MenuBarBg] = t.HeaderBar;

        colors[(int)ImGuiCol.ScrollbarBg] = t.HeaderBar;
        colors[(int)ImGuiCol.ScrollbarGrab] = scrollGrab;
        colors[(int)ImGuiCol.ScrollbarGrabHovered] = scrollGrabHover;
        colors[(int)ImGuiCol.ScrollbarGrabActive] = scrollGrabActive;

        colors[(int)ImGuiCol.CheckMark] = isLight ? t.Accent : t.TextPrimary;
        colors[(int)ImGuiCol.SliderGrab] = t.TextSecondary;
        colors[(int)ImGuiCol.SliderGrabActive] = t.Accent;

        colors[(int)ImGuiCol.Button] = t.Control;
        colors[(int)ImGuiCol.ButtonHovered] = t.ControlHovered;
        colors[(int)ImGuiCol.ButtonActive] = t.ControlActive;

        colors[(int)ImGuiCol.Header] = t.Control;
        colors[(int)ImGuiCol.HeaderHovered] = hoverNeutral;
        colors[(int)ImGuiCol.HeaderActive] = t.AccentSubtle;

        colors[(int)ImGuiCol.Separator] = t.Border;
        colors[(int)ImGuiCol.SeparatorHovered] = t.Accent;
        colors[(int)ImGuiCol.SeparatorActive] = t.AccentHovered;

        colors[(int)ImGuiCol.ResizeGrip] = WithAlpha(t.TextDisabled, 0.25f);
        colors[(int)ImGuiCol.ResizeGripHovered] = t.Accent;
        colors[(int)ImGuiCol.ResizeGripActive] = t.AccentActive;
        colors[(int)ImGuiCol.InputTextCursor] = t.TextPrimary;

        colors[(int)ImGuiCol.Tab] = t.HeaderBar;
        colors[(int)ImGuiCol.TabHovered] = t.PanelElevated;
        colors[(int)ImGuiCol.TabSelected] = t.Panel;
        colors[(int)ImGuiCol.TabSelectedOverline] = t.Accent;
        colors[(int)ImGuiCol.TabDimmed] = t.HeaderBar;
        colors[(int)ImGuiCol.TabDimmedSelected] = t.Panel;
        colors[(int)ImGuiCol.TabDimmedSelectedOverline] = t.BorderSubtle;

        colors[(int)ImGuiCol.DockingPreview] = WithAlpha(t.Accent, 0.50f);
        colors[(int)ImGuiCol.DockingEmptyBg] = t.Canvas;

        colors[(int)ImGuiCol.PlotLines] = t.Accent;
        colors[(int)ImGuiCol.PlotLinesHovered] = t.SemanticWarning;
        colors[(int)ImGuiCol.PlotHistogram] = t.AccentHovered;
        colors[(int)ImGuiCol.PlotHistogramHovered] = t.SemanticSuccess;

        colors[(int)ImGuiCol.TableHeaderBg] = t.HeaderBar;
        colors[(int)ImGuiCol.TableBorderStrong] = t.Border;
        colors[(int)ImGuiCol.TableBorderLight] = t.BorderSubtle;
        colors[(int)ImGuiCol.TableRowBg] = Vector4.Zero;
        colors[(int)ImGuiCol.TableRowBgAlt] = t.TableRowAlt;

        colors[(int)ImGuiCol.TextLink] = t.AccentHovered;
        colors[(int)ImGuiCol.TextSelectedBg] = accentActiveFill;
        colors[(int)ImGuiCol.TreeLines] = WithAlpha(t.TextDisabled, 0.55f);
        colors[(int)ImGuiCol.DragDropTarget] = t.Accent;
        colors[(int)ImGuiCol.NavCursor] = t.Accent;
        colors[(int)ImGuiCol.NavWindowingHighlight] = accentSoftHover;
        colors[(int)ImGuiCol.NavWindowingDimBg] = t.ModalDim;
        colors[(int)ImGuiCol.ModalWindowDimBg] = t.ModalDim;
    }

    private static void ApplyImPlotStyle(ImPlotStylePtr plotStyle, in EditorColorTokens t)
    {
        var colors = plotStyle.Colors;
        colors[(int)ImPlotCol.Line] = t.AccentHovered;
        colors[(int)ImPlotCol.Fill] = t.AccentSubtle;
        colors[(int)ImPlotCol.MarkerOutline] = t.Accent;
        colors[(int)ImPlotCol.MarkerFill] = t.AccentHovered;
        colors[(int)ImPlotCol.ErrorBar] = t.TextSecondary;
        colors[(int)ImPlotCol.FrameBg] = t.Panel;
        colors[(int)ImPlotCol.Bg] = t.Recessed;
        colors[(int)ImPlotCol.Border] = t.BorderSubtle;
        colors[(int)ImPlotCol.LegendBg] = WithAlpha(t.PanelElevated, 0.94f);
        colors[(int)ImPlotCol.LegendBorder] = t.BorderSubtle;
        colors[(int)ImPlotCol.LegendText] = t.TextPrimary;
        colors[(int)ImPlotCol.TitleText] = t.TextPrimary;
        colors[(int)ImPlotCol.InlayText] = t.TextPrimary;
        colors[(int)ImPlotCol.AxisText] = t.TextSecondary;
        colors[(int)ImPlotCol.AxisGrid] = WithAlpha(t.TextPrimary, 0.08f);
        colors[(int)ImPlotCol.AxisTick] = WithAlpha(t.TextSecondary, 0.40f);
        colors[(int)ImPlotCol.AxisBg] = Vector4.Zero;
        colors[(int)ImPlotCol.AxisBgHovered] = t.Control;
        colors[(int)ImPlotCol.AxisBgActive] = t.ControlHovered;
        colors[(int)ImPlotCol.Selection] = t.Accent;
        colors[(int)ImPlotCol.Crosshairs] = WithAlpha(t.TextPrimary, 0.50f);

        plotStyle.PlotBorderSize = 1.0f;
        plotStyle.MinorAlpha = 0.25f;
        plotStyle.PlotPadding = new Vector2(8.0f, 8.0f);
        plotStyle.LabelPadding = new Vector2(4.0f, 4.0f);
        plotStyle.LegendPadding = new Vector2(8.0f, 6.0f);
    }

    public static Vector4 WithAlpha(Vector4 color, float alpha)
    {
        return new Vector4(color.X, color.Y, color.Z, alpha);
    }
}
