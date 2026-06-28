using Ghost.AssetForge.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;

namespace Ghost.AssetForge.Views;

/// <summary>
/// Landing page for project management
/// </summary>
public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel { get; }

    public DashboardPage()
    {
        ViewModel = App.AppHost.Services.GetRequiredService<DashboardViewModel>();
        InitializeComponent();
    }

    private async void NewProjectButton_Click(object sender, RoutedEventArgs e)
    {
        var nameTextBox = new TextBox { Header = "Project Name", PlaceholderText = "MyProject" };
        var locationTextBox = new TextBox { Header = "Project Location", PlaceholderText = @"C:\GhostProjects" };
        var browseButton = new Button { Content = "Browse...", VerticalAlignment = VerticalAlignment.Bottom };

        var locationGrid = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, new ColumnDefinition { Width = GridLength.Auto } }, Margin = new Thickness(0, 12, 0, 0) };
        locationTextBox.Margin = new Thickness(0, 0, 8, 0);
        locationGrid.Children.Add(locationTextBox);
        Grid.SetColumn(locationTextBox, 0);

        browseButton.Click += async (s, ev) =>
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.FileTypeFilter.Add("*");
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance);
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                locationTextBox.Text = folder.Path;
            }
        };
        locationGrid.Children.Add(browseButton);
        Grid.SetColumn(browseButton, 1);

        var panel = new StackPanel { Spacing = 12 };
        panel.Children.Add(nameTextBox);
        panel.Children.Add(locationGrid);

        var dialog = new ContentDialog
        {
            Title = "Create New Project",
            Content = panel,
            PrimaryButtonText = "Create",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            var name = nameTextBox.Text.Trim();
            var location = locationTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(location))
            {
                try
                {
                    ViewModel.CreateProject(new KeyValuePair<string, string>(location, name));
                }
                catch (Exception ex)
                {
                    var errDialog = new ContentDialog
                    {
                        Title = "Error",
                        Content = $"Failed to create project: {ex.Message}",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await errDialog.ShowAsync();
                }
            }
        }
    }

    private async void OpenProjectButton_Click(object sender, RoutedEventArgs e)
    {
        var folderPicker = new Windows.Storage.Pickers.FolderPicker();
        folderPicker.FileTypeFilter.Add("*");
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance);
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
        var folder = await folderPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            try
            {
                ViewModel.OpenProject(folder.Path);
            }
            catch (Exception ex)
            {
                var errDialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"Failed to open project: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errDialog.ShowAsync();
            }
        }
    }

    private async void RecentProjectList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ProjectItem item)
        {
            try
            {
                ViewModel.OpenProject(item.Path);
            }
            catch (Exception ex)
            {
                var errDialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"Failed to open recent project: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errDialog.ShowAsync();
            }
        }
    }
}
