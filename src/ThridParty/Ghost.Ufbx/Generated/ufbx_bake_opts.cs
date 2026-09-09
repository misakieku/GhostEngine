namespace Ghost.Ufbx
{
    /// <include file='ufbx_bake_opts.xml' path='doc/member[@name="ufbx_bake_opts"]/*' />
    public partial struct ufbx_bake_opts
    {
        /// <include file='ufbx_bake_opts.xml' path='doc/member[@name="ufbx_bake_opts._begin_zero"]/*' />
        [NativeTypeName("uint32_t")]
        public uint _begin_zero;

        /// <include file='ufbx_bake_opts.xml' path='doc/member[@name="ufbx_bake_opts.temp_allocator"]/*' />
        public ufbx_allocator_opts temp_allocator;

        /// <include file='ufbx_bake_opts.xml' path='doc/member[@name="ufbx_bake_opts.result_allocator"]/*' />
        public ufbx_allocator_opts result_allocator;

        /// <include file='ufbx_bake_opts.xml' path='doc/member[@name="ufbx_bake_opts.trim_start_time"]/*' />
        [NativeTypeName("_Bool")]
        public bool trim_start_time;

        /// <include file='ufbx_bake_opts.xml' path='doc/member[@name="ufbx_bake_opts.resample_rate"]/*' />
        public double resample_rate;

        /// <include file='ufbx_bake_opts.xml' path='doc/member[@name="ufbx_bake_opts.minimum_sample_rate"]/*' />
        public double minimum_sample_rate;

        /// <include file='ufbx_bake_opts.xml' path='doc/member[@name="ufbx_bake_opts.maximum_sample_rate"]/*' />
        public double maximum_sample_rate;

        /// <include file='ufbx_bake_opts.xml' path='doc/member[@name="ufbx_bake_opts.bake_transform_props"]/*' />
        [NativeTypeName("_Bool")]
        public bool bake_transform_props;

        /// <include file='ufbx_bake_opts.xml' path='doc/member[@name="ufbx_bake_opts.skip_node_transforms"]/*' />
        [NativeTypeName("_Bool")]
        public bool skip_node_transforms;

        /// <include file='ufbx_bake_opts.xml' path='doc/member[@name="ufbx_bake_opts.no_resample_rotation"]/*' />
        [NativeTypeName("_Bool")]
        public bool no_resample_rotation;

        /// <include file='ufbx_bake_opts.xml' path='doc/member[@name="ufbx_bake_opts.ignore_layer_weight_animation"]/*' />
        [NativeTypeName("_Bool")]
        public bool ignore_layer_weight_animation;

        /// <include file='ufbx_bake_opts.xml' path='doc/member[@name="ufbx_bake_opts.max_keyframe_segments"]/*' />
        [NativeTypeName("size_t")]
        public nuint max_keyframe_segments;

        /// <include file='ufbx_bake_opts.xml' path='doc/member[@name="ufbx_bake_opts.step_handling"]/*' />
        public ufbx_bake_step_handling step_handling;

        /// <include file='ufbx_bake_opts.xml' path='doc/member[@name="ufbx_bake_opts.step_custom_duration"]/*' />
        public double step_custom_duration;

        /// <include file='ufbx_bake_opts.xml' path='doc/member[@name="ufbx_bake_opts.step_custom_epsilon"]/*' />
        public double step_custom_epsilon;

        /// <include file='ufbx_bake_opts.xml' path='doc/member[@name="ufbx_bake_opts.evaluate_flags"]/*' />
        [NativeTypeName("uint32_t")]
        public uint evaluate_flags;

        /// <include file='ufbx_bake_opts.xml' path='doc/member[@name="ufbx_bake_opts.key_reduction_enabled"]/*' />
        [NativeTypeName("_Bool")]
        public bool key_reduction_enabled;

        /// <include file='ufbx_bake_opts.xml' path='doc/member[@name="ufbx_bake_opts.key_reduction_rotation"]/*' />
        [NativeTypeName("_Bool")]
        public bool key_reduction_rotation;

        /// <include file='ufbx_bake_opts.xml' path='doc/member[@name="ufbx_bake_opts.key_reduction_threshold"]/*' />
        public double key_reduction_threshold;

        /// <include file='ufbx_bake_opts.xml' path='doc/member[@name="ufbx_bake_opts.key_reduction_passes"]/*' />
        [NativeTypeName("size_t")]
        public nuint key_reduction_passes;

        /// <include file='ufbx_bake_opts.xml' path='doc/member[@name="ufbx_bake_opts._end_zero"]/*' />
        [NativeTypeName("uint32_t")]
        public uint _end_zero;
    }
}
