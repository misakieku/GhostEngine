using System.Text;

namespace Ghost.Ufbx;

public unsafe partial struct ufbx_string
{
    public readonly ReadOnlySpan<byte> AsSpan()
    {
        if (data == null || length == 0)
            return ReadOnlySpan<byte>.Empty;
        return new ReadOnlySpan<byte>((byte*)data, checked((int)length));
    }

    public static implicit operator ReadOnlySpan<byte>(ufbx_string s) => s.AsSpan();

    public override readonly string ToString()
    {
        var span = AsSpan();
        return span.IsEmpty ? string.Empty : Encoding.UTF8.GetString(span);
    }
}