namespace Ghost.Ufbx
{
    /// <include file='ufbx_color_set_list.xml' path='doc/member[@name="ufbx_color_set_list"]/*' />
    public unsafe partial struct ufbx_color_set_list
    {
        /// <include file='ufbx_color_set_list.xml' path='doc/member[@name="ufbx_color_set_list.data"]/*' />
        public ufbx_color_set* data;

        /// <include file='ufbx_color_set_list.xml' path='doc/member[@name="ufbx_color_set_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
