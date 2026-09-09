namespace Ghost.Ufbx
{
    /// <include file='ufbx_real_list.xml' path='doc/member[@name="ufbx_real_list"]/*' />
    public unsafe partial struct ufbx_real_list
    {
        /// <include file='ufbx_real_list.xml' path='doc/member[@name="ufbx_real_list.data"]/*' />
        [NativeTypeName("ufbx_real *")]
        public float* data;

        /// <include file='ufbx_real_list.xml' path='doc/member[@name="ufbx_real_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
