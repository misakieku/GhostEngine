using Microsoft.UI.Reactor.Core;

namespace Ghost.Studio.DesignSystem;

public static class GhostColors
{
    public static readonly ThemeRef RootBackground = Theme.SolidBackground;
    public static readonly ThemeRef PanelBackground = Theme.Ref("SolidBackgroundFillColorTertiaryBrush");
    public static readonly ThemeRef CardBackground = Theme.CardBackground;
    public static readonly ThemeRef CardHeaderBackground = Theme.Ref("SubtleFillColorSecondaryBrush");
    public static readonly ThemeRef CardHover = Theme.ControlFillSecondary;
    public static readonly ThemeRef CardSelected = Theme.Accent;
    public static readonly ThemeRef SubtleSelected = Theme.Ref("SubtleFillColorSecondaryBrush");
    public static readonly ThemeRef TransparentHitTarget = Theme.Ref("SubtleFillColorTransparentBrush");
    public static readonly ThemeRef OverlayBackground = Theme.Ref("AcrylicBackgroundFillColorDefaultBrush");
    public static readonly ThemeRef OverlayBorder = Theme.Ref("SurfaceStrokeColorFlyoutBrush");
    public static readonly ThemeRef SubtleBorder = Theme.DividerStroke;
    public static readonly ThemeRef CardBorder = Theme.CardStroke;
    public static readonly ThemeRef AccentPrimary = Theme.AccentText;
    public static readonly ThemeRef TextPrimary = Theme.PrimaryText;
    public static readonly ThemeRef TextSecondary = Theme.SecondaryText;
    public static readonly ThemeRef TextTertiary = Theme.TertiaryText;
    public static readonly ThemeRef TextOnAccent = Theme.Ref("TextOnAccentFillColorPrimaryBrush");
    public static readonly ThemeRef StatusSuccess = Theme.SystemSuccess;
    public static readonly ThemeRef StatusWarning = Theme.SystemCaution;
    public static readonly ThemeRef StatusError = Theme.SystemCritical;
}
