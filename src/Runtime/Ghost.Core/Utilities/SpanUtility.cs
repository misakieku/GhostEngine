using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Core.Utilities;

public static class SpanUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<byte> AsBytes<T>(ref T value)
        where T : struct
    {
        return MemoryMarshal.AsBytes(new Span<T>(ref value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T AsRef<T>(Span<byte> bytes)
        where T : struct
    {
        Logger.DebugAssert(bytes.Length >= Unsafe.SizeOf<T>(), "Byte span is too small to contain the target type.");
        return ref MemoryMarshal.AsRef<T>(bytes);
    }
}
