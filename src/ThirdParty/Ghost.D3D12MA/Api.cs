using System.Runtime.InteropServices;

namespace Ghost.D3D12MA;

public partial class Api
{
    static Api()
    {
        NativeLibrary.SetDllImportResolver(typeof(Api).Assembly, (libraryName, assembly, searchPath) =>
        {
            if (libraryName != "d3d12ma")
            {
                return IntPtr.Zero;
            }

            var platform = OperatingSystem.IsWindows() ? "win" :
                           OperatingSystem.IsLinux() ? "linux" :
                           OperatingSystem.IsMacOS() ? "osx" : "unknown";
            var ext = OperatingSystem.IsWindows() ? ".dll" :
                      OperatingSystem.IsLinux() ? ".so" :
                      OperatingSystem.IsMacOS() ? ".dylib" : "";
            var arch = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "x64",
                Architecture.Arm64 => "arm64",
                Architecture.X86 => "x86",
                _ => "x64"
            };

            var nativePath = Path.Combine(AppContext.BaseDirectory, "runtimes", $"{platform}-{arch}", "native", libraryName + ext);
            return NativeLibrary.TryLoad(nativePath, out var handle) ? handle : IntPtr.Zero;
        });
    }
}
