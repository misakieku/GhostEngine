using Ghost.Data.Models;
using Ghost.Data.Services;
using Ghost.Editor.AppStates;
using Ghost.Editor.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace Ghost.Editor.View.Pages.Landing;

internal sealed partial class OpenProjectPage : Page
{
    private readonly ProjectService _projectService;
    private readonly StackedNotificationService _notificationService;
    private readonly AppStateService _stateService;

    public readonly ObservableCollection<ProjectMetadataInfo> projects = new();

    public OpenProjectPage()
    {
        _notificationService = App.GetService<StackedNotificationService>();
        _projectService = App.GetService<ProjectService>();
        _stateService = App.GetService<AppStateService>();
        InitializeComponent();
    }

    private void UpdateEmptyPlaceHolderVisibility()
    {
        EmptyPlaceHolder.Visibility = projects.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        projects.Clear();
        await foreach (var projectInfo in _projectService.LoadAllProjectAsync())
        {
            var metadata = await ProjectService.LoadMetadataAsync(projectInfo.MetadataPath);
            if (metadata == null)
            {
                continue;
            }

            projects.Add(new(projectInfo.MetadataPath, metadata));
        }

        UpdateEmptyPlaceHolderVisibility();
    }

    private void ProjectContainer_DragEnter(object sender, DragEventArgs e)
    {
        DragVisual.Visibility = Visibility.Visible;
        EmptyPlaceHolder.Visibility = Visibility.Collapsed;
    }

    private void ProjectContainer_DragLeave(object sender, DragEventArgs e)
    {
        DragVisual.Visibility = Visibility.Collapsed;
        UpdateEmptyPlaceHolderVisibility();
    }

    private void ProjectContainer_DragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            e.AcceptedOperation = DataPackageOperation.Link;
        }
        else
        {
            e.AcceptedOperation = DataPackageOperation.None;
        }
    }

    private async void ProjectContainer_Drop(object sender, DragEventArgs e)
    {
        var errorMessage = string.Empty;
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            var items = await e.DataView.GetStorageItemsAsync();
            var rootFolder = items.OfType<StorageFolder>().FirstOrDefault();
            if (rootFolder != null)
            {
                var result = await _projectService.AddProjectFromDirectoryAsync(rootFolder.Path);
                if (result.success)
                {
                    projects.Add(result.data);
                    DragVisual.Visibility = Visibility.Collapsed;
                    goto CloseDropPanel;
                }
                else
                {
                    errorMessage = result.message;
                }
            }
        }
        else
        {
            errorMessage = "Unsupported data format. Please drop a folder containing a project.";
        }

        _notificationService.ShowNotification(errorMessage, InfoBarSeverity.Error);

    CloseDropPanel:
        DragVisual.Visibility = Visibility.Collapsed;
        UpdateEmptyPlaceHolderVisibility();
    }

    private async void ListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not ProjectMetadataInfo project)
        {
            return;
        }

        try
        {
            project.Metadata.LastOpened = DateTime.Now;
            await ProjectService.CreateMetadataFileAsync(project.Path, project.Metadata);

            await _stateService.TransitionToAsync(StateKey.EngineEditor, project.Metadata);
        }
        catch (Exception exp)
        {
            _notificationService.ShowNotification($"Failed to load project: {exp.Message}", InfoBarSeverity.Error);
        }
    }
}