namespace Ghost.Ufbx
{
    /// <include file='ufbx_anim.xml' path='doc/member[@name="ufbx_anim"]/*' />
    public partial struct ufbx_anim
    {
        /// <include file='ufbx_anim.xml' path='doc/member[@name="ufbx_anim.time_begin"]/*' />
        public double time_begin;

        /// <include file='ufbx_anim.xml' path='doc/member[@name="ufbx_anim.time_end"]/*' />
        public double time_end;

        /// <include file='ufbx_anim.xml' path='doc/member[@name="ufbx_anim.layers"]/*' />
        public ufbx_anim_layer_list layers;

        /// <include file='ufbx_anim.xml' path='doc/member[@name="ufbx_anim.override_layer_weights"]/*' />
        public ufbx_real_list override_layer_weights;

        /// <include file='ufbx_anim.xml' path='doc/member[@name="ufbx_anim.prop_overrides"]/*' />
        public ufbx_prop_override_list prop_overrides;

        /// <include file='ufbx_anim.xml' path='doc/member[@name="ufbx_anim.transform_overrides"]/*' />
        public ufbx_transform_override_list transform_overrides;

        /// <include file='ufbx_anim.xml' path='doc/member[@name="ufbx_anim.ignore_connections"]/*' />
        [NativeTypeName("_Bool")]
        public bool ignore_connections;

        /// <include file='ufbx_anim.xml' path='doc/member[@name="ufbx_anim.custom"]/*' />
        [NativeTypeName("_Bool")]
        public bool custom;
    }
}
