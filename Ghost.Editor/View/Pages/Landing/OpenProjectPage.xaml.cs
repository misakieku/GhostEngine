using Ghost.Database.DataContext;
using Ghost.Database.Models.Projects;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Ghost.Editor.View.Pages.Landing;

internal sealed partial class OpenProjectPage : Page
{
    public readonly ObservableCollection<ProjectInfo> projects = new();

    public OpenProjectPage()
    {
        foreach (var project in ProjectRepository.LoadProjects())
        {
            projects.Add(project);
        }

        InitializeComponent();

        if (projects.Count == 0)
        {
            PlaceHolderText.Visibility = Visibility.Visible;
            ProjectListView.Visibility = Visibility.Collapsed;
        }
    }

    private void ListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not ProjectInfo project)
        {
            return;
        }

        //TODO: Load project
    }
}