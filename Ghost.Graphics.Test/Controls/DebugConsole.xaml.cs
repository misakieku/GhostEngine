using System.Collections.ObjectModel;

using Ghost.Graphics.Test.Models;
using Ghost.Graphics.Test.Services;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace Ghost.Graphics.Test.Controls;

public sealed partial class DebugConsole : UserControl
{
    private readonly ObservableCollection<LogItem> _filteredLogs = [];
    private readonly LoggingService _loggingService;

    public DebugConsole()
    {
        InitializeComponent();
        _loggingService = LoggingService.Instance;

        LogItemsRepeater.ItemsSource = _filteredLogs;

        // Subscribe to logging events
        _loggingService.LogAdded += OnLogAdded;
        _loggingService.LogsCleared += OnLogsCleared;

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

    private void OnLogAdded(LogItem logItem)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (ShouldShowLogItem(logItem))
            {
                _filteredLogs.Add(logItem);

                if (AutoScrollCheckBox.IsChecked == true)
                {
                    LogScrollViewer.ScrollToVerticalOffset(LogScrollViewer.ScrollableHeight);
                }
            }
        });
    }

    private void OnLogsCleared()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            _filteredLogs.Clear();
        });
    }

    private void OnFilterChanged(object sender, RoutedEventArgs e)
    {
        RefreshLogs();
    }

    private bool ShouldShowLogItem(LogItem logItem)
    {
        return logItem.Level switch
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

        foreach (var log in _loggingService.Logs)
        {
            if (ShouldShowLogItem(log))
            {
                _filteredLogs.Add(log);
            }
        }

        if (AutoScrollCheckBox.IsChecked == true)
        {
            LogScrollViewer.ScrollToVerticalOffset(LogScrollViewer.ScrollableHeight);
        }
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        _loggingService.Clear();
    }

    private void ShowStackTraceCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        _loggingService.CaptureStackTrace = true;
    }

    private void ShowStackTraceCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        _loggingService.CaptureStackTrace = false;
    }
}

// Converter for log level to color
public class LogLevelToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is LogLevel level)
        {
            return level switch
            {
                LogLevel.Info => new SolidColorBrush(Colors.DodgerBlue),
                LogLevel.Warning => new SolidColorBrush(Colors.Orange),
                LogLevel.Error => new SolidColorBrush(Colors.Red),
                LogLevel.Debug => new SolidColorBrush(Colors.Gray),
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
public class LogLevelToSymbolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is LogLevel level)
        {
            return level switch
            {
                LogLevel.Info => "ℹ",
                LogLevel.Warning => "⚠",
                LogLevel.Error => "✖",
                LogLevel.Debug => "🐛",
                _ => "•"
            };
        }
        return "•";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
