using Ghost.Core;
using Ghost.Graphics.Test.Windows;

using Microsoft.UI.Xaml;
using Misaki.HighPerformance.LowLevel.Buffer;
using System.Runtime.InteropServices;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Ghost.Graphics.Test;
/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class UnitTestApp : Application
{
    private Window? _window;

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public UnitTestApp()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new GraphicsTestWindow();
        _window.Activate();

        UnhandledException += (sender, e) =>
        {
            Logger.LogError(e.Exception);
#if DEBUG
            System.Diagnostics.Debugger.Break();
#endif
            Environment.FailFast("Unhandled exception", e.Exception);
        };
    }
}