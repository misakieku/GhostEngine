using Ghost.AssetForge.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;

namespace Ghost.AssetForge.Views;

/// <summary>
/// Explorer page for project assets
/// </summary>
public sealed partial class ProjectExplorerPage : Page
{
    public ProjectExplorerViewModel ViewModel { get; }
    private readonly Inspector.InspectorDrawerRegistry _drawerRegistry;

    public ProjectExplorerPage()
    {
        ViewModel = App.AppHost.Services.GetRequiredService<ProjectExplorerViewModel>();
        _drawerRegistry = App.AppHost.Services.GetRequiredService<Inspector.InspectorDrawerRegistry>();
        InitializeComponent();

        ViewModel.Folders.CollectionChanged += (s, e) => UpdateTreeView();
        UpdateTreeView();

        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        RebuildInspector();
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProjectExplorerViewModel.SelectedSettings))
        {
            RebuildInspector();
        }
    }

    private void RebuildInspector()
    {
        DynamicInspectorPanel.Children.Clear();
        if (ViewModel.SelectedSettings != null)
        {
            var element = _drawerRegistry.DrawObject(ViewModel.SelectedSettings);
            DynamicInspectorPanel.Children.Add(element);
        }
    }

    private void FolderTreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        if (args.InvokedItem is TreeViewNode node && node.Content is FolderItem folder)
        {
            ViewModel.SelectedFolderNode = folder;
        }
    }

    private void UpdateTreeView()
    {
        FolderTreeView.RootNodes.Clear();
        foreach (var folder in ViewModel.Folders)
        {
            var rootNode = CreateNode(folder);
            FolderTreeView.RootNodes.Add(rootNode);
        }
    }

    private TreeViewNode CreateNode(FolderItem folder)
    {
        var node = new TreeViewNode
        {
            Content = folder
        };

        foreach (var child in folder.Children)
        {
            node.Children.Add(CreateNode(child));
        }

        return node;
    }



    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder;
        picker.FileTypeFilter.Add("*");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var files = await picker.PickMultipleFilesAsync();
        if (files != null && files.Count > 0)
        {
            var paths = new System.Collections.Generic.List<string>();
            foreach (var file in files)
            {
                paths.Add(file.Path);
            }
            ViewModel.ImportFilesCommand.Execute(paths);
        }
    }

    private void AssetGridView_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
        e.DragUIOverride.Caption = "Import to current folder";
        e.DragUIOverride.IsCaptionVisible = true;
        e.DragUIOverride.IsContentVisible = true;
        e.DragUIOverride.IsGlyphVisible = true;
    }

    private async void AssetGridView_Drop(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
        {
            var items = await e.DataView.GetStorageItemsAsync();
            if (items != null && items.Count > 0)
            {
                var paths = new System.Collections.Generic.List<string>();
                foreach (var item in items)
                {
                    if (item is Windows.Storage.StorageFile file)
                    {
                        paths.Add(file.Path);
                    }
                }
                if (paths.Count > 0)
                {
                    ViewModel.ImportFilesCommand.Execute(paths);
                }
            }
        }
    }
}

#region Helper Value Converters

public class BoolToVisibilityConverter : IValueConverter
{
    public bool IsInverse { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b)
        {
            var show = IsInverse ? !b : b;
            return show ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class NullToVisibilityConverter : IValueConverter
{
    public bool IsInverse { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var isNull = value == null;
        if (parameter?.ToString() == "Inverse") isNull = !isNull;

        var show = IsInverse ? isNull : !isNull;
        return show ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class ItemCountConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int count)
        {
            return $"{count} items";
        }
        return "0 items";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

#endregion
