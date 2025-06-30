using System.Runtime.InteropServices;

namespace Ghost.Graphics.Contracts;

[ComImport]
[Guid("63aad0b8-7c24-40ff-85a8-640d944cc325")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ISwapChainPanelNativeRaw
{
    // IUnknown: QueryInterface, AddRef, Release
    void QueryInterface(in Guid riid, out IntPtr ppvObject);
    uint AddRef();
    uint Release();

    // SetSwapChain is the 4th slot in the vtable (0-based index 3)
    int SetSwapChain(IntPtr swapChainPtr);
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
public unsafe readonly struct ISwapChainPanelNative
{
    private readonly IntPtr _nativePtr;
    public readonly IntPtr NativePointer => _nativePtr;

    public ISwapChainPanelNative(IntPtr nativePtr)
    {
        _nativePtr = nativePtr;
    }

    public static ISwapChainPanelNative FromSwapChainPanel(object panel)
    {
        // Get the IUnknown/IInspectable pointer
        var unknown = Marshal.GetIUnknownForObject(panel);
        try
        {
            // Query for ISwapChainPanelNative
            var iid = typeof(ISwapChainPanelNativeRaw).GUID;
            var result = Marshal.QueryInterface(unknown, in iid, out var nativePtr);
            if (result < 0)
            {
                Marshal.ThrowExceptionForHR(result);
            }

            return new ISwapChainPanelNative(nativePtr);
        }
        finally
        {
            Marshal.Release(unknown);
        }
    }

    public int SetSwapChain(IntPtr swapChainPtr)
    {
        var raw = (ISwapChainPanelNativeRaw)Marshal.GetObjectForIUnknown(_nativePtr);
        var hr = raw.SetSwapChain(swapChainPtr);
        Marshal.ReleaseComObject(raw);
        return hr;
    }

    public void Dispose() => Marshal.Release(_nativePtr);
}