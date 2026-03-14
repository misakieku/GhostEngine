using System.Text;

namespace Ghost.Ufbx;

public static unsafe class UfbxErrorExtensions
{
    /// <summary>
    /// Formats a ufbx_error into a human-readable string using ufbx_format_error.
    /// Allocates a 2KB stack buffer; the result is truncated if the message exceeds that.
    /// </summary>
    public static string FormatError(ref this ufbx_error error)
    {
        const int BufferSize = 2048;
        Span<byte> buffer = stackalloc byte[BufferSize];
        fixed (ufbx_error* pError = &error)
        fixed (byte* pBuffer = buffer)
        {
            var len = Api.ufbx_format_error((sbyte*)pBuffer, (nuint)BufferSize, pError);
            if (len == 0)
                return string.Empty;
            // ufbx_format_error returns the number of characters written (excluding null terminator)
            return Encoding.UTF8.GetString(buffer[..(int)len]);
        }
    }
}
