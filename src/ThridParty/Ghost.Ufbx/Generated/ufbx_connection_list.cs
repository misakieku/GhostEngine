namespace Ghost.Ufbx
{
    /// <include file='ufbx_connection_list.xml' path='doc/member[@name="ufbx_connection_list"]/*' />
    public unsafe partial struct ufbx_connection_list
    {
        /// <include file='ufbx_connection_list.xml' path='doc/member[@name="ufbx_connection_list.data"]/*' />
        public ufbx_connection* data;

        /// <include file='ufbx_connection_list.xml' path='doc/member[@name="ufbx_connection_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
