namespace Ghost.Ufbx
{
    public partial struct ufbx_anim
    {
        public double time_begin;

        public double time_end;

        public ufbx_anim_layer_list layers;

        public ufbx_real_list override_layer_weights;

        public ufbx_prop_override_list prop_overrides;

        public ufbx_transform_override_list transform_overrides;

        [NativeTypeName("_Bool")]
        public bool ignore_connections;

        [NativeTypeName("_Bool")]
        public bool custom;
    }
}
