namespace Ghost.Ufbx;

public unsafe struct Light
{
    private ufbx_light* _ptr;

    internal Light(ufbx_light* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public Misaki.HighPerformance.Mathematics.float3 Color => _ptr->color;

    public float Intensity => _ptr->intensity;

    public Misaki.HighPerformance.Mathematics.float3 LocalDirection => _ptr->local_direction;

    public ufbx_light_type Type => _ptr->type;

    public ufbx_light_decay Decay => _ptr->decay;

    public ufbx_light_area_shape AreaShape => _ptr->area_shape;

    public float InnerAngle => _ptr->inner_angle;

    public float OuterAngle => _ptr->outer_angle;

    public bool CastLight => _ptr->cast_light;

    public bool CastShadows => _ptr->cast_shadows;

    public Element Element => new((ufbx_element*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->element));

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public Props Props => new((ufbx_props*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->props));

    public uint ElementId => _ptr->element_id;

    public uint TypedId => _ptr->typed_id;

    public NodeList Instances => new(_ptr->instances.data, _ptr->instances.count);

    internal ufbx_light* GetUnsafePtr() => _ptr;
}
