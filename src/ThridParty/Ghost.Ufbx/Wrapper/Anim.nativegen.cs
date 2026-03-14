namespace Ghost.Ufbx;

public unsafe ref struct Anim
{
    private ufbx_anim* _ptr;

    internal Anim(ufbx_anim* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public ufbx_prop EvaluatePropLen(Element element, sbyte* name, nuint nameLen, double time)
    {
        return Api.ufbx_evaluate_prop_len(_ptr, element.GetUnsafePtr(), name, nameLen, time);
    }

    public ufbx_prop EvaluateProp(Element element, sbyte* name, double time)
    {
        return Api.ufbx_evaluate_prop(_ptr, element.GetUnsafePtr(), name, time);
    }

    public ufbx_prop EvaluatePropFlagsLen(Element element, sbyte* name, nuint nameLen, double time, uint flags)
    {
        return Api.ufbx_evaluate_prop_flags_len(_ptr, element.GetUnsafePtr(), name, nameLen, time, flags);
    }

    public ufbx_prop EvaluatePropFlags(Element element, sbyte* name, double time, uint flags)
    {
        return Api.ufbx_evaluate_prop_flags(_ptr, element.GetUnsafePtr(), name, time, flags);
    }

    public ufbx_props EvaluateProps(Element element, double time, Prop buffer, nuint bufferSize)
    {
        return Api.ufbx_evaluate_props(_ptr, element.GetUnsafePtr(), time, buffer.GetUnsafePtr(), bufferSize);
    }

    public ufbx_props EvaluatePropsFlags(Element element, double time, Prop buffer, nuint bufferSize, uint flags)
    {
        return Api.ufbx_evaluate_props_flags(_ptr, element.GetUnsafePtr(), time, buffer.GetUnsafePtr(), bufferSize, flags);
    }

    public ufbx_transform EvaluateTransform(Node node, double time)
    {
        return Api.ufbx_evaluate_transform(_ptr, node.GetUnsafePtr(), time);
    }

    public ufbx_transform EvaluateTransformFlags(Node node, double time, uint flags)
    {
        return Api.ufbx_evaluate_transform_flags(_ptr, node.GetUnsafePtr(), time, flags);
    }

    public float EvaluateBlendWeight(BlendChannel channel, double time)
    {
        return Api.ufbx_evaluate_blend_weight(_ptr, channel.GetUnsafePtr(), time);
    }

    public float EvaluateBlendWeightFlags(BlendChannel channel, double time, uint flags)
    {
        return Api.ufbx_evaluate_blend_weight_flags(_ptr, channel.GetUnsafePtr(), time, flags);
    }

    public void FreeAnim()
    {
        Api.ufbx_free_anim(_ptr);
    }

    public void RetainAnim()
    {
        Api.ufbx_retain_anim(_ptr);
    }

    public double TimeBegin => _ptr->time_begin;

    public double TimeEnd => _ptr->time_end;

    public AnimLayerList Layers => new(_ptr->layers.data, _ptr->layers.count);

    public ReadOnlySpan<float> OverrideLayerWeights => _ptr->override_layer_weights.data == null ? ReadOnlySpan<float>.Empty : new ReadOnlySpan<float>(_ptr->override_layer_weights.data, checked((int)_ptr->override_layer_weights.count));

    public ReadOnlySpan<ufbx_prop_override> PropOverrides => _ptr->prop_overrides.data == null ? ReadOnlySpan<ufbx_prop_override>.Empty : new ReadOnlySpan<ufbx_prop_override>(_ptr->prop_overrides.data, checked((int)_ptr->prop_overrides.count));

    public ReadOnlySpan<ufbx_transform_override> TransformOverrides => _ptr->transform_overrides.data == null ? ReadOnlySpan<ufbx_transform_override>.Empty : new ReadOnlySpan<ufbx_transform_override>(_ptr->transform_overrides.data, checked((int)_ptr->transform_overrides.count));

    public bool IgnoreConnections => _ptr->ignore_connections;

    public bool Custom => _ptr->custom;

    internal ufbx_anim* GetUnsafePtr() => _ptr;
}
