using Ghost.Editor.Core;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel;
using WinUIEx;

namespace Ghost.Editor.Views.Windows;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
internal sealed partial class SplashWindow : WindowEx
{
    public SplashWindow()
    {
        InitializeComponent();

        IsResizable = false;
        IsMaximizable = false;
        IsMinimizable = false;
        ExtendsContentIntoTitleBar = true;

        this.CenterOnScreen(750, 400);
    }

    private void MainGrid_Loaded(object sender, RoutedEventArgs e)
    {
        var version = Package.Current.Id.Version;
        VersionTextBlock.Text = $"Version {version.Major}.{version.Minor}.{version.Build}";
        LoadingTextBlock.Text = $"Loading {EditorApplication.ProjectName}...";
        CopyrightTextBlock.Text = $"Copyright © {DateTime.Now.Year} Ghost Engine. All rights reserved.";
    }
}
