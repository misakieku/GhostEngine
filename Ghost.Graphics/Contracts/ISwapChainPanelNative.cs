using System.Runtime.InteropServices;

namespace Ghost.Graphics.Contracts;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
public unsafe readonly struct ISwapChainPanelNative
{
    [ComImport]
    [Guid("63aad0b8-7c24-40ff-85a8-640d944cc325")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface Interface
    {
        // IUnknown: QueryInterface, AddRef, Release
        void QueryInterface(in Guid riid, out IntPtr ppvObject);
        uint AddRef();
        uint Release();

        // SetSwapChain is the 4th slot in the vtable (0-based index 3)
        int SetSwapChain(IntPtr swapChainPtr);
    }

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
            var iid = typeof(Interface).GUID;
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
        var vtbl = *(void***)_nativePtr;
        var setSwapChainFn = (delegate* unmanaged<IntPtr, IntPtr, int>)vtbl[3];
        return setSwapChainFn(_nativePtr, swapChainPtr);
    }

    public void Dispose() => Marshal.Release(_nativePtr);
}