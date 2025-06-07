using CommunityToolkit.Mvvm.ComponentModel;
using Ghost.App.Contracts;
using Ghost.App.Infrastructures.AppState;
using Ghost.App.Services;
using Ghost.Data.Models;
using Ghost.Data.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace Ghost.Editor.ViewModels.Pages.Landing;

internal partial class OpenProjectViewModel(ProjectService projectService, StackedNotificationService _notificationService, AppStateMachine _stateService) : ObservableObject, INavigationAware
{
    public readonly ObservableCollection<ProjectMetadataInfo> projects = new();

    [ObservableProperty]
    public partial Visibility EmptyVisibility
    {
        get;
        set;
    }

    [ObservableProperty]
    public partial Visibility DragVisibility
    {
        get;
        set;
    }

    public void UpdateEmptyPlaceHolderVisibility()
    {
        EmptyVisibility = projects.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public async void OnNavigatedTo(object? parameter)
    {
        projects.Clear();
        await foreach (var projectInfo in projectService.GetAllProjectAsync())
        {
            var metadata = await ProjectService.LoadMetadataAsync(projectInfo.MetadataPath);
            if (metadata == null)
            {
                continue;
            }

            projects.Add(new(projectInfo.MetadataPath, metadata));
        }

        UpdateEmptyPlaceHolderVisibility();
        DragVisibility = Visibility.Collapsed;
    }

    public void OnNavigatedFrom()
    {
    }

    public async Task ContentDrop(DataPackageView dataView)
    {
        var errorMessage = string.Empty;
        if (dataView.Contains(StandardDataFormats.StorageItems))
        {
            var items = await dataView.GetStorageItemsAsync();
            var rootFolder = items.OfType<StorageFolder>().FirstOrDefault();
            if (rootFolder != null)
            {
                var result = await projectService.AddProjectFromDirectoryAsync(rootFolder.Path);
                if (result.success)
                {
                    projects.Add(result.data);
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
        DragVisibility = Visibility.Collapsed;
        UpdateEmptyPlaceHolderVisibility();
    }

    public async Task LoadProject(ProjectMetadataInfo project)
    {
        try
        {
            project.Metadata.LastOpened = DateTime.Now;
            await ProjectService.CreateMetadataFileAsync(project.Path, project.Metadata);

            await _stateService.TransitionToAsync(StateKey.EngineEditor, project);
        }
        catch (Exception e)
        {
            _notificationService.ShowNotification($"Failed to load project: {e.Message}", InfoBarSeverity.Error);
        }
    }
}