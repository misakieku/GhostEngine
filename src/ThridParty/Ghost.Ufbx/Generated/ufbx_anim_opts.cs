namespace Ghost.Ufbx
{
    public partial struct ufbx_anim_opts
    {
        [NativeTypeName("uint32_t")]
        public uint _begin_zero;

        public ufbx_const_uint32_list layer_ids;

        public ufbx_const_real_list override_layer_weights;

        public ufbx_const_prop_override_desc_list prop_overrides;

        public ufbx_const_transform_override_list transform_overrides;

        [NativeTypeName("_Bool")]
        public bool ignore_connections;

        public ufbx_allocator_opts result_allocator;

        [NativeTypeName("uint32_t")]
        public uint _end_zero;
    }
}
