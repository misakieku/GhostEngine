namespace Ghost.Ufbx
{
    /// <include file='ufbx_void_list.xml' path='doc/member[@name="ufbx_void_list"]/*' />
    public unsafe partial struct ufbx_void_list
    {
        /// <include file='ufbx_void_list.xml' path='doc/member[@name="ufbx_void_list.data"]/*' />
        public void* data;

        /// <include file='ufbx_void_list.xml' path='doc/member[@name="ufbx_void_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
