using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.FMOD;

internal static class FmodDllImportResolver
{
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Initialize()
    {
        NativeLibrary.SetDllImportResolver(typeof(FmodDllImportResolver).Assembly, Resolve);
    }

    private static nint Resolve(string libraryName, System.Reflection.Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName != "fmod" && libraryName != "fmodstudio")
        {
            return IntPtr.Zero;
        }

        var platform = OperatingSystem.IsWindows() ? "win" :
                       OperatingSystem.IsLinux() ? "linux" :
                       OperatingSystem.IsMacOS() ? "osx" : "unknown";
        var (prefix, ext) = OperatingSystem.IsWindows() ? ("", ".dll") :
                            OperatingSystem.IsLinux() ? ("lib", ".so") :
                            OperatingSystem.IsMacOS() ? ("lib", ".dylib") : ("", "");
        var arch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            Architecture.X86 => "x86",
            _ => "x64"
        };

        var nativeDir = Path.Combine(AppContext.BaseDirectory, "runtimes", $"{platform}-{arch}", "native");

        if (libraryName == "fmodstudio")
        {
            NativeLibrary.TryLoad(Path.Combine(nativeDir, $"{prefix}fmod{ext}"), out _);
        }

        var nativePath = Path.Combine(nativeDir, $"{prefix}{libraryName}{ext}");
        return NativeLibrary.TryLoad(nativePath, out var handle) ? handle : IntPtr.Zero;
    }
}
