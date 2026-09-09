namespace Ghost.Ufbx
{
    /// <include file='ufbx_blend_shape_list.xml' path='doc/member[@name="ufbx_blend_shape_list"]/*' />
    public unsafe partial struct ufbx_blend_shape_list
    {
        /// <include file='ufbx_blend_shape_list.xml' path='doc/member[@name="ufbx_blend_shape_list.data"]/*' />
        public ufbx_blend_shape** data;

        /// <include file='ufbx_blend_shape_list.xml' path='doc/member[@name="ufbx_blend_shape_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
