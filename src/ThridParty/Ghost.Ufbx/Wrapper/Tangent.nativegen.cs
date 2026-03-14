namespace Ghost.Ufbx;

public unsafe struct Tangent
{
    private ufbx_tangent* _ptr;

    internal Tangent(ufbx_tangent* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public float Dx => _ptr->dx;

    public float Dy => _ptr->dy;

    internal ufbx_tangent* GetUnsafePtr() => _ptr;
}
