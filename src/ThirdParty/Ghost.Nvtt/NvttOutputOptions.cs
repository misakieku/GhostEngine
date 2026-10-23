using System.Runtime.InteropServices;

namespace Ghost.Nvtt;

public delegate void BeginImageDelegate(int w, int h, int d, int f, int m, int t);
public delegate NvttBoolean OutputDelegate(IntPtr data, int size);
public delegate void EndImageDelegate();

public delegate void ErrorDelegate(NvttError error);

public class NvttOutputHandler : IDisposable
{
    public BeginImageDelegate? beginImageHandler;
    public OutputDelegate? outputHandler;
    public EndImageDelegate? endImageHandler;
    public ErrorDelegate? errorHandler;

    public void Dispose()
    {
        beginImageHandler = null;
        outputHandler = null;
        endImageHandler = null;
        errorHandler = null;

        GC.SuppressFinalize(this);
    }
}

public unsafe partial struct NvttOutputOptions
{
    public void SetOutputHandler(NvttOutputHandler handler)
    {
        var beginPtr = handler.beginImageHandler == null ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(handler.beginImageHandler);
        var outputPtr = handler.outputHandler == null ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(handler.outputHandler);
        var endPtr = handler.endImageHandler == null ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(handler.endImageHandler);

        Api.nvttSetOutputOptionsOutputHandler(
            (NvttOutputOptions*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref this),
            (delegate* unmanaged[Cdecl]<int, int, int, int, int, int, void>)beginPtr,
            (delegate* unmanaged[Cdecl]<void*, int, NvttBoolean>)outputPtr,
            endPtr);

        var errorPtr = handler.errorHandler == null ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(handler.errorHandler);

        Api.nvttSetOutputOptionsErrorHandler(
            (NvttOutputOptions*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref this),
            (delegate* unmanaged[Cdecl]<NvttError, void>)errorPtr);
    }
}
