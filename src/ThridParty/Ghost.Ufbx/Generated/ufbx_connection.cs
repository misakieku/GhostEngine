namespace Ghost.Ufbx
{
    /// <include file='ufbx_connection.xml' path='doc/member[@name="ufbx_connection"]/*' />
    public unsafe partial struct ufbx_connection
    {
        /// <include file='ufbx_connection.xml' path='doc/member[@name="ufbx_connection.src"]/*' />
        public ufbx_element* src;

        /// <include file='ufbx_connection.xml' path='doc/member[@name="ufbx_connection.dst"]/*' />
        public ufbx_element* dst;

        /// <include file='ufbx_connection.xml' path='doc/member[@name="ufbx_connection.src_prop"]/*' />
        public ufbx_string src_prop;

        /// <include file='ufbx_connection.xml' path='doc/member[@name="ufbx_connection.dst_prop"]/*' />
        public ufbx_string dst_prop;
    }
}
