using CommunityToolkit.Mvvm.ComponentModel;
using Ghost.Database.Models.Projects;
using System.Collections.ObjectModel;

namespace Ghost.Editor.ViewModel.Pages.Landing;

internal partial class CreateProjectViewModel : ObservableRecipient
{
    public ObservableCollection<TemplateInfo> templates = new();

    [ObservableProperty]
    public partial TemplateInfo SelectedTemplate
    {
        get;
        set;
    }
}