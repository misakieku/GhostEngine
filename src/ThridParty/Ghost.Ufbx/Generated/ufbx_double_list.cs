namespace Ghost.Ufbx
{
    /// <include file='ufbx_double_list.xml' path='doc/member[@name="ufbx_double_list"]/*' />
    public unsafe partial struct ufbx_double_list
    {
        /// <include file='ufbx_double_list.xml' path='doc/member[@name="ufbx_double_list.data"]/*' />
        public double* data;

        /// <include file='ufbx_double_list.xml' path='doc/member[@name="ufbx_double_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
