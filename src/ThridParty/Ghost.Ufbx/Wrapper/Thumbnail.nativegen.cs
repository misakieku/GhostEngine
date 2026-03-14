namespace Ghost.Ufbx;

public unsafe struct Thumbnail
{
    private ufbx_thumbnail* _ptr;

    internal Thumbnail(ufbx_thumbnail* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public Props Props => new((ufbx_props*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->props));

    public uint Width => _ptr->width;

    public uint Height => _ptr->height;

    public ufbx_thumbnail_format Format => _ptr->format;

    public ReadOnlySpan<byte> Data => NativeWrapperHelpers.AsSpan(_ptr->data);

    internal ufbx_thumbnail* GetUnsafePtr() => _ptr;
}
