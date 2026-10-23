namespace Ghost.Ufbx
{
    /// <include file='ufbx_anim_opts.xml' path='doc/member[@name="ufbx_anim_opts"]/*' />
    public partial struct ufbx_anim_opts
    {
        /// <include file='ufbx_anim_opts.xml' path='doc/member[@name="ufbx_anim_opts._begin_zero"]/*' />
        [NativeTypeName("uint32_t")]
        public uint _begin_zero;

        /// <include file='ufbx_anim_opts.xml' path='doc/member[@name="ufbx_anim_opts.layer_ids"]/*' />
        public ufbx_const_uint32_list layer_ids;

        /// <include file='ufbx_anim_opts.xml' path='doc/member[@name="ufbx_anim_opts.override_layer_weights"]/*' />
        public ufbx_const_real_list override_layer_weights;

        /// <include file='ufbx_anim_opts.xml' path='doc/member[@name="ufbx_anim_opts.prop_overrides"]/*' />
        public ufbx_const_prop_override_desc_list prop_overrides;

        /// <include file='ufbx_anim_opts.xml' path='doc/member[@name="ufbx_anim_opts.transform_overrides"]/*' />
        public ufbx_const_transform_override_list transform_overrides;

        /// <include file='ufbx_anim_opts.xml' path='doc/member[@name="ufbx_anim_opts.ignore_connections"]/*' />
        [NativeTypeName("_Bool")]
        public bool ignore_connections;

        /// <include file='ufbx_anim_opts.xml' path='doc/member[@name="ufbx_anim_opts.result_allocator"]/*' />
        public ufbx_allocator_opts result_allocator;

        /// <include file='ufbx_anim_opts.xml' path='doc/member[@name="ufbx_anim_opts._end_zero"]/*' />
        [NativeTypeName("uint32_t")]
        public uint _end_zero;
    }
}
