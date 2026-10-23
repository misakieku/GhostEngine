namespace Ghost.Ufbx
{
    /// <include file='ufbx_uint32_list.xml' path='doc/member[@name="ufbx_uint32_list"]/*' />
    public unsafe partial struct ufbx_uint32_list
    {
        /// <include file='ufbx_uint32_list.xml' path='doc/member[@name="ufbx_uint32_list.data"]/*' />
        [NativeTypeName("uint32_t *")]
        public uint* data;

        /// <include file='ufbx_uint32_list.xml' path='doc/member[@name="ufbx_uint32_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
