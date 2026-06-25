using Ghost.AssetBaker.Models;
using Ghost.AssetBaker.Services;
using Ghost.AssetBaker.Views.Components;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Input;
using Microsoft.UI.Reactor.Layout;
using Microsoft.UI.Xaml;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.AssetBaker.Views;

public record WorkspaceViewProps(
    Window WindowContext,
    BakeSettings GlobalSettings
);

public class WorkspaceView : Component<WorkspaceViewProps>
{
    public override Element Render()
    {
        var service = BakeService.Instance;
        var (selectedAssetId, setSelectedAssetId) = UseState<Guid?>(null);
        var (dummy, setDummy) = UseReducer(0);

        // Subscribe to BakeService changes to trigger re-renders
        UseEffect(() =>
        {
            void handler() => setDummy(d => d + 1);
            service.OnStateChanged += handler;
            return () => service.OnStateChanged -= handler;
        }, Array.Empty<object>());

        var selectedAsset = selectedAssetId.HasValue
            ? service.Queue.FirstOrDefault(a => a.Id == selectedAssetId.Value)
            : null;

        // Custom File Picker Action
        async Task openFiles()
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder;
            picker.FileTypeFilter.Add(".fbx");
            picker.FileTypeFilter.Add(".obj");
            picker.FileTypeFilter.Add(".gltf");
            picker.FileTypeFilter.Add(".glb");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".tga");
            picker.FileTypeFilter.Add(".dds");
            picker.FileTypeFilter.Add(".hlsl");
            picker.FileTypeFilter.Add(".shader");
            picker.FileTypeFilter.Add(".wav");
            picker.FileTypeFilter.Add(".mp3");
            picker.FileTypeFilter.Add("*");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(Props.WindowContext);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var files = await picker.PickMultipleFilesAsync();
            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    service.AddFile(file.Path);
                }
            }
        }

        // Custom Folder Picker Action (Import folder contents)
        async Task openFolder()
        {
            var picker = new Windows.Storage.Pickers.FolderPicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder;
            picker.FileTypeFilter.Add("*");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(Props.WindowContext);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                var files = Directory.GetFiles(folder.Path, "*.*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    service.AddFile(file);
                }
            }
        }

        // Drag & Drop event handlers
        void onDragOver(DragTargetArgs args)
        {
            args.AcceptedOperation = DragOperations.Copy;
            args.UIOverride.Caption = "Add to Baker Queue";
            args.UIOverride.IsCaptionVisible = true;
            args.UIOverride.IsContentVisible = true;
            args.UIOverride.IsGlyphVisible = true;
        }

        void onDrop(DragTargetArgs args)
        {
            if (args.Data.TryGetSafeLocalFiles(out var files) && files != null)
            {
                foreach (var file in files)
                {
                    if (Directory.Exists(file.Path))
                    {
                        var folderFiles = Directory.GetFiles(file.Path, "*.*", SearchOption.AllDirectories);
                        foreach (var subFile in folderFiles)
                        {
                            service.AddFile(subFile);
                        }
                    }
                    else
                    {
                        service.AddFile(file.Path);
                    }
                }
            }
        }

        var isQueueEmpty = service.Queue.Count == 0;
        var hasPending = service.Queue.Any(a => a.Status == AssetState.Pending);

        return Grid(
            columns: [GridSize.Star(3), GridSize.Star(2)],
            rows: [GridSize.Star()],

            // Left Pane (Queue list and toolbar)
            (FlexColumn(
                RenderToolbar(service, isQueueEmpty, hasPending, openFiles, openFolder, setSelectedAssetId)
                    .Flex(shrink: 0),
                RenderQueueList(service, selectedAssetId, setSelectedAssetId, onDragOver, onDrop)
            ) with
            { RowGap = 12 })
            .Margin(right: 12)
            .Grid(column: 0),

            // Right Pane (Settings panel + Monospaced console output)
            RenderDetailsAndLogs(selectedAsset, service)
                .Grid(column: 1)
        ).Margin(16);
    }

    private Element RenderToolbar(
        BakeService service,
        bool isQueueEmpty,
        bool hasPending,
        Func<Task> openFiles,
        Func<Task> openFolder,
        Action<Guid?> setSelectedAssetId)
    {
        var bakeButtonEnabled = hasPending && !service.IsBaking;

        return FlexRow(
            Button("Add Files...", () => _ = openFiles())
                .IsEnabled(!service.IsBaking),
            Button("Add Folder...", () => _ = openFolder())
                .IsEnabled(!service.IsBaking),
            Button("Clear Completed", () => service.ClearCompleted())
                .IsEnabled(!service.IsBaking && !isQueueEmpty),
            Button("Clear All", () =>
            {
                setSelectedAssetId(null);
                service.ClearAll();
            })
                .IsEnabled(!service.IsBaking && !isQueueEmpty),

            // Spacer to right-align the Bake button
            Empty().Flex(grow: 1, basis: 0),

            Button("Bake Queue", () =>
            {
                _ = Task.Run(() => service.BakeQueueAsync(Props.GlobalSettings));
            })
            .AccentButton()
            .IsEnabled(bakeButtonEnabled)
        ) with
        {
            ColumnGap = 8,
            AlignItems = FlexAlign.Center
        };
    }

    private Element RenderQueueList(
        BakeService service,
        Guid? selectedAssetId,
        Action<Guid?> setSelectedAssetId,
        Action<DragTargetArgs> onDragOver,
        Action<DragTargetArgs> onDrop)
    {
        Element queueListContent;
        if (service.Queue.Count == 0)
        {
            // Empty placeholder dropzone
            queueListContent = (Border(
                FlexColumn(
                    Icon(FontIcon("\uE105", fontSize: 48))
                        .Foreground(Theme.DisabledText)
                        .HAlign(HorizontalAlignment.Center),
                    BodyStrong("Drag & Drop Assets Here")
                        .HAlign(HorizontalAlignment.Center),
                    Caption("Supports Mesh (.fbx, .obj), Texture (.png, .dds), Shaders (.hlsl), and Audio (.wav)")
                        .Foreground(Theme.SecondaryText)
                        .TextAlignment(TextAlignment.Center)
                ) with
                {
                    RowGap = 12,
                    JustifyContent = FlexJustify.Center,
                    AlignItems = FlexAlign.Center
                }
            ) with
            {
                BorderThickness = 2,
                CornerRadius = 8,
                ThemeBindings = new Dictionary<string, ThemeRef> { { "BorderBrush", Theme.CardStroke } }
            })
            .Background(Theme.SubtleFill)
            .Padding(24)
            .Flex(grow: 1, basis: 0);
        }
        else
        {
            // Scrollable list
            queueListContent = ScrollView(
                FlexColumn(
                    ForEach(service.Queue, asset =>
                        Component<AssetItemCard, AssetItemCardProps>(new AssetItemCardProps(
                            Asset: asset,
                            IsSelected: selectedAssetId == asset.Id,
                            OnSelect: () => setSelectedAssetId(asset.Id),
                            OnDelete: () =>
                            {
                                if (selectedAssetId == asset.Id) setSelectedAssetId(null);
                                service.RemoveFile(asset.Id);
                            }
                        )).WithKey(asset.Id.ToString())
                    )
                ) with
                { RowGap = 8 }
            )
            .Flex(grow: 1, basis: 0);
        }

        return Border(queueListContent)
            .OnDragOver(onDragOver)
            .OnDrop(onDrop)
            .Flex(grow: 1, basis: 0);
    }

    private Element RenderDetailsAndLogs(QueuedAsset? selectedAsset, BakeService service)
    {
        return Grid(
            columns: [GridSize.Star()],
            rows: [GridSize.Star(4), GridSize.Star(3)],

            Component<SettingsPanel, SettingsPanelProps>(new SettingsPanelProps(
                Asset: selectedAsset,
                OnSettingsChanged: settings => service.UpdateAssetSettings(selectedAsset!.Id, settings),
                WindowContext: Props.WindowContext
            ))
            .Grid(row: 0)
            .Margin(bottom: 12),

            Component<LogConsole, LogConsoleProps>(new LogConsoleProps(
                Logs: service.Logs.ToArray()
            ))
            .Grid(row: 1)
        );
    }
}
