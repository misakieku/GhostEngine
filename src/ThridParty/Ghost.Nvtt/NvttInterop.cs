using System.Runtime.CompilerServices;
using System.Text;

namespace Ghost.Nvtt;

/// <summary>
/// Internal helpers for converting between managed and unmanaged types.
/// </summary>
internal static unsafe class NvttInterop
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static NvttBoolean ToNvtt(bool value)
        => value ? NvttBoolean.NVTT_True : NvttBoolean.NVTT_False;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool ToBool(NvttBoolean value)
        => value != NvttBoolean.NVTT_False;

    // -------------------------------------------------------------------------
    // String to UTF-8 sbyte*
    //
    // Usage pattern:
    //   fixed (byte* ptr = NvttInterop.ToUtf8(str, stackalloc byte[MaxStack]))
    //       Api.nvttSomething((sbyte*)ptr);
    //
    // For paths longer than MaxStack bytes the helper falls back to a heap
    // allocation via Encoding.UTF8.GetBytes.  The Span<byte> overload lets the
    // caller decide the stackalloc size.
    // -------------------------------------------------------------------------

    internal const int _MAX_STACK_PATH = 512;

    /// <summary>
    /// Encode <paramref name="value"/> as null-terminated UTF-8 into
    /// <paramref name="buffer"/>.  Returns the used portion (including the
    /// null terminator).  If the buffer is too small a new heap array is
    /// returned instead.
    /// </summary>
    internal static Span<byte> ToUtf8(string value, Span<byte> buffer)
    {
        var needed = Encoding.UTF8.GetByteCount(value) + 1; // +1 for null term
        if (needed > buffer.Length)
        {
            buffer = new byte[needed];
        }

        var written = Encoding.UTF8.GetBytes(value, buffer);
        buffer[written] = 0; // null terminator
        return buffer[..(written + 1)];
    }

    internal static string? FromUtf8(sbyte* ptr)
    {
        if (ptr == null)
        {
            return null;
        }

        var len = 0;
        while (ptr[len] != 0)
        {
            len++;
        }

        return Encoding.UTF8.GetString((byte*)ptr, len);
    }
}
