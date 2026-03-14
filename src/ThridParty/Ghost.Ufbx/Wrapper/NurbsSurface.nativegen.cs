namespace Ghost.Ufbx;

public unsafe struct NurbsSurface
{
    private ufbx_nurbs_surface* _ptr;

    internal NurbsSurface(ufbx_nurbs_surface* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public ufbx_surface_point EvaluateNurbsSurface(float u, float v)
    {
        return Api.ufbx_evaluate_nurbs_surface(_ptr, u, v);
    }

    public Mesh TessellateNurbsSurface(TessellateSurfaceOpts opts, Error error)
    {
        return new(Api.ufbx_tessellate_nurbs_surface(_ptr, opts.GetUnsafePtr(), error.GetUnsafePtr()));
    }

    public NurbsBasis BasisU => new((ufbx_nurbs_basis*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->basis_u));

    public NurbsBasis BasisV => new((ufbx_nurbs_basis*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->basis_v));

    public nuint NumControlPointsU => _ptr->num_control_points_u;

    public nuint NumControlPointsV => _ptr->num_control_points_v;

    public ReadOnlySpan<Misaki.HighPerformance.Mathematics.float4> ControlPoints => _ptr->control_points.data == null ? ReadOnlySpan<Misaki.HighPerformance.Mathematics.float4>.Empty : new ReadOnlySpan<Misaki.HighPerformance.Mathematics.float4>(_ptr->control_points.data, checked((int)_ptr->control_points.count));

    public uint SpanSubdivisionU => _ptr->span_subdivision_u;

    public uint SpanSubdivisionV => _ptr->span_subdivision_v;

    public bool FlipNormals => _ptr->flip_normals;

    public bool HasMaterial => _ptr->material != null;
    public Material Material => _ptr->material != null ? new(_ptr->material) : throw new InvalidOperationException("Material is null.");

    public Element Element => new((ufbx_element*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->element));

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public Props Props => new((ufbx_props*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->props));

    public uint ElementId => _ptr->element_id;

    public uint TypedId => _ptr->typed_id;

    public NodeList Instances => new(_ptr->instances.data, _ptr->instances.count);

    internal ufbx_nurbs_surface* GetUnsafePtr() => _ptr;
}
