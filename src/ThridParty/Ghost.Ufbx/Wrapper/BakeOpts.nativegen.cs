namespace Ghost.Ufbx;

public unsafe struct BakeOpts
{
    private ufbx_bake_opts* _ptr;

    internal BakeOpts(ufbx_bake_opts* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public AllocatorOpts TempAllocator => new((ufbx_allocator_opts*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->temp_allocator));

    public AllocatorOpts ResultAllocator => new((ufbx_allocator_opts*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->result_allocator));

    public bool TrimStartTime => _ptr->trim_start_time;

    public double ResampleRate => _ptr->resample_rate;

    public double MinimumSampleRate => _ptr->minimum_sample_rate;

    public double MaximumSampleRate => _ptr->maximum_sample_rate;

    public bool BakeTransformProps => _ptr->bake_transform_props;

    public bool SkipNodeTransforms => _ptr->skip_node_transforms;

    public bool NoResampleRotation => _ptr->no_resample_rotation;

    public bool IgnoreLayerWeightAnimation => _ptr->ignore_layer_weight_animation;

    public nuint MaxKeyframeSegments => _ptr->max_keyframe_segments;

    public ufbx_bake_step_handling StepHandling => _ptr->step_handling;

    public double StepCustomDuration => _ptr->step_custom_duration;

    public double StepCustomEpsilon => _ptr->step_custom_epsilon;

    public uint EvaluateFlags => _ptr->evaluate_flags;

    public bool KeyReductionEnabled => _ptr->key_reduction_enabled;

    public bool KeyReductionRotation => _ptr->key_reduction_rotation;

    public double KeyReductionThreshold => _ptr->key_reduction_threshold;

    public nuint KeyReductionPasses => _ptr->key_reduction_passes;

    internal ufbx_bake_opts* GetUnsafePtr() => _ptr;
}
