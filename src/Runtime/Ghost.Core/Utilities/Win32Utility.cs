using Misaki.HighPerformance.LowLevel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using TerraFX.Interop.Windows;

namespace Ghost.Core.Utilities;

[SupportedOSPlatform("windows10.0.19041.0")]
internal static unsafe partial class Win32Utility
{

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

    extension(MemoryLeakException)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfRefCountNonZero(uint count)
        {
            if (count != 0)
            {
                throw new MemoryLeakException($"Reference count is not zero: {count}");
            }
        }
    }
}