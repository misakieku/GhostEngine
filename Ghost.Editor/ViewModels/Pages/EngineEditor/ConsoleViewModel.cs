using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ghost.Engine.Models;
using Ghost.Engine.Services;
using System.Collections.ObjectModel;

namespace Ghost.Editor.ViewModels.Pages.EngineEditor;

internal partial class ConsoleViewModel : ObservableObject
{
    [ObservableProperty]
    public partial ObservableCollection<LogMessage> Logs
    {
        get; set;
    } = new();

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

    public ConsoleViewModel()
    {
        foreach (var log in Logger.Logs)
        {
            Logs.Add(log);
        }

        Logger.OnLogsUpdate += UpdateLogs;
    }

    ~ConsoleViewModel()
    {
        Logger.OnLogsUpdate -= UpdateLogs;
    }

    private void UpdateLogs(LogChangeType updateType)
    {
        switch (updateType)
        {
            case LogChangeType.LogAdded:
                Logs.Add(Logger.Logs[^1]);
                break;
            case LogChangeType.LogRemoved:
                if (Logs.Count > 0)
                {
                    Logs.RemoveAt(0);
                }
                break;
            case LogChangeType.LogsCleared:
                Logs.Clear();
                break;
        }
    }

    partial void OnShowStackTraceChanged(bool value)
    {
        Logger.HasStackTrace = value;
        Logger.LogInfo($"Stack trace visibility set to {value}.");
    }

    [RelayCommand]
    private void ClearLogs()
    {
        Logger.Clear();
    }
}
