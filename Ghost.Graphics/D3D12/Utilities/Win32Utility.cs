using System.Runtime.CompilerServices;
using TerraFX.Interop.Windows;

namespace Ghost.Graphics.D3D12.Utilities;

internal unsafe static class Win32Utility
{
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

    public static ComPtr<T> Move<T>(this ComPtr<T> comPtr)
        where T : unmanaged, IUnknown.Interface
    {
        ComPtr<T> copy = default;
        Unsafe.AsRef(in comPtr).Swap(ref copy);
        return copy;
    }

    public static bool HasFlag<T>(this uint flags, T flag)
        where T : Enum
    {
        return (flags & Unsafe.As<T, uint>(ref flag)) != 0;
    }
}