namespace Ghost.Ufbx;

public unsafe struct TransformOverride
{
    private ufbx_transform_override* _ptr;

    internal TransformOverride(ufbx_transform_override* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public uint NodeId => _ptr->node_id;

    public Transform Transform => new((ufbx_transform*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->transform));

    internal ufbx_transform_override* GetUnsafePtr() => _ptr;
}
