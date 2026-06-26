using System;
using System.Collections.Generic;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Layout;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Ghost.AssetBaker.Models;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.AssetBaker.Views;

public record GlobalSettingsViewProps(
    BakeSettings Settings,
    Action<BakeSettings> OnSettingsChanged,
    Window WindowContext
);

public class GlobalSettingsView : Component<GlobalSettingsViewProps>
{
    public override Element Render()
    {
        var settings = Props.Settings;

        // Custom Browse Folder Action
        async Task browseOutputFolder()
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
        }

        var (targetPlatformIndex, setTargetPlatformIndex) = UseState(0);
        var platforms = new[] { "PC (Direct3D 12 - x64)", "Xbox Series X/S (D3D12)", "Mobile (Vulkan - ARM64)" };

        return ScrollView(
            FlexColumn(
                // Page Header
                Heading("Global Settings"),
                Caption("Configure defaults and output targets for the asset baking pipeline.")
                    .Foreground(Theme.SecondaryText),
                
                Border(Empty()).Height(1).Background(Theme.DividerStroke),

                // Group 1: Output Location
                CardGroup(
                    "Output Configuration",
                    FlexColumn(
                        BodyStrong("Default Output Directory")
                            .Margin(bottom: 4),
                        FlexRow(
                            TextBox(settings.OutputPath, path => Props.OnSettingsChanged(settings with { OutputPath = path }))
                                .PlaceholderText("Output folder path")
                                .AutomationName("Default Output Directory Path")
                                .Flex(grow: 1, basis: 0),
                            Button("Browse...", () => _ = browseOutputFolder())
                                .Margin(left: 8)
                        ) with { AlignItems = FlexAlign.Center },
                        Caption("The fallback directory where baked assets will be saved if a specific asset output is not overridden.")
                            .Foreground(Theme.SecondaryText)
                            .Margin(top: 4)
                    )
                ),

                // Group 3: Targets and Formats
                CardGroup(
                    "Target Hardware Profile",
                    FlexColumn(
                        BodyStrong("Target Platform Profile")
                            .Margin(bottom: 4),
                        ComboBox(platforms, targetPlatformIndex, setTargetPlatformIndex),
                        Caption("Baking processes, compression standards (e.g. BC7/ASTC), and alignments will be compiled to best suit this platform.")
                            .Foreground(Theme.SecondaryText)
                            .Margin(top: 4)
                    )
                )

            ) with { RowGap = 24 }
        ).Padding(horizontal: 24, vertical: 8);
    }

    private static Element CardGroup(string title, Element content)
    {
        return FlexColumn(
            SubHeading(title)
                .Margin(bottom: 8),
            (Border(content) with
            {
                BorderThickness = 1,
                CornerRadius = 8,
                ThemeBindings = new Dictionary<string, ThemeRef> { { "BorderBrush", Theme.CardStroke } }
            })
            .Padding(16)
            .Background(Theme.CardBackground)
        ) with { RowGap = 4 };
    }
}
