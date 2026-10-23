namespace Ghost.Ufbx
{
    /// <include file='ufbx_blob_list.xml' path='doc/member[@name="ufbx_blob_list"]/*' />
    public unsafe partial struct ufbx_blob_list
    {
        /// <include file='ufbx_blob_list.xml' path='doc/member[@name="ufbx_blob_list.data"]/*' />
        public ufbx_blob* data;

        /// <include file='ufbx_blob_list.xml' path='doc/member[@name="ufbx_blob_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
