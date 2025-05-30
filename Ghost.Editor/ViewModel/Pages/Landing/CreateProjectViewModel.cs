using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ghost.Data.Models;
using Ghost.Data.Services;
using Ghost.Editor.AppStates;
using Ghost.Editor.Contracts;
using Ghost.Editor.Helpers;
using Ghost.Editor.Services;
using Ghost.Engine.Resources;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Ghost.Editor.ViewModel.Pages.Landing;

internal partial class CreateProjectViewModel(StackedNotificationService notificationService, ProjectService projectService, AppStateService stateService) : ObservableRecipient, INavigationAware
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
            notificationService.ShowNotification("Incorrect project info", InfoBarSeverity.Error);
            return;
        }

        var result = await projectService.CreateProjectAsync(ProjectName, ProjectLocation, EngineData.s_engineVersion, SelectedTemplate.Value.directory);
        if (!result.success || result.data == null)
        {
            notificationService.ShowNotification(result.message, InfoBarSeverity.Error);
            return;
        }

        var metadata = await ProjectService.LoadMetadataAsync(result.data.MetadataPath); // Metadata should not be null here if create project succeeded
        try
        {
            await stateService.TransitionToAsync(StateKey.EngineEditor, metadata);
        }
        catch (System.Exception e)
        {
            notificationService.ShowNotification($"Failed to load project: {e.Message}", InfoBarSeverity.Error);
        }
    }
}