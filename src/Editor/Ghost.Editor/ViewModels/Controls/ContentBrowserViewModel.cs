using CommunityToolkit.Mvvm.ComponentModel;
using Ghost.Editor.Core;
using Ghost.Editor.Core.Assets;
using Ghost.Editor.Core.Contracts;
using Ghost.Editor.Core.Utilities;
using Ghost.Editor.Models;
using Ghost.Engine.Streaming;
using Microsoft.UI.Dispatching;
using System.Collections.ObjectModel;

namespace Ghost.Editor.ViewModels.Controls;

internal partial class ContentBrowserViewModel : ObservableObject
{
    private readonly IInspectorService _inspectorService;
    private readonly IAssetRegistry _assetRegistry;
    private readonly DispatcherQueue _dispatcherQueue;

    private readonly Dictionary<string, ExplorerItem> _pathToDirectoryItemMap = new();
    private ExplorerItem? _selectedItem;

    public ObservableCollection<ExplorerItem> Directories
    {
        get;
    } = new();

    public ObservableCollection<ExplorerItem> Files
    {
        get;
    } = new();

    public ExplorerItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            // TODO: Resolve inspector by reading metadata from selected asset
            _selectedItem = value;
        }
    }

    public string CurrentDirectoryPath
    {
        get;
        set => field = PathUtility.Normalize(value);
    } = string.Empty;

    public ContentBrowserViewModel(IInspectorService inspectorService, IAssetRegistry assetRegistry)
    {
        _inspectorService = inspectorService;
        _assetRegistry = assetRegistry;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        var assetsRootItem = new ExplorerItem(EditorApplication.ASSETS_FOLDER_NAME, EditorApplication.AssetsFolderPath, true);
        LoadSubFolderRecursive(assetsRootItem);

        Directories.Add(assetsRootItem);

        _assetRegistry.OnAssetChanged += OnAssetChanged;
    }

    private void OnAssetChanged(object? sender, AssetChangedEventArgs e)
    {
        if (e.AssetPath.EndsWith(FileExtensions.META_FILE_EXTENSION))
        {
            return;
        }

        var fullPath = PathUtility.Normalize(e.AssetPath);
        var dirPath = Path.GetDirectoryName(fullPath);

        if (string.Equals(dirPath, CurrentDirectoryPath, StringComparison.OrdinalIgnoreCase))
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                if (e.ChangeType == AssetChangeType.Created || e.ChangeType == AssetChangeType.Renamed)
                {
                    if (e.ChangeType == AssetChangeType.Renamed && e.OldAssetPath != null)
                    {
                        var oldFullPath = PathUtility.Normalize(e.OldAssetPath);
                        var oldItem = Files.FirstOrDefault(f => string.Equals(f.Path, oldFullPath, StringComparison.OrdinalIgnoreCase));
                        if (oldItem != null) Files.Remove(oldItem);
                    }

                    if (!Files.Any(f => string.Equals(f.Path, fullPath, StringComparison.OrdinalIgnoreCase)))
                    {
                        var isDir = Directory.Exists(fullPath);
                        var assetType = AssetType.Unknown;
                        if (!isDir)
                        {
                            var ext = Path.GetExtension(fullPath);
                            if (AssetHandlerRegistry.TryGetHandlerInfoByExtension(ext, out var info))
                            {
                                assetType = info.RuntimeAssetType;
                            }
                        }

                        Files.Add(new ExplorerItem(Path.GetFileName(fullPath), fullPath, isDir, assetType));
                    }
                }
                else if (e.ChangeType == AssetChangeType.Deleted)
                {
                    var item = Files.FirstOrDefault(f => string.Equals(f.Path, fullPath, StringComparison.OrdinalIgnoreCase));
                    if (item != null)
                    {
                        Files.Remove(item);
                    }
                }
            });
        }
    }

    private void LoadSubFolderRecursive(ExplorerItem parentItem)
    {
        foreach (var directory in Directory.EnumerateDirectories(parentItem.Path))
        {
            var item = new ExplorerItem(Path.GetFileName(directory), directory, true);
            LoadSubFolderRecursive(item);

            _pathToDirectoryItemMap[directory] = item;

            parentItem.Children ??= new();
            parentItem.Children.Add(item);
        }
    }

    internal void NavigateToDirectory(string? path)
    {
        Files.Clear();

        if (!Directory.Exists(path))
        {
            return;
        }

        foreach (var directory in Directory.EnumerateDirectories(path))
        {
            var directoryItem = new ExplorerItem(Path.GetFileName(directory), directory, true);
            Files.Add(directoryItem);
        }

        foreach (var file in Directory.EnumerateFiles(path))
        {
            if (file.EndsWith(FileExtensions.META_FILE_EXTENSION))
            {
                continue;
            }

            var ext = Path.GetExtension(file);
            var assetType = AssetHandlerRegistry.TryGetHandlerInfoByExtension(ext, out var handlerInfo) ? handlerInfo.RuntimeAssetType : AssetType.Unknown;

            var fileItem = new ExplorerItem(Path.GetFileName(file), file, false, assetType);
            Files.Add(fileItem);
        }

        CurrentDirectoryPath = Path.GetFullPath(path);
    }

    internal async ValueTask<(ExplorerItem?, int)> OpenSelected()
    {
        if (SelectedItem == null)
        {
            return (null, 0);
        }

        if (SelectedItem.IsDirectory)
        {
            NavigateToDirectory(SelectedItem.Path);
            SelectedItem = _pathToDirectoryItemMap[SelectedItem.Path];
            return (SelectedItem, 0);
        }
        else
        {
            var result = await _assetRegistry.OpenAssetAsync(SelectedItem.Path);
            if (result.IsFailure)
            {
                return (null, -1);
            }

            return (null, 1);
        }
    }
}
