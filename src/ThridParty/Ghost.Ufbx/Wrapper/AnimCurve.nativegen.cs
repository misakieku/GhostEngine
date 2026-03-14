namespace Ghost.Ufbx;

public unsafe struct AnimCurve
{
    private ufbx_anim_curve* _ptr;

    internal AnimCurve(ufbx_anim_curve* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public float EvaluateCurve(double time, float defaultValue)
    {
        return Api.ufbx_evaluate_curve(_ptr, time, defaultValue);
    }

    public float EvaluateCurveFlags(double time, float defaultValue, uint flags)
    {
        return Api.ufbx_evaluate_curve_flags(_ptr, time, defaultValue, flags);
    }

    public ReadOnlySpan<ufbx_keyframe> Keyframes => _ptr->keyframes.data == null ? ReadOnlySpan<ufbx_keyframe>.Empty : new ReadOnlySpan<ufbx_keyframe>(_ptr->keyframes.data, checked((int)_ptr->keyframes.count));

    public Extrapolation PreExtrapolation => new((ufbx_extrapolation*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->pre_extrapolation));

    public Extrapolation PostExtrapolation => new((ufbx_extrapolation*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->post_extrapolation));

    public float MinValue => _ptr->min_value;

    public float MaxValue => _ptr->max_value;

    public double MinTime => _ptr->min_time;

    public double MaxTime => _ptr->max_time;

    public Element Element => new((ufbx_element*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->element));

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public Props Props => new((ufbx_props*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->props));

    public uint ElementId => _ptr->element_id;

    public uint TypedId => _ptr->typed_id;

    internal ufbx_anim_curve* GetUnsafePtr() => _ptr;
}
