using System.Runtime.InteropServices;

namespace Ghost.Nvtt;

public delegate void BeginImageDelegate(int w, int h, int d, int f, int m, int t);
public delegate NvttBoolean OutputDelegate(IntPtr data, int size);
public delegate void EndImageDelegate();

public delegate void ErrorDelegate(NvttError error);

public unsafe partial struct NvttOutputOptions
{
    public void SetOutputHandler(BeginImageDelegate? beginImageHandler, OutputDelegate? outputHandler, EndImageDelegate? endImageHandler)
    {
        var beginPtr = beginImageHandler == null ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(beginImageHandler);
        var outputPtr = outputHandler == null ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(outputHandler);
        var endPtr = endImageHandler == null ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(endImageHandler);

        Api.nvttSetOutputOptionsOutputHandler(
            (NvttOutputOptions*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref this),
            (delegate* unmanaged[Cdecl]<int, int, int, int, int, int, void>)beginPtr,
            (delegate* unmanaged[Cdecl]<void*, int, NvttBoolean>)outputPtr,
            endPtr);
    }

    public void SetErrorHandler(ErrorDelegate? errorHandler)
    {
        var errorPtr = errorHandler == null ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(errorHandler);

        Api.nvttSetOutputOptionsErrorHandler(
            (NvttOutputOptions*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref this),
            (delegate* unmanaged[Cdecl]<NvttError, void>)errorPtr);
    }
}
