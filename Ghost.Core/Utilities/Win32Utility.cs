using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using TerraFX.Interop.Windows;

namespace Ghost.Core.Utilities;

#if PLATEFORME_WIN64
[SupportedOSPlatform("windows10.0.19041.0")]
internal unsafe static class Win32Utility
{
    public static Guid* IID_NULL => (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in IID.IID_NULL));

    [Conditional("DEBUG")]
    public static void Assert(this HRESULT hr)
    {
        Debug.Assert(hr.SUCCEEDED);
    }

    public static void ThrowIfFailed(this HRESULT hr)
    {
        Windows.ThrowIfFailed(hr);
    }

    public static void** GetVoidAddressOf<T>(this ComPtr<T> comPtr)
        where T : unmanaged, IUnknown.Interface
    {
        return (void**)comPtr.GetAddressOf();
    }

    public static void** ReleaseAndGetVoidAddressOf<T>(this ComPtr<T> comPtr)
        where T : unmanaged, IUnknown.Interface
    {
        return (void**)comPtr.ReleaseAndGetAddressOf();
    }

    public static ComPtr<T> Move<T>(ref this ComPtr<T> comPtr)
        where T : unmanaged, IUnknown.Interface
    {
        var copy = default(ComPtr<T>);
        comPtr.Swap(ref copy);
        return copy;
    }

    public static bool HasFlag<T>(this uint flags, T flag)
        where T : Enum
    {
        return (flags & Unsafe.As<T, uint>(ref flag)) != 0;
    }
}
#endif