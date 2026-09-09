namespace Ghost.Ufbx
{
    /// <include file='ufbx_int32_list.xml' path='doc/member[@name="ufbx_int32_list"]/*' />
    public unsafe partial struct ufbx_int32_list
    {
        /// <include file='ufbx_int32_list.xml' path='doc/member[@name="ufbx_int32_list.data"]/*' />
        [NativeTypeName("int32_t *")]
        public int* data;

        /// <include file='ufbx_int32_list.xml' path='doc/member[@name="ufbx_int32_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
