using Ghost.AssetForge.ViewModels;
using Ghost.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using System.Text;
using Windows.ApplicationModel.DataTransfer;

namespace Ghost.AssetForge.Views;

/// <summary>
/// Packing and baking page
/// </summary>
public sealed partial class PackingPage : Page
{
    public PackingViewModel ViewModel { get; }
    private readonly Inspector.InspectorDrawerRegistry _drawerRegistry;

    public PackingPage()
    {
        ViewModel = App.AppHost.Services.GetRequiredService<PackingViewModel>();
        _drawerRegistry = App.AppHost.Services.GetRequiredService<Inspector.InspectorDrawerRegistry>();
        InitializeComponent();

        ViewModel.LogMessages.CollectionChanged += LogMessages_CollectionChanged;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;

        Loaded += (s, e) =>
        {
            ViewModel.RefreshAssetStatistics();
            RebuildPackingSettings();
        };
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PackingViewModel.BakeSettings))
        {
            RebuildPackingSettings();
        }
    }

    private void RebuildPackingSettings()
    {
        PackingSettingsPanel.Children.Clear();
        if (ViewModel.BakeSettings != null)
        {
            var element = _drawerRegistry.DrawObject(ViewModel.BakeSettings);
            PackingSettingsPanel.Children.Add(element);
        }
    }

    private void LogMessages_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
        {
            if (ConsoleListView.Items.Count > 0)
            {
                ConsoleListView.ScrollIntoView(ConsoleListView.Items[^1]);
            }
        }
    }

    private async void BrowseOutputFolder_Click(object sender, RoutedEventArgs e)
    {
        var folderPicker = new Windows.Storage.Pickers.FolderPicker();
        folderPicker.FileTypeFilter.Add("*");
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance);
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
        var folder = await folderPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            ViewModel.OutputDirectory = folder.Path;
        }
    }



    private void CopyLog_Click(object sender, RoutedEventArgs e)
    {
        var sb = new StringBuilder();
        foreach (var msg in ViewModel.LogMessages)
        {
            sb.AppendLine(msg.ToString());
        }

        var package = new DataPackage();
        package.SetText(sb.ToString());
        Clipboard.SetContent(package);
    }


}

#region Helper Value Converters

public class LogLevelToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is LogMessage msg)
        {
            return msg.Level switch
            {
                LogLevel.Info => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 180, 180, 180)), // Gray
                LogLevel.Warning => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 247, 99, 12)), // Orange
                LogLevel.Error => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 232, 17, 35)), // Red
                LogLevel.Debug => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 120, 215)), // Blue/Accent
                _ => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 180, 180, 180))
            };
        }

        return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 180, 180, 180));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

#endregion
