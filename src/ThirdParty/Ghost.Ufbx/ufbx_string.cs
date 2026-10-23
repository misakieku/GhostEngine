namespace Ghost.Ufbx;

public unsafe partial struct ufbx_string
{
    public readonly ReadOnlySpan<byte> AsSpan()
    {
        if (data == null || length == 0)
        {
            return ReadOnlySpan<byte>.Empty;
        }

        return new ReadOnlySpan<byte>((byte*)data, checked((int)length));
    }

    public override readonly string ToString()
    {
        if (data == null || length == 0)
        {
            return string.Empty;
        }

        return new string(data, 0, checked((int)length));
    }

    public static implicit operator ReadOnlySpan<byte>(ufbx_string s) => s.AsSpan();
}