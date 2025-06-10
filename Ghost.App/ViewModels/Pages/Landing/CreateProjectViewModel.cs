using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ghost.App.Contracts;
using Ghost.App.Infrastructures.AppState;
using Ghost.App.Services;
using Ghost.App.Utilities;
using Ghost.Data.Models;
using Ghost.Data.Services;
using Ghost.Editor.Models;
using Ghost.Engine.Resources;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Ghost.Editor.ViewModels.Pages.Landing;

internal partial class CreateProjectViewModel(NotificationService notificationService, ProjectService projectService, AppStateMachine stateService) : ObservableObject, INavigationAware
{
    public ObservableCollection<TemplateData> templates = new();

    [ObservableProperty]
    public partial TemplateData? SelectedTemplate
    {
        get;
        set;
    }

    [ObservableProperty]
    public partial string? ProjectName
    {
        get;
        set;
    }

    [ObservableProperty]
    public partial string? ProjectLocation
    {
        get;
        set;
    }

    public async void OnNavigatedTo(object? parameter)
    {
        templates.Clear();
        await foreach (var (path, info) in ProjectService.GetProjectTemplatesAsync())
        {
            templates.Add(new(path, info));
        }

        SelectedTemplate = templates.FirstOrDefault();
    }

    public void OnNavigatedFrom()
    {
    }

    [RelayCommand]
    private async Task SelectionProjectLocation()
    {
        var folder = await SystemUtilities.OpenFolderPickerAsync();
        if (folder != null)
        {
            ProjectLocation = folder.Path;
        }
    }

    [RelayCommand]
    private async Task CreateProject()
    {
        if (string.IsNullOrWhiteSpace(ProjectName)
            || !Directory.Exists(ProjectLocation)
            || !SelectedTemplate.HasValue)
        {
            notificationService.ShowNotification("Incorrect project info", MessageType.Error);
            return;
        }

        var result = await projectService.CreateProjectAsync(ProjectName, ProjectLocation, EngineData.s_engineVersion, SelectedTemplate.Value.directory);
        if (!result.success)
        {
            notificationService.ShowNotification(result.message, MessageType.Error);
            return;
        }

        try
        {
            await stateService.TransitionToAsync(StateKey.EngineEditor, result.data);
        }
        catch (System.Exception e)
        {
            notificationService.ShowNotification($"Failed to load project: {e.Message}", MessageType.Error);
        }
    }
}