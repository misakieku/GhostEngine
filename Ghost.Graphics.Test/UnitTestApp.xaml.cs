using Ghost.Graphics.Test.Windows;

using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
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

    private static void LoadDll()
    {
        var currentDir = AppContext.BaseDirectory;
        var platform = OperatingSystem.IsWindows() ? "win" :
                       OperatingSystem.IsLinux() ? "linux" :
                       OperatingSystem.IsMacOS() ? "osx" : "unknown";
        var arch = Environment.Is64BitProcess ? "x64" : "x86";
        var nativeDllDir = Path.Combine(currentDir, "runtime", platform + "-" + arch, "native");
        if (Directory.Exists(nativeDllDir))
        {
            foreach (var dll in Directory.EnumerateFiles(nativeDllDir, "*.dll"))
            {
                NativeLibrary.Load(dll);
            }
        }
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        LoadDll();

        Microsoft.VisualStudio.TestPlatform.TestExecutor.UnitTestClient.CreateDefaultUI();

        _window = new GraphicsTestWindow();
        _window.Activate();

        UITestMethodAttribute.DispatcherQueue = _window.DispatcherQueue;

        Microsoft.VisualStudio.TestPlatform.TestExecutor.UnitTestClient.Run(Environment.CommandLine);
    }
}
