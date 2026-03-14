namespace Ghost.Ufbx;

public unsafe struct AnimOpts
{
    private ufbx_anim_opts* _ptr;

    internal AnimOpts(ufbx_anim_opts* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public ReadOnlySpan<uint> LayerIds => _ptr->layer_ids.data == null ? ReadOnlySpan<uint>.Empty : new ReadOnlySpan<uint>(_ptr->layer_ids.data, checked((int)_ptr->layer_ids.count));

    public ReadOnlySpan<float> OverrideLayerWeights => _ptr->override_layer_weights.data == null ? ReadOnlySpan<float>.Empty : new ReadOnlySpan<float>(_ptr->override_layer_weights.data, checked((int)_ptr->override_layer_weights.count));

    public ReadOnlySpan<ufbx_prop_override_desc> PropOverrides => _ptr->prop_overrides.data == null ? ReadOnlySpan<ufbx_prop_override_desc>.Empty : new ReadOnlySpan<ufbx_prop_override_desc>(_ptr->prop_overrides.data, checked((int)_ptr->prop_overrides.count));

    public ReadOnlySpan<ufbx_transform_override> TransformOverrides => _ptr->transform_overrides.data == null ? ReadOnlySpan<ufbx_transform_override>.Empty : new ReadOnlySpan<ufbx_transform_override>(_ptr->transform_overrides.data, checked((int)_ptr->transform_overrides.count));

    public bool IgnoreConnections => _ptr->ignore_connections;

    public AllocatorOpts ResultAllocator => new((ufbx_allocator_opts*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->result_allocator));

    internal ufbx_anim_opts* GetUnsafePtr() => _ptr;
}
