using Ghost.Data.Models;
using Ghost.Data.Services;
using Ghost.Editor.View.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.ObjectModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Ghost.Editor.View.Pages.Landing;

internal sealed partial class OpenProjectPage : Page
{
    private readonly ProjectService _projectService;

    public readonly ObservableCollection<ProjectInfo> projects = new();

    public OpenProjectPage()
    {
        _projectService = App.GetService<ProjectService>();

        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        await foreach (var project in _projectService.LoadAllProjectAsync())
        {
            projects.Add(project);
        }

        if (projects.Count == 0)
        {
            PlaceHolderText.Visibility = Visibility.Visible;
            ProjectListView.Visibility = Visibility.Collapsed;
        }
    }

    private async void ListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not ProjectInfo project)
        {
            return;
        }

        if (EngineEditorWindow.TryLoadProject(project))
        {
            App.GetService<LandingWindow>().Close();

            project.LastOpened = System.DateTime.Now;
            await _projectService.UpdateProjectAsync(project);
        }
    }
}