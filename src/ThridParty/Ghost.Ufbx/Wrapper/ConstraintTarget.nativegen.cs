namespace Ghost.Ufbx;

public unsafe struct ConstraintTarget
{
    private ufbx_constraint_target* _ptr;

    internal ConstraintTarget(ufbx_constraint_target* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public bool HasNode => _ptr->node != null;
    public Node Node => _ptr->node != null ? new(_ptr->node) : throw new InvalidOperationException("Node is null.");

    public float Weight => _ptr->weight;

    public Transform Transform => new((ufbx_transform*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->transform));

    internal ufbx_constraint_target* GetUnsafePtr() => _ptr;
}
