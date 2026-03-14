namespace Ghost.Ufbx;

public unsafe struct AnimValue
{
    private ufbx_anim_value* _ptr;

    internal AnimValue(ufbx_anim_value* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public float EvaluateAnimValueReal(double time)
    {
        return Api.ufbx_evaluate_anim_value_real(_ptr, time);
    }

    public Misaki.HighPerformance.Mathematics.float3 EvaluateAnimValueVec3(double time)
    {
        return Api.ufbx_evaluate_anim_value_vec3(_ptr, time);
    }

    public float EvaluateAnimValueRealFlags(double time, uint flags)
    {
        return Api.ufbx_evaluate_anim_value_real_flags(_ptr, time, flags);
    }

    public Misaki.HighPerformance.Mathematics.float3 EvaluateAnimValueVec3Flags(double time, uint flags)
    {
        return Api.ufbx_evaluate_anim_value_vec3_flags(_ptr, time, flags);
    }

    public Misaki.HighPerformance.Mathematics.float3 DefaultValue => _ptr->default_value;

    public Element Element => new((ufbx_element*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->element));

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public Props Props => new((ufbx_props*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->props));

    public uint ElementId => _ptr->element_id;

    public uint TypedId => _ptr->typed_id;

    internal ufbx_anim_value* GetUnsafePtr() => _ptr;
}
