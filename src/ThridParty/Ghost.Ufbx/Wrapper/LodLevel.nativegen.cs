namespace Ghost.Ufbx;

public unsafe struct LodLevel
{
    private ufbx_lod_level* _ptr;

    internal LodLevel(ufbx_lod_level* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public float Distance => _ptr->distance;

    public ufbx_lod_display Display => _ptr->display;

    internal ufbx_lod_level* GetUnsafePtr() => _ptr;
}
