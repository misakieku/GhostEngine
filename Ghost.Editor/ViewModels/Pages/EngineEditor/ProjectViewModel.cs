using CommunityToolkit.Mvvm.ComponentModel;
using Ghost.Editor.Core;
using Ghost.Editor.Core.AssetHandle;
using Ghost.Editor.Models;
using System.Collections.ObjectModel;

namespace Ghost.Editor.ViewModels.Pages.EngineEditor;

internal partial class ProjectViewModel : ObservableObject
{
    public ObservableCollection<ExplorerItem> SubDirectories
    {
        get;
    } = new();

    [ObservableProperty]
    public partial ObservableCollection<ExplorerItem> DirectoryAssets
    {
        get;
        set;
    } = new();

    [ObservableProperty]
    public partial ExplorerItem? SelectedDirectory
    {
        get;
        set;
    }

    [ObservableProperty]
    public partial ExplorerItem? SelectedAsset
    {
        get;
        set;
    }

    public ProjectViewModel()
    {
        var assetsRootItem = new ExplorerItem("Assets", Path.Combine(EditorApplication.CurrentProjectPath, EditorApplication.ASSETS_FOLDER_NAME), true);
        LoadSubFolderRecursive(ref assetsRootItem);

        SubDirectories.Add(assetsRootItem);
    }

    private static void LoadSubFolderRecursive(ref ExplorerItem parentItem)
    {
        foreach (var directory in Directory.EnumerateDirectories(parentItem.FullName))
        {
            var item = new ExplorerItem(Path.GetFileName(directory), directory, true);
            LoadSubFolderRecursive(ref item);

            parentItem.Children ??= new();
            parentItem.Children.Add(item);
        }
    }

    public static Task<ExplorerItem?> FindNodeIterative(ExplorerItem root, Func<ExplorerItem, bool> predicate)
    {
        var stack = new Stack<ExplorerItem>();
        stack.Push(root);

        return Task.Run(() =>
        {
            while (stack.Count > 0)
            {
                var node = stack.Pop();
                if (predicate(node))
                {
                    return node;
                }

                if (node.Children == null || node.Children.Count == 0)
                {
                    continue;
                }

                for (var i = node.Children.Count - 1; i >= 0; i--)
                {
                    stack.Push(node.Children[i]);
                }
            }

            return null;
        });
    }

    private void NavigateToDirectory(string? path)
    {
        App.Window?.DispatcherQueue.TryEnqueue(async () =>
        {
            DirectoryAssets.Clear();

            if (!Directory.Exists(path))
            {
                return;
            }

            foreach (var directory in Directory.EnumerateDirectories(path))
            {
                var directoryItem = new ExplorerItem(Path.GetFileName(directory), directory, true);
                DirectoryAssets.Add(directoryItem);
            }

            foreach (var file in Directory.EnumerateFiles(path))
            {
                var fileItem = new ExplorerItem(Path.GetFileName(file), file, false);
                DirectoryAssets.Add(fileItem);
            }

            SelectedDirectory = await FindNodeIterative(SubDirectories[0], x => x.FullName == path);
        });
    }

    public void OpenSelected()
    {
        if (SelectedAsset == null)
        {
            return;
        }

        if (SelectedAsset.IsDirectory)
        {
            NavigateToDirectory(SelectedAsset.FullName);
        }
        else
        {
            AssetDatabase.OpenAsset(SelectedAsset.FullName);
        }
    }

    partial void OnSelectedDirectoryChanged(ExplorerItem? value)
    {
        if (value == null)
        {
            return;
        }

        DirectoryAssets.Clear();
        NavigateToDirectory(value.FullName);
    }
}