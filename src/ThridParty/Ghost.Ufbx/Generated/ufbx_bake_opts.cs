namespace Ghost.Ufbx
{
    public partial struct ufbx_bake_opts
    {
        [NativeTypeName("uint32_t")]
        public uint _begin_zero;

        public ufbx_allocator_opts temp_allocator;

        public ufbx_allocator_opts result_allocator;

        [NativeTypeName("_Bool")]
        public bool trim_start_time;

        public double resample_rate;

        public double minimum_sample_rate;

        public double maximum_sample_rate;

        [NativeTypeName("_Bool")]
        public bool bake_transform_props;

        [NativeTypeName("_Bool")]
        public bool skip_node_transforms;

        [NativeTypeName("_Bool")]
        public bool no_resample_rotation;

        [NativeTypeName("_Bool")]
        public bool ignore_layer_weight_animation;

        [NativeTypeName("size_t")]
        public nuint max_keyframe_segments;

        public ufbx_bake_step_handling step_handling;

        public double step_custom_duration;

        public double step_custom_epsilon;

        [NativeTypeName("uint32_t")]
        public uint evaluate_flags;

        [NativeTypeName("_Bool")]
        public bool key_reduction_enabled;

        [NativeTypeName("_Bool")]
        public bool key_reduction_rotation;

        public double key_reduction_threshold;

        [NativeTypeName("size_t")]
        public nuint key_reduction_passes;

        [NativeTypeName("uint32_t")]
        public uint _end_zero;
    }
}
