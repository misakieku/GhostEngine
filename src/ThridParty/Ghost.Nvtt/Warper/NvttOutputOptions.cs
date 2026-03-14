using System.Runtime.InteropServices;

namespace Ghost.Nvtt.Warper;

/// <summary>
/// Configures where compressed data is written and how it is formatted.
///
/// Managed callback delegates are stored in this object and passed to the
/// native library via <see cref="Marshal.GetFunctionPointerForDelegate"/>.
/// The delegates are kept alive by pinned <see cref="GCHandle"/> instances
/// for the lifetime of this object.
/// </summary>
public sealed unsafe class NvttOutputOptionsHandle : IDisposable
{
    private NvttOutputOptions* _ptr;

    /// <summary>Raw pointer - use only when calling the native API directly.</summary>
    public NvttOutputOptions* Ptr => _ptr;

    // -------------------------------------------------------------------------
    // Managed callback storage
    // -------------------------------------------------------------------------

    // Delegate types that match the native C signatures exactly.
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void BeginImageDelegate(int size, int width, int height, int depth, int face, int mipLevel);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate NvttBoolean OutputDataDelegate(void* data, int size, NvttBoolean lastChunk);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ErrorDelegate(NvttError error);

    // Pinned delegate instances - must stay alive as long as native code may call them.
    private BeginImageDelegate? _beginImageDelegate;
    private OutputDataDelegate? _outputDataDelegate;
    private ErrorDelegate? _errorDelegate;

    // Managed user-facing callbacks.
    private Action<int, int, int, int, int, int>? _beginImage;
    private Func<nint, int, bool>? _outputData;
    private Action? _endImage;
    private Action<NvttError>? _errorHandler;

    // -------------------------------------------------------------------------
    // Construction / destruction
    // -------------------------------------------------------------------------

    public NvttOutputOptionsHandle() => _ptr = Api.nvttCreateOutputOptions();

    public void Dispose()
    {
        if (_ptr != null)
        {
            Api.nvttDestroyOutputOptions(_ptr);
            _ptr = null;
        }
        // Release delegate references so GC can collect them.
        _beginImageDelegate = null;
        _outputDataDelegate = null;
        _errorDelegate = null;
    }

    // -------------------------------------------------------------------------
    // Properties
    // -------------------------------------------------------------------------

    /// <summary>
    /// Path of the output file.  The file is created/overwritten when
    /// compression runs.
    /// </summary>
    public string FileName
    {
        set
        {
            ThrowIfDisposed();
            Span<byte> buf = stackalloc byte[NvttInterop._MAX_STACK_PATH];
            var utf8 = NvttInterop.ToUtf8(value, buf);
            fixed (byte* p = utf8)
            {
                Api.nvttSetOutputOptionsFileName(_ptr, (sbyte*)p);
            }
        }
    }

    /// <summary>
    /// Whether to write a DDS / KTX file header.  Default is <c>true</c>.
    /// </summary>
    public bool OutputHeader
    {
        set { ThrowIfDisposed(); Api.nvttSetOutputOptionsOutputHeader(_ptr, NvttInterop.ToNvtt(value)); }
    }

    /// <summary>Container format (DDS or DDS10).</summary>
    public NvttContainer Container
    {
        set { ThrowIfDisposed(); Api.nvttSetOutputOptionsContainer(_ptr, value); }
    }

    /// <summary>Application-defined version number stored in the header.</summary>
    public int UserVersion
    {
        set { ThrowIfDisposed(); Api.nvttSetOutputOptionsUserVersion(_ptr, value); }
    }

    /// <summary>Sets the sRGB flag in the output header.</summary>
    public bool Srgb
    {
        set { ThrowIfDisposed(); Api.nvttSetOutputOptionsSrgbFlag(_ptr, NvttInterop.ToNvtt(value)); }
    }

    // -------------------------------------------------------------------------
    // Methods
    // -------------------------------------------------------------------------

    /// <summary>Resets all options to their default values.</summary>
    public void Reset()
    {
        ThrowIfDisposed();
        Api.nvttResetOutputOptions(_ptr);
    }

    /// <summary>
    /// Installs managed callbacks that receive the compressed data stream.
    ///
    /// <para><paramref name="beginImage"/> is called once per mip level before any
    /// data arrives: <c>(size, width, height, depth, face, mipLevel)</c>.</para>
    /// <para><paramref name="outputData"/> receives each chunk of compressed bytes.
    /// The first argument is an <see cref="nint"/> pointing to the data, the
    /// second is the byte count.  Return <c>true</c> to continue, <c>false</c>
    /// to abort.</para>
    /// <para><paramref name="endImage"/> is called once after the last chunk.</para>
    ///
    /// Only one set of output callbacks can be active at a time; calling this
    /// method again replaces the previous ones.
    /// </summary>
    public void SetOutputHandler(
        Action<int, int, int, int, int, int>? beginImage,
        Func<nint, int, bool>? outputData,
        Action? endImage)
    {
        ThrowIfDisposed();
        _beginImage = beginImage;
        _outputData = outputData;
        _endImage = endImage;
        RebindOutputHandler();
    }

    /// <summary>
    /// Installs a managed error handler.  The handler is invoked with the
    /// <see cref="NvttError"/> code whenever the native library encounters an
    /// error.
    /// </summary>
    public void SetErrorHandler(Action<NvttError>? handler)
    {
        ThrowIfDisposed();
        _errorHandler = handler;
        RebindErrorHandler();
    }

    // -------------------------------------------------------------------------
    // Internal delegate trampolines (instance-bound via closures)
    // -------------------------------------------------------------------------

    private void RebindOutputHandler()
    {
        // Capture current callbacks into local vars for the closure.
        var beginImage = _beginImage;
        var outputData = _outputData;

        _beginImageDelegate = (size, width, height, depth, face, mipLevel) =>
            beginImage?.Invoke(size, width, height, depth, face, mipLevel);

        _outputDataDelegate = (data, size, lastChunk) =>
        {
            var ok = outputData?.Invoke((nint)data, size) ?? true;
            return ok ? Ghost.Nvtt.NvttBoolean.NVTT_True : Ghost.Nvtt.NvttBoolean.NVTT_False;
        };

        var beginPtr = Marshal.GetFunctionPointerForDelegate(_beginImageDelegate);
        var outputPtr = Marshal.GetFunctionPointerForDelegate(_outputDataDelegate);

        Api.nvttSetOutputOptionsOutputHandler(
            _ptr,
            (delegate* unmanaged[Cdecl]<int, int, int, int, int, int, void>)beginPtr,
            (delegate* unmanaged[Cdecl]<void*, int, NvttBoolean>)outputPtr,
            IntPtr.Zero);
    }

    private void RebindErrorHandler()
    {
        var handler = _errorHandler;
        _errorDelegate = error => handler?.Invoke(error);

        var errorPtr = Marshal.GetFunctionPointerForDelegate(_errorDelegate);
        Api.nvttSetOutputOptionsErrorHandler(
            _ptr,
            (delegate* unmanaged[Cdecl]<NvttError, void>)errorPtr);
    }

    // -------------------------------------------------------------------------

    private void ThrowIfDisposed()
    {
        if (_ptr == null)
        {
            throw new ObjectDisposedException(nameof(NvttOutputOptionsHandle));
        }
    }
}
