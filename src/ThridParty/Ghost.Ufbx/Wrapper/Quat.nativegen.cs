namespace Ghost.Ufbx;

public unsafe struct Quat
{
    private ufbx_quat* _ptr;

    internal Quat(ufbx_quat* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public float X => _ptr->x;

    public float Y => _ptr->y;

    public float Z => _ptr->z;

    public float W => _ptr->w;

    internal ufbx_quat* GetUnsafePtr() => _ptr;
}
