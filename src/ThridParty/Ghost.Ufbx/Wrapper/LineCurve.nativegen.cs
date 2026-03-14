namespace Ghost.Ufbx;

public unsafe struct LineCurve
{
    private ufbx_line_curve* _ptr;

    internal LineCurve(ufbx_line_curve* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public void FreeLineCurve()
    {
        Api.ufbx_free_line_curve(_ptr);
    }

    public void RetainLineCurve()
    {
        Api.ufbx_retain_line_curve(_ptr);
    }

    public Misaki.HighPerformance.Mathematics.float3 Color => _ptr->color;

    public ReadOnlySpan<Misaki.HighPerformance.Mathematics.float3> ControlPoints => _ptr->control_points.data == null ? ReadOnlySpan<Misaki.HighPerformance.Mathematics.float3>.Empty : new ReadOnlySpan<Misaki.HighPerformance.Mathematics.float3>(_ptr->control_points.data, checked((int)_ptr->control_points.count));

    public ReadOnlySpan<uint> PointIndices => _ptr->point_indices.data == null ? ReadOnlySpan<uint>.Empty : new ReadOnlySpan<uint>(_ptr->point_indices.data, checked((int)_ptr->point_indices.count));

    public ReadOnlySpan<ufbx_line_segment> Segments => _ptr->segments.data == null ? ReadOnlySpan<ufbx_line_segment>.Empty : new ReadOnlySpan<ufbx_line_segment>(_ptr->segments.data, checked((int)_ptr->segments.count));

    public bool FromTessellatedNurbs => _ptr->from_tessellated_nurbs;

    public Element Element => new((ufbx_element*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->element));

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public Props Props => new((ufbx_props*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->props));

    public uint ElementId => _ptr->element_id;

    public uint TypedId => _ptr->typed_id;

    public NodeList Instances => new(_ptr->instances.data, _ptr->instances.count);

    internal ufbx_line_curve* GetUnsafePtr() => _ptr;
}
