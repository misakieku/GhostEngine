namespace Ghost.Ufbx
{
    /// <include file='ufbx_texture_layer_list.xml' path='doc/member[@name="ufbx_texture_layer_list"]/*' />
    public unsafe partial struct ufbx_texture_layer_list
    {
        /// <include file='ufbx_texture_layer_list.xml' path='doc/member[@name="ufbx_texture_layer_list.data"]/*' />
        public ufbx_texture_layer* data;

        /// <include file='ufbx_texture_layer_list.xml' path='doc/member[@name="ufbx_texture_layer_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
