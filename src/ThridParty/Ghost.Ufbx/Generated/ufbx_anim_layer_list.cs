namespace Ghost.Ufbx
{
    /// <include file='ufbx_anim_layer_list.xml' path='doc/member[@name="ufbx_anim_layer_list"]/*' />
    public unsafe partial struct ufbx_anim_layer_list
    {
        /// <include file='ufbx_anim_layer_list.xml' path='doc/member[@name="ufbx_anim_layer_list.data"]/*' />
        public ufbx_anim_layer** data;

        /// <include file='ufbx_anim_layer_list.xml' path='doc/member[@name="ufbx_anim_layer_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
