using CommunityToolkit.Mvvm.ComponentModel;
using Ghost.Editor.Core;
using Ghost.Editor.Core.AssetHandle;
using Ghost.Editor.Core.Contracts;
using Ghost.Editor.Core.Utilities;
using Ghost.Editor.Models;
using System.Collections.ObjectModel;

namespace Ghost.Editor.ViewModels.Controls;

internal partial class ProjectBrowserViewModel : ObservableObject
{
    private readonly IInspectorService _inspectorService;

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
        get; set;
    } = string.Empty;

    public ProjectBrowserViewModel(IInspectorService inspectorService)
    {
        _inspectorService = inspectorService;

        var assetsRootItem = new ExplorerItem(EditorApplication.ASSETS_FOLDER_NAME, Path.Combine(EditorApplication.CurrentProjectPath, EditorApplication.ASSETS_FOLDER_NAME), true);
        LoadSubFolderRecursive(assetsRootItem);

        Directories.Add(assetsRootItem);
    }

    private void LoadSubFolderRecursive(ExplorerItem parentItem)
    {
        foreach (var directory in Directory.EnumerateDirectories(parentItem.FullName))
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
            if (Path.GetExtension(file) == FileExtensions.META_FILE_EXTENSION)
            {
                continue;
            }

            var fileItem = new ExplorerItem(Path.GetFileName(file), file, false);
            Files.Add(fileItem);
        }

        CurrentDirectoryPath = path;
    }

    internal (ExplorerItem?, int) OpenSelected()
    {
        if (SelectedItem == null)
        {
            return (null, 0);
        }

        if (SelectedItem.IsDirectory)
        {
            NavigateToDirectory(SelectedItem.FullName);
            SelectedItem = _pathToDirectoryItemMap[SelectedItem.FullName];
            return (SelectedItem, 0);
        }
        else
        {
            AssetService.OpenAsset(SelectedItem.FullName);
            return (null, 1);
        }
    }
}
