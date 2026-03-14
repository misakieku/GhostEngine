namespace Ghost.Ufbx;

public unsafe struct Extrapolation
{
    private ufbx_extrapolation* _ptr;

    internal Extrapolation(ufbx_extrapolation* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public ufbx_extrapolation_mode Mode => _ptr->mode;

    public int RepeatCount => _ptr->repeat_count;

    internal ufbx_extrapolation* GetUnsafePtr() => _ptr;
}
