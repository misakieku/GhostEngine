namespace Ghost.Ufbx
{
    /// <include file='ufbx_bool_list.xml' path='doc/member[@name="ufbx_bool_list"]/*' />
    public unsafe partial struct ufbx_bool_list
    {
        /// <include file='ufbx_bool_list.xml' path='doc/member[@name="ufbx_bool_list.data"]/*' />
        [NativeTypeName("_Bool *")]
        public bool* data;

        /// <include file='ufbx_bool_list.xml' path='doc/member[@name="ufbx_bool_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
