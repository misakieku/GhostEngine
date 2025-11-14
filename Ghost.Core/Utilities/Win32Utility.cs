using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using TerraFX.Interop.Windows;

namespace Ghost.Core.Utilities;

[SupportedOSPlatform("windows10.0.19041.0")]
internal static unsafe partial class Win32Utility
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly ref struct IID_PPV
    {
        public readonly Guid* iid;
        public readonly void** ppv;

        public IID_PPV(Guid* iid, void** ppv)
        {
            this.iid = iid;
            this.ppv = ppv;
        }

        public void Deconstruct(out Guid* iid, out void** ppv)
        {
            iid = this.iid;
            ppv = this.ppv;
        }
    }

    public static Guid* IID_NULL
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in TerraFX.Interop.Windows.IID.IID_NULL));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IID_PPV IID_PPV_ARGS<T>(ComPtr<T> comPtr)
        where T : unmanaged, IUnknown.Interface
    {
        return new IID_PPV(Windows.__uuidof<T>(), comPtr.PPV());
    }

    [Conditional("DEBUG")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Assert(this HRESULT hr)
    {
        Debug.Assert(hr.SUCCEEDED, hr.ToString());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfFailed(this HRESULT hr)
    {
        Windows.ThrowIfFailed(hr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid* IID<T>(this ComPtr<T> comPtr)
        where T : unmanaged, IUnknown.Interface
    {
        return Windows.__uuidof<T>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid* IID<T>(this T ptr)
    where T : unmanaged, IUnknown.Interface
    {
        return Windows.__uuidof<T>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void** PPV<T>(ref this ComPtr<T> comPtr)
        where T : unmanaged, IUnknown.Interface
    {
        return (void**)comPtr.GetAddressOf();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void** ReleaseAndGetVoidAddressOf<T>(ref this ComPtr<T> comPtr)
        where T : unmanaged, IUnknown.Interface
    {
        return (void**)comPtr.ReleaseAndGetAddressOf();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ComPtr<T> Move<T>(ref this ComPtr<T> comPtr)
        where T : unmanaged, IUnknown.Interface
    {
        var copy = default(ComPtr<T>);
        comPtr.Swap(ref copy);
        return copy;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasFlag<T>(this uint flags, T flag)
        where T : Enum
    {
        return (flags & Unsafe.As<T, uint>(ref flag)) != 0;
    }
}