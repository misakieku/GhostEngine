using FluentIcons.Common;
using Ghost.Core;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;

namespace Ghost.Editor.Views.Controls;

public sealed partial class LogViewer : UserControl
{
    private readonly ObservableCollection<LogMessage> _filteredLogs = [];

    public LogViewer()
    {
        InitializeComponent();

        LogItemsView.ItemsSource = _filteredLogs;

        Logger.Impl.OnLogAdded += OnLogAdded;
        Logger.Impl.OnLogsCleared += OnLogCleared;

        // Subscribe to filter changes
        ShowInfoCheckBox.Checked += OnFilterChanged;
        ShowInfoCheckBox.Unchecked += OnFilterChanged;
        ShowWarningCheckBox.Checked += OnFilterChanged;
        ShowWarningCheckBox.Unchecked += OnFilterChanged;
        ShowErrorCheckBox.Checked += OnFilterChanged;
        ShowErrorCheckBox.Unchecked += OnFilterChanged;
        ShowDebugCheckBox.Checked += OnFilterChanged;
        ShowDebugCheckBox.Unchecked += OnFilterChanged;

        // Load existing logs
        RefreshLogs();
    }

    private void OnLogAdded(LogMessage message)
    {
        if (ShouldShowLogItem(message))
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                _filteredLogs.Add(message);
                if (AutoScrollCheckBox.IsChecked == true)
                {
                    LogScrollView.UpdateLayout();
                    LogScrollView.ScrollTo(0.0, LogScrollView.ScrollableHeight);
                }
            });
        }
    }

    private void OnLogCleared()
    {
        DispatcherQueue.TryEnqueue(_filteredLogs.Clear);
    }

    private void OnFilterChanged(object sender, RoutedEventArgs e)
    {
        RefreshLogs();
    }

    private bool ShouldShowLogItem(LogMessage message)
    {
        return message.Level switch
        {
            LogLevel.Info => ShowInfoCheckBox.IsChecked == true,
            LogLevel.Warning => ShowWarningCheckBox.IsChecked == true,
            LogLevel.Error => ShowErrorCheckBox.IsChecked == true,
            LogLevel.Debug => ShowDebugCheckBox.IsChecked == true,
            _ => true
        };
    }

    private void RefreshLogs()
    {
        _filteredLogs.Clear();
        Logger.Info("Message");

        foreach (var log in Logger.Logs)
        {
            if (ShouldShowLogItem(log))
            {
                _filteredLogs.Add(log);
            }
        }

        if (AutoScrollCheckBox.IsChecked == true)
        {
            LogScrollView.UpdateLayout();
            LogScrollView.ScrollTo(0.0, LogScrollView.ScrollableHeight);
        }
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        Logger.Impl.Clear();
    }

    private void ShowStackTraceCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        Logger.Impl.CaptureStackTrace = true;
    }

    private void ShowStackTraceCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        Logger.Impl.CaptureStackTrace = false;
    }

    private void LogItemsView_SelectionChanged(ItemsView sender, ItemsViewSelectionChangedEventArgs args)
    {
        if (sender.SelectedItem is LogMessage selectedLog)
        {
            SelectedLogMessageTextBlock.Text = selectedLog.Message;
            SelectedLogStackTraceTextBlock.Text = selectedLog.StackTrace ?? "Stack trace not available.";
        }
        else
        {
            SelectedLogMessageTextBlock.Text = string.Empty;
            SelectedLogStackTraceTextBlock.Text = string.Empty;
        }
    }
}

// Converter for log level to color
public partial class LogLevelToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is LogLevel level)
        {
            Application.Current.Resources.TryGetValue("SystemFillColorAttentionBrush", out var infoBrush);
            Application.Current.Resources.TryGetValue("SystemFillColorCautionBrush", out var warningBrush);
            Application.Current.Resources.TryGetValue("SystemFillColorCriticalBrush", out var errorBrush);
            Application.Current.Resources.TryGetValue("SystemFillColorNeutralBrush", out var debugBrush);

            return level switch
            {
                LogLevel.Info => infoBrush,
                LogLevel.Warning => warningBrush,
                LogLevel.Error => errorBrush,
                LogLevel.Debug => debugBrush,
                _ => new SolidColorBrush(Colors.Black)
            };
        }
        return new SolidColorBrush(Colors.Black);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

// Converter for log level to symbol
public partial class LogLevelToSymbolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is LogLevel level)
        {
            return level switch
            {
                LogLevel.Info => Icon.Info,
                LogLevel.Warning => Icon.Warning,
                LogLevel.Error => Icon.ErrorCircle,
                LogLevel.Debug => Icon.Bug,
                _ => Icon.Info
            };
        }
        return Icon.Info;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
