namespace Ghost.Ufbx
{
    /// <include file='ufbx_float_list.xml' path='doc/member[@name="ufbx_float_list"]/*' />
    public unsafe partial struct ufbx_float_list
    {
        /// <include file='ufbx_float_list.xml' path='doc/member[@name="ufbx_float_list.data"]/*' />
        public float* data;

        /// <include file='ufbx_float_list.xml' path='doc/member[@name="ufbx_float_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
