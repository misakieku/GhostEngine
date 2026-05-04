using System.Reflection;
using System.Runtime.InteropServices;

namespace Ghost.Nvtt;

public partial class Api
{
    static Api()
    {
        NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), (libraryName, assembly, searchPath) =>
        {
            var platform = OperatingSystem.IsWindows() ? "win" :
                            OperatingSystem.IsLinux() ? "linux" :
                            OperatingSystem.IsMacOS() ? "osx" : "unknown";
            var ext = OperatingSystem.IsWindows() ? ".dll" :
                        OperatingSystem.IsLinux() ? ".so" :
                        OperatingSystem.IsMacOS() ? ".dylib" : "";

            var arch = Environment.Is64BitProcess ? "x64" : "x86";
            var nativeDllDir = Path.Combine(AppContext.BaseDirectory, "runtimes", platform + "-" + arch, "native");

            return NativeLibrary.Load(Path.Combine(nativeDllDir, libraryName + ext));
        });
    }
}
