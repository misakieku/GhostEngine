using System.Reflection;
using System.Runtime.InteropServices;

namespace Ghost.DXC;

public partial class Api
{
    static Api()
    {
        NativeLibrary.SetDllImportResolver(typeof(Api).Assembly, DxcDllImportResolver);
    }

    private static nint DxcDllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        // NOTE: Currently only support Windows.
        if (libraryName == "dxcompiler")
        {
            NativeLibrary.TryLoad("runtimes/win-x64/native/dxil.dll", out _);

            if (NativeLibrary.TryLoad("runtimes/win-x64/native/dxcompiler.dll", out var dxcHandle))
            {
                return dxcHandle;
            }
        }

        return IntPtr.Zero;
    }
}
