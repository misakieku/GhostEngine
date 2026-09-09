using Ghost.AssetForge.Core.Services;
using Ghost.AssetForge.ViewModels;
using Ghost.AssetForge.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;

namespace Ghost.AssetForge;

/// <summary>
/// Main Shell Window of the application
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }
    private readonly ProjectService _projectService;

    public MainWindow(MainViewModel viewModel, ProjectService projectService)
    {
        ViewModel = viewModel;
        _projectService = projectService;
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;

        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        ContentFrame.Navigated += ContentFrame_Navigated;
        _projectService.OnProjectLoaded += OnProjectLoaded;

        // Set default page
        NavView.SelectedItem = NavView.MenuItems.OfType<NavigationViewItem>().First();
        NavigateToPage(ViewModel.CurrentPageName);
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.CurrentPageName))
        {
            NavigateToPage(ViewModel.CurrentPageName);
        }
    }

    private void OnProjectLoaded()
    {
        NoProjectOverlay.DispatcherQueue.TryEnqueue(() => UpdateOverlayVisibility());
    }

    private void ContentFrame_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        UpdateOverlayVisibility();
    }

    private void UpdateOverlayVisibility()
    {
        var pageType = ContentFrame.Content?.GetType();
        var requiresProject = pageType == typeof(ProjectExplorerPage) || pageType == typeof(PackingPage);
        var hasProject = _projectService.CurrentProject != null;

        if (requiresProject && !hasProject)
        {
            NoProjectOverlay.Visibility = Visibility.Visible;
        }
        else
        {
            NoProjectOverlay.Visibility = Visibility.Collapsed;
        }
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ViewModel.CurrentPageName = "Settings";
        }
        else if (args.SelectedItemContainer is NavigationViewItem item)
        {
            ViewModel.CurrentPageName = item.Tag?.ToString() ?? "Dashboard";
        }
    }

    private void NavigateToPage(string pageName)
    {
        var menuItems = NavView.MenuItems.OfType<NavigationViewItem>();
        var footerItems = NavView.FooterMenuItems.OfType<NavigationViewItem>();
        var allItems = menuItems.Concat(footerItems);

        var targetItem = allItems.FirstOrDefault(i => i.Tag?.ToString() == pageName);
        if (targetItem != null && (NavigationViewItem)NavView.SelectedItem != targetItem)
        {
            NavView.SelectedItem = targetItem;
        }

        var pageType = pageName switch
        {
            "Dashboard" => typeof(DashboardPage),
            "ProjectExplorer" => typeof(ProjectExplorerPage),
            "Packing" => typeof(PackingPage),
            _ => typeof(DashboardPage)
        };

        if (ContentFrame.CurrentSourcePageType != pageType)
        {
            ContentFrame.Navigate(pageType);
        }
    }

    private async void NewProjectButton_Click(object sender, RoutedEventArgs e)
    {
        var dashboardVm = App.AppHost.Services.GetRequiredService<DashboardViewModel>();

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
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
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
            XamlRoot = ContentFrame.XamlRoot
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
                    dashboardVm.CreateProject(new System.Collections.Generic.KeyValuePair<string, string>(location, name));
                }
                catch (Exception ex)
                {
                    var errDialog = new ContentDialog
                    {
                        Title = "Error",
                        Content = $"Failed to create project: {ex.Message}",
                        CloseButtonText = "OK",
                        XamlRoot = ContentFrame.XamlRoot
                    };
                    await errDialog.ShowAsync();
                }
            }
        }
    }

    private async void OpenProjectButton_Click(object sender, RoutedEventArgs e)
    {
        var dashboardVm = App.AppHost.Services.GetRequiredService<DashboardViewModel>();

        var folderPicker = new Windows.Storage.Pickers.FolderPicker();
        folderPicker.FileTypeFilter.Add("*");
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
        var folder = await folderPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            try
            {
                dashboardVm.OpenProject(folder.Path);
            }
            catch (Exception ex)
            {
                var errDialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"Failed to open project: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = ContentFrame.XamlRoot
                };
                await errDialog.ShowAsync();
            }
        }
    }
}
