using CommunityToolkit.Mvvm.ComponentModel;
using Ghost.Data.Models;
using Ghost.Engine.Resources;

namespace Ghost.Editor.ViewModel.Windows;

internal partial class EngineEditorViewModel : ObservableRecipient
{
    public string engineVersionDescriptor = $"{EngineData.ENGINE_NAME} - {EngineData.s_engineVersion}";

    [ObservableProperty]
    public partial ProjectMetadata CurrentProject
    {
        get;
        set;
    }
}