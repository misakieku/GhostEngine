using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ghost.Core;
using System.Collections.ObjectModel;

namespace Ghost.Editor.ViewModels.Pages.EngineEditor;

internal partial class ConsoleViewModel : ObservableObject
{
    public ReadOnlyObservableCollection<LogMessage> Logs => Logger.Logs;

    [ObservableProperty]
    public partial bool ShowInfo
    {
        get; set;
    } = true;

    [ObservableProperty]
    public partial bool ShowWarning
    {
        get; set;
    } = true;

    [ObservableProperty]
    public partial bool ShowError
    {
        get; set;
    } = true;

    [ObservableProperty]
    public partial bool ShowStackTrace
    {
        get; set;
    } = false;

    [ObservableProperty]
    public partial LogMessage? SelectedLog
    {
        get; set;
    }

    partial void OnShowStackTraceChanged(bool value)
    {
        //Logger.HasStackTrace = value;
        //Logger.LogInfo($"Stack trace visibility set to {value}.");
    }

    [RelayCommand]
    private void ClearLogs()
    {
        Logger.Clear();
    }
}