using Ghost.Core;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Ghost.Graphics.Test.Controls;

public sealed partial class DebugConsole : UserControl
{
    private readonly ObservableCollection<LogMessage> _filteredLogs = [];

    public DebugConsole()
    {
        InitializeComponent();

        LogItemsRepeater.ItemsSource = _filteredLogs;

        Logger.Logs.LogChanged += OnLogChange;

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

    private void OnLogChange(object? sender, NotifyCollectionChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null)
                    {
                        foreach (var item in e.NewItems)
                        {
                            if (item is LogMessage logMessage && ShouldShowLogItem(logMessage))
                            {
                                _filteredLogs.Add(logMessage);
                                if (AutoScrollCheckBox.IsChecked == true)
                                {
                                    LogScrollViewer.ScrollToVerticalOffset(LogScrollViewer.ScrollableHeight);
                                }
                            }
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                    {
                        foreach (var item in e.OldItems)
                        {
                            if (item is LogMessage logMessage)
                            {
                                _filteredLogs.Remove(logMessage);
                            }
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    RefreshLogs();
                    break;
                default:
                    break;
            }
        });
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

        foreach (var log in Logger.Logs)
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
