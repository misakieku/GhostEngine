namespace Ghost.Ufbx;

public unsafe struct Connection
{
    private ufbx_connection* _ptr;

    internal Connection(ufbx_connection* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public bool HasSrc => _ptr->src != null;
    public Element Src => _ptr->src != null ? new(_ptr->src) : throw new InvalidOperationException("Src is null.");

    public bool HasDst => _ptr->dst != null;
    public Element Dst => _ptr->dst != null ? new(_ptr->dst) : throw new InvalidOperationException("Dst is null.");

    public ReadOnlySpan<byte> SrcPropBytes => NativeWrapperHelpers.AsByteSpan(_ptr->src_prop);
    public string SrcProp => NativeWrapperHelpers.GetString(_ptr->src_prop);

    public ReadOnlySpan<byte> DstPropBytes => NativeWrapperHelpers.AsByteSpan(_ptr->dst_prop);
    public string DstProp => NativeWrapperHelpers.GetString(_ptr->dst_prop);

    internal ufbx_connection* GetUnsafePtr() => _ptr;
}
