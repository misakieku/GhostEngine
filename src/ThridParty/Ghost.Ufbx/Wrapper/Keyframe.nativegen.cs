namespace Ghost.Ufbx;

public unsafe struct Keyframe
{
    private ufbx_keyframe* _ptr;

    internal Keyframe(ufbx_keyframe* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public double Time => _ptr->time;

    public float Value => _ptr->value;

    public ufbx_interpolation Interpolation => _ptr->interpolation;

    public Tangent Left => new((ufbx_tangent*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->left));

    public Tangent Right => new((ufbx_tangent*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->right));

    internal ufbx_keyframe* GetUnsafePtr() => _ptr;
}
