using CommunityToolkit.Mvvm.ComponentModel;
using Ghost.Data.Services;
using Ghost.Editor.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

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
        if (ProjectService.CurrentProject.Metadata == null)
        {
            throw new InvalidOperationException("Current project is not set.");
        }

        var assetsItem = new ExplorerItem("Assets", Path.Combine(Path.GetDirectoryName(ProjectService.CurrentProject.Path)!, ProjectService.ASSETS_FOLDER), true);
        LoadSubFolderRecursive(ref assetsItem);

        SubDirectories.Add(assetsItem);
    }

    private void LoadSubFolderRecursive(ref ExplorerItem parentItem)
    {
        foreach (var directory in Directory.EnumerateDirectories(parentItem.Path))
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

            SelectedDirectory = await FindNodeIterative(SubDirectories[0], x => x.Path == path);
        });
    }

    public void NavigateToSelected()
    {
        if (SelectedAsset == null || !SelectedAsset.IsDirectory)
        {
            return;
        }

        NavigateToDirectory(SelectedAsset.Path);
    }

    partial void OnSelectedDirectoryChanged(ExplorerItem? value)
    {
        DirectoryAssets.Clear();
        if (value == null)
        {
            return;
        }

        NavigateToDirectory(value.Path);
    }
}