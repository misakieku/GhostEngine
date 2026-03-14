namespace Ghost.Ufbx;

public unsafe struct NurbsCurve
{
    private ufbx_nurbs_curve* _ptr;

    internal NurbsCurve(ufbx_nurbs_curve* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public ufbx_curve_point EvaluateNurbsCurve(float u)
    {
        return Api.ufbx_evaluate_nurbs_curve(_ptr, u);
    }

    public LineCurve TessellateNurbsCurve(TessellateCurveOpts opts, Error error)
    {
        return new(Api.ufbx_tessellate_nurbs_curve(_ptr, opts.GetUnsafePtr(), error.GetUnsafePtr()));
    }

    public NurbsBasis Basis => new((ufbx_nurbs_basis*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->basis));

    public ReadOnlySpan<Misaki.HighPerformance.Mathematics.float4> ControlPoints => _ptr->control_points.data == null ? ReadOnlySpan<Misaki.HighPerformance.Mathematics.float4>.Empty : new ReadOnlySpan<Misaki.HighPerformance.Mathematics.float4>(_ptr->control_points.data, checked((int)_ptr->control_points.count));

    public Element Element => new((ufbx_element*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->element));

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public Props Props => new((ufbx_props*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->props));

    public uint ElementId => _ptr->element_id;

    public uint TypedId => _ptr->typed_id;

    public NodeList Instances => new(_ptr->instances.data, _ptr->instances.count);

    internal ufbx_nurbs_curve* GetUnsafePtr() => _ptr;
}
