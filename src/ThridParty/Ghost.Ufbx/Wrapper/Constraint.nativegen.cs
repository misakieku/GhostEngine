namespace Ghost.Ufbx;

public unsafe struct Constraint
{
    private ufbx_constraint* _ptr;

    internal Constraint(ufbx_constraint* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public ufbx_constraint_type Type => _ptr->type;

    public ReadOnlySpan<byte> TypeNameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->type_name);
    public string TypeName => NativeWrapperHelpers.GetString(_ptr->type_name);

    public bool HasNode => _ptr->node != null;
    public Node Node => _ptr->node != null ? new(_ptr->node) : throw new InvalidOperationException("Node is null.");

    public ReadOnlySpan<ufbx_constraint_target> Targets => _ptr->targets.data == null ? ReadOnlySpan<ufbx_constraint_target>.Empty : new ReadOnlySpan<ufbx_constraint_target>(_ptr->targets.data, checked((int)_ptr->targets.count));

    public float Weight => _ptr->weight;

    public bool Active => _ptr->active;

    public Transform TransformOffset => new((ufbx_transform*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->transform_offset));

    public Misaki.HighPerformance.Mathematics.float3 AimVector => _ptr->aim_vector;

    public ufbx_constraint_aim_up_type AimUpType => _ptr->aim_up_type;

    public bool HasAimUpNode => _ptr->aim_up_node != null;
    public Node AimUpNode => _ptr->aim_up_node != null ? new(_ptr->aim_up_node) : throw new InvalidOperationException("AimUpNode is null.");

    public Misaki.HighPerformance.Mathematics.float3 AimUpVector => _ptr->aim_up_vector;

    public bool HasIkEffector => _ptr->ik_effector != null;
    public Node IkEffector => _ptr->ik_effector != null ? new(_ptr->ik_effector) : throw new InvalidOperationException("IkEffector is null.");

    public bool HasIkEndNode => _ptr->ik_end_node != null;
    public Node IkEndNode => _ptr->ik_end_node != null ? new(_ptr->ik_end_node) : throw new InvalidOperationException("IkEndNode is null.");

    public Misaki.HighPerformance.Mathematics.float3 IkPoleVector => _ptr->ik_pole_vector;

    public Element Element => new((ufbx_element*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->element));

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public Props Props => new((ufbx_props*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->props));

    public uint ElementId => _ptr->element_id;

    public uint TypedId => _ptr->typed_id;

    internal ufbx_constraint* GetUnsafePtr() => _ptr;
}
