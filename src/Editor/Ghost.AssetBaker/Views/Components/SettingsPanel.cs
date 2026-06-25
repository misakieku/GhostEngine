using System;
using System.Collections.Generic;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Layout;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Ghost.AssetBaker.Models;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.AssetBaker.Views.Components;

public record SettingsPanelProps(
    QueuedAsset? Asset,
    Action<BakeSettings> OnSettingsChanged,
    Window WindowContext
);

public class SettingsPanel : Component<SettingsPanelProps>
{
    public override Element Render()
    {
        var asset = Props.Asset;

        if (asset == null)
        {
            return (Border(
                FlexColumn(
                    Icon(FontIcon("\uE713", fontSize: 36)) // Settings icon
                        .Foreground(Theme.DisabledText)
                        .HAlign(HorizontalAlignment.Center),
                    BodyStrong("No Asset Selected")
                        .HAlign(HorizontalAlignment.Center),
                    Caption("Select an asset from the queue on the left to configure its baking parameters.")
                        .Foreground(Theme.SecondaryText)
                        .TextAlignment(TextAlignment.Center)
                ) with
                {
                    RowGap = 8,
                    JustifyContent = FlexJustify.Center,
                    AlignItems = FlexAlign.Center
                }
            ) with
            {
                BorderThickness = 1,
                CornerRadius = 8,
                ThemeBindings = new Dictionary<string, ThemeRef> { { "BorderBrush", Theme.CardStroke } }
            })
            .Padding(24)
            .Background(Theme.CardBackground)
            .Flex(grow: 1, basis: 0);
        }

        var settings = asset.Settings;
        var compressionItems = new[] { "None", "Fast", "High" };
        var selectedCompressionIndex = (int)settings.Compression;

        // Custom Browse Folder Action
        var browseOutputFolder = async () =>
        {
            var picker = new Windows.Storage.Pickers.FolderPicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder;
            picker.FileTypeFilter.Add("*");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(Props.WindowContext);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                Props.OnSettingsChanged(settings with { OutputPath = folder.Path });
            }
        };

        return (Border(
            ScrollView(
                FlexColumn(
                    // Asset Info Header
                    FlexRow(
                        Icon(FontIcon("\uE946", fontSize: 18)) // Info icon
                            .Foreground(Theme.AccentText)
                            .VAlign(VerticalAlignment.Center),
                        Subtitle("Bake Parameters")
                            .VAlign(VerticalAlignment.Center)
                    ) with { ColumnGap = 8 },
                    
                    Border(Empty()).Height(1).Background(Theme.DividerStroke),

                    BodyStrong($"Target: {asset.Name}"),
                    Caption($"Type: {asset.Type} | Size: {asset.SizeFormatted}")
                        .Foreground(Theme.SecondaryText),

                    // Output Path Group
                    FlexColumn(
                        BodyStrong("Output Directory")
                            .Margin(bottom: 4),
                        FlexRow(
                            TextBox(settings.OutputPath, path => Props.OnSettingsChanged(settings with { OutputPath = path }))
                                .PlaceholderText("Output folder path")
                                .AutomationName("Output Directory Path")
                                .Flex(grow: 1, basis: 0),
                            Button("Browse...", () => _ = browseOutputFolder())
                                .Margin(left: 8)
                        ) with { AlignItems = FlexAlign.Center }
                    ),

                    // Compression Level Group
                    FlexColumn(
                        BodyStrong("Compression Level")
                            .Margin(bottom: 4),
                        ComboBox(
                            compressionItems, 
                            selectedCompressionIndex, 
                            idx => Props.OnSettingsChanged(settings with { Compression = (CompressionLevel)idx })
                        )
                    ),

                    // Type Specific Settings Group
                    (Border(
                        FlexColumn(
                            BodyStrong("Type-Specific Options")
                                .Margin(bottom: 8),

                            asset.Type switch
                            {
                                AssetType.Mesh => FlexColumn(
                                    CheckBox(
                                        isChecked: settings.OptimizeMesh,
                                        onIsCheckedChanged: val => Props.OnSettingsChanged(settings with { OptimizeMesh = val }),
                                        label: "Optimize Mesh (Cache & Index layouts)"
                                    ),
                                    CheckBox(
                                        isChecked: settings.GenerateLods,
                                        onIsCheckedChanged: val => Props.OnSettingsChanged(settings with { GenerateLods = val }),
                                        label: "Generate Level of Details (LODs)"
                                    )
                                ) with { RowGap = 8 },

                                AssetType.Texture => FlexColumn(
                                    CheckBox(
                                        isChecked: settings.GenerateMipmaps,
                                        onIsCheckedChanged: val => Props.OnSettingsChanged(settings with { GenerateMipmaps = val }),
                                        label: "Generate Mipmap chain"
                                    )
                                ),

                                _ => Caption("No custom settings available for this asset type.")
                                    .Foreground(Theme.DisabledText)
                            }
                        )
                    ) with
                    {
                        BorderThickness = 1,
                        CornerRadius = 6,
                        ThemeBindings = new Dictionary<string, ThemeRef> { { "BorderBrush", Theme.CardStroke } }
                    })
                    .Padding(12)
                    .Background(Theme.ControlFillSecondary)

                ) with { RowGap = 16 }
            )
        ) with
        {
            BorderThickness = 1,
            CornerRadius = 8,
            ThemeBindings = new Dictionary<string, ThemeRef> { { "BorderBrush", Theme.CardStroke } }
        })
        .Padding(20)
        .Background(Theme.CardBackground)
        .Flex(grow: 1, basis: 0);
    }
}
