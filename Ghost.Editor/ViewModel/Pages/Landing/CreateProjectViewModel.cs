using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ghost.Data.Models;
using Ghost.Data.Services;
using Ghost.Editor.Contracts;
using Ghost.Editor.Helpers;
using Ghost.Editor.View.Windows;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace Ghost.Editor.ViewModel.Pages.Landing;

internal partial class CreateProjectViewModel(ProjectService projectService) : ObservableRecipient, INavigationAware
{
    public ObservableCollection<TemplateData> templates = new();

    [ObservableProperty]
    public partial TemplateData? SelectedTemplate
    {
        get;
        set;
    }

    [ObservableProperty]
    public partial string ProjectName
    {
        get;
        set;
    }

    [ObservableProperty]
    public partial string ProjectLocation
    {
        get;
        set;
    }

    public async void OnNavigatedTo(object? parameter)
    {
        await foreach (var (path, info) in projectService.GetProjectTemplatesAsync())
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
        ProjectLocation = (await SystemUtilities.OpenFolderPickerAsync())?.Path ?? string.Empty;
    }

    [RelayCommand]
    private async Task CreateProject()
    {
        if (string.IsNullOrWhiteSpace(ProjectName) || !Directory.Exists(ProjectLocation) || SelectedTemplate == null)
        {
            return;
        }

        var projectPath = await projectService.CreateProjectAsync(ProjectName, ProjectLocation, SelectedTemplate.directory);

        var packageVersion = Package.Current.Id.Version;
        var newProject = await projectService.AddProjectAsync(ProjectName, projectPath, new System.Version(packageVersion.Major, packageVersion.Minor, packageVersion.Build));

        if (EngineEditorWindow.TryLoadProject(newProject))
        {
            App.GetService<LandingWindow>().Close();
        }
    }
}