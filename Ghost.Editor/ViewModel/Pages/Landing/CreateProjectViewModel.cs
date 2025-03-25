using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ghost.Database.Models.Projects;
using Ghost.Editor.Constants;
using Ghost.Editor.Contracts;
using Ghost.Editor.Helpers;
using Ghost.Editor.Models.Warpers;
using Microsoft.UI.Dispatching;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ghost.Editor.ViewModel.Pages.Landing;

internal partial class CreateProjectViewModel : ObservableRecipient, INavigationAware
{
    public ObservableCollection<TemplateInfoWarper> templates = new();

    [ObservableProperty]
    public partial TemplateInfoWarper? SelectedTemplate
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

    public void OnNavigatedTo(object? parameter)
    {
        DispatcherQueue.GetForCurrentThread().TryEnqueue(DispatcherQueuePriority.High, () =>
        {
            foreach (var templatePath in Directory.GetFiles(EditorDataPath.ProjectTemplatesFolder, "template.json", SearchOption.AllDirectories))
            {
                var templateInfo = JsonSerializer.Deserialize<TemplateInfo>(File.ReadAllText(templatePath));
                if (templateInfo == null)
                {
                    continue;
                }

                templates.Add(new(templatePath, templateInfo));
            }

            SelectedTemplate = templates.FirstOrDefault();
        });
    }

    public void OnNavigatedFrom()
    {
    }

    [RelayCommand]
    private async Task SelectionProjectLocation()
    {
        ProjectLocation = (await SystemUtilities.OpenFolderPickerAsync())?.Path ?? string.Empty;
    }
}