using Ghost.AssetBaker.Models;
using Ghost.Core;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.AssetBaker.Views.Components;

public record AssetItemCardProps(
    QueuedAsset Asset,
    bool IsSelected,
    Action OnSelect,
    Action OnDelete
);

public class AssetItemCard : Component<AssetItemCardProps>
{
    public override Element Render()
    {
        var asset = Props.Asset;
        var (isHovered, setIsHovered) = UseState(false);

        // Get type-specific icon and color
        var (icon, iconColor) = GetIconDetails(asset.Type);

        // Build status UI
        var statusUi = asset.Status switch
        {
            AssetState.Pending => TextBlock("Pending")
                .Foreground(Theme.SecondaryText)
                .VAlign(VerticalAlignment.Center),

            AssetState.Baking => FlexRow(
                ProgressRing(asset.Progress).IsActive(true).Width(16).Height(16).VAlign(VerticalAlignment.Center),
                TextBlock($" {asset.Progress:F0}%")
                    .Foreground(Theme.AccentText)
                    .VAlign(VerticalAlignment.Center)
            ) with
            { ColumnGap = 6 },

            AssetState.Success => FlexRow(
                Icon(FontIcon("\uE8FB")) // Checkmark
                    .Foreground(Theme.SystemSuccess)
                    .VAlign(VerticalAlignment.Center),
                TextBlock("Success")
                    .Foreground(Theme.SystemSuccess)
                    .VAlign(VerticalAlignment.Center)
            ) with
            { ColumnGap = 4 },

            AssetState.Failed => FlexRow(
                Icon(FontIcon("\uE711")) // Error cross
                    .Foreground(Theme.SystemCritical)
                    .VAlign(VerticalAlignment.Center),
                TextBlock("Failed")
                    .Foreground(Theme.SystemCritical)
                    .VAlign(VerticalAlignment.Center)
            ) with
            { ColumnGap = 4 },

            _ => Empty()
        };

        // Accent border styling for selection
        var borderBrush = Props.IsSelected ? Theme.Accent : Theme.CardStroke;
        var bgBrush = Props.IsSelected
            ? Theme.ControlFillSecondary
            : (isHovered ? Theme.SubtleFill : Theme.CardBackground);

        return (Border(
            Grid(
                columns: [GridSize.Auto, GridSize.Star(), GridSize.Auto, GridSize.Auto],
                rows: [GridSize.Auto],

                // 1. Asset Type Icon
                Border(
                    Icon(FontIcon(icon, fontSize: 20))
                        .Foreground(iconColor)
                        .HAlign(HorizontalAlignment.Center)
                        .VAlign(VerticalAlignment.Center)
                )
                .Width(40)
                .Height(40)
                .CornerRadius(6)
                .Background(Theme.ControlFillTertiary)
                .Grid(column: 0)
                .Margin(right: 12),

                // 2. Name & Details
                FlexColumn(
                    BodyStrong(asset.Name)
                        .TextWrapping(TextWrapping.NoWrap)
                        .TextTrimming(TextTrimming.CharacterEllipsis),
                    Caption(asset.SizeFormatted)
                        .Foreground(Theme.SecondaryText)
                )
                .Grid(column: 1)
                .VAlign(VerticalAlignment.Center),

                // 3. Status Badge
                statusUi
                    .Grid(column: 2)
                    .VAlign(VerticalAlignment.Center)
                    .Margin(left: 12),

                // 4. Delete action
                (asset.Status == AssetState.Success
                    ? Empty()
                    : Button(
                        Icon(FontIcon("\uE107", fontSize: 12)) // Trash icon
                            .Foreground(Theme.SecondaryText),
                        Props.OnDelete
                    )
                    .SubtleButton()
                    .AutomationName("Delete from queue")
                )
                .Grid(column: 3)
                .VAlign(VerticalAlignment.Center)
                .Margin(left: 8)
            )
        ) with
        {
            BorderThickness = 1,
            CornerRadius = 8,
            ThemeBindings = new Dictionary<string, ThemeRef> { { "BorderBrush", borderBrush } }
        })
        .Background(bgBrush)
        .Padding(horizontal: 16, vertical: 10)
        .OnPointerEntered((s, e) => setIsHovered(true))
        .OnPointerExited((s, e) => setIsHovered(false))
        .OnTapped((s, e) => Props.OnSelect());
    }

    private static (string Glyph, string Color) GetIconDetails(AssetType type)
    {
        return type switch
        {
            AssetType.Mesh => ("\uF158", "#0078D4"),     // Cube / 3D element (Blue)
            AssetType.Texture => ("\uE91B", "#107C41"),  // Photo / Image (Green)
            AssetType.Shader => ("\uE943", "#D83B01"),   // Code / Settings (Orange/Red)
            AssetType.Audio => ("\uE8D6", "#8764B8"),    // Sound / Audio (Purple)
            _ => ("\uE8A5", "#605E5C")                   // Document (Gray)
        };
    }
}
