namespace Ghost.Ufbx;

public unsafe struct NurbsBasis
{
    private ufbx_nurbs_basis* _ptr;

    internal NurbsBasis(ufbx_nurbs_basis* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public nuint EvaluateNurbsBasis(float u, float* weights, nuint numWeights, float* derivatives, nuint numDerivatives)
    {
        return Api.ufbx_evaluate_nurbs_basis(_ptr, u, weights, numWeights, derivatives, numDerivatives);
    }

    public uint Order => _ptr->order;

    public ufbx_nurbs_topology Topology => _ptr->topology;

    public ReadOnlySpan<float> KnotVector => _ptr->knot_vector.data == null ? ReadOnlySpan<float>.Empty : new ReadOnlySpan<float>(_ptr->knot_vector.data, checked((int)_ptr->knot_vector.count));

    public float TMin => _ptr->t_min;

    public float TMax => _ptr->t_max;

    public ReadOnlySpan<float> Spans => _ptr->spans.data == null ? ReadOnlySpan<float>.Empty : new ReadOnlySpan<float>(_ptr->spans.data, checked((int)_ptr->spans.count));

    public bool Is2d => _ptr->is_2d;

    public nuint NumWrapControlPoints => _ptr->num_wrap_control_points;

    public bool Valid => _ptr->valid;

    internal ufbx_nurbs_basis* GetUnsafePtr() => _ptr;
}
