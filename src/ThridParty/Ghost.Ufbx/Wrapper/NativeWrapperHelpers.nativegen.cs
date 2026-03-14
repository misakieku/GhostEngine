using System.Text;

namespace Ghost.Ufbx;

internal static unsafe class NativeWrapperHelpers
{
    public static ReadOnlySpan<byte> AsByteSpan(ufbx_string value)
    {
        if (value.data == null || value.length == 0)
        {
            return ReadOnlySpan<byte>.Empty;
        }

        return new ReadOnlySpan<byte>((byte*)value.data, checked((int)value.length) * 1);
    }

    public static string GetString(ufbx_string value)
    {
        var bytes = AsByteSpan(value);
        if (bytes.IsEmpty)
        {
            return string.Empty;
        }

        return Encoding.UTF8.GetString(bytes);
    }

    public static ReadOnlySpan<byte> AsSpan(ufbx_blob value)
    {
        if (value.data == null || value.size == 0)
        {
            return ReadOnlySpan<byte>.Empty;
        }

        return new ReadOnlySpan<byte>(value.data, checked((int)value.size));
    }

    public static void ThrowIfOutOfRange(int index, int count)
    {
        if ((uint)index >= (uint)count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}
