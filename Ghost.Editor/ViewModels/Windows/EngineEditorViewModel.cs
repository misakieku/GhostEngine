using CommunityToolkit.Mvvm.ComponentModel;
using Ghost.Data.Models;
using Ghost.Data.Services;
using Ghost.Engine.Resources;

namespace Ghost.Editor.ViewModels.Windows;

internal partial class EngineEditorViewModel : ObservableRecipient
{
    public string engineVersionDescriptor = $"{EngineData.ENGINE_NAME} - {EngineData.EngineVersion}";

    public ProjectMetadataInfo CurrentProject => ProjectService.CurrentProject;
}