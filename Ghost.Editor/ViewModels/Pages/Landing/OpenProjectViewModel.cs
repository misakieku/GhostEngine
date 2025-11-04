using CommunityToolkit.Mvvm.ComponentModel;
using Ghost.Data.Models;
using Ghost.Data.Services;
using Ghost.Editor.Core.AppState;
using Ghost.Editor.Core.Contracts;
using Ghost.Editor.Core.Notifications;
using Microsoft.UI.Xaml;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace Ghost.Editor.ViewModels.Pages.Landing;

internal partial class OpenProjectViewModel(ProjectService projectService, INotificationService _notificationService, AppStateMachine _stateService) : ObservableObject, INavigationAware
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
        projects.Clear();
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
                    projects.Add(result.value);
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

        _notificationService.ShowNotification(errorMessage, MessageType.Error);

    CloseDropPanel:
        DragVisibility = Visibility.Collapsed;
        UpdateEmptyPlaceHolderVisibility();
    }

    public async Task OpenProjectAsync(ProjectMetadataInfo project)
    {
        try
        {
            project.Metadata.LastOpened = DateTime.Now;
            await ProjectService.CreateMetadataFileAsync(project.Path, project.Metadata);

            await _stateService.TransitionToAsync(StateKey.EngineEditor, project);
        }
        catch (Exception e)
        {
            _notificationService.ShowNotification($"Failed to load project: {e.Message}", MessageType.Error);
        }
    }
}