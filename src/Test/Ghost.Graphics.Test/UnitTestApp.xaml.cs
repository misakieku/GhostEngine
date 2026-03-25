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

    private static void LoadDll()
    {
        var currentDir = AppContext.BaseDirectory;
        var platform = OperatingSystem.IsWindows() ? "win" :
                       OperatingSystem.IsLinux() ? "linux" :
                       OperatingSystem.IsMacOS() ? "osx" : "unknown";
        var arch = Environment.Is64BitProcess ? "x64" : "x86";
        var nativeDllDir = Path.Combine(currentDir, "runtimes", platform + "-" + arch, "native");
        if (Directory.Exists(nativeDllDir))
        {
            foreach (var dll in Directory.EnumerateFiles(nativeDllDir, "*.dll"))
            {
                NativeLibrary.Load(dll);
            }
        }
        //NativeLibrary.SetDllImportResolver(typeof(UnitTestApp).Assembly, (libraryName, assembly, searchPath) =>
        //{
        //    if (libraryName == "dxcompiler")
        //    {
        //        NativeLibrary.Load(Path.Combine(nativeDllDir, "dxil.dll"));
        //    }

        //    return IntPtr.Zero;
        //});
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        LoadDll();

        var opts = new AllocationManagerInitOpts
        {
            ArenaCapacity = 1024 * 1024 * 1024, // 1GB
            StackCapacity = 1024 * 1024 * 32, // 32MB
            FreeListConcurrencyLevel = Environment.ProcessorCount,
        };

        AllocationManager.Initialize(opts);

        _window = new GraphicsTestWindow();
        _window.Activate();

        UnhandledException += (sender, e) =>
        {
            Logger.LogError(e.Exception);
#if DEBUG
            System.Diagnostics.Debugger.Break();
#endif
        };
    }
}
