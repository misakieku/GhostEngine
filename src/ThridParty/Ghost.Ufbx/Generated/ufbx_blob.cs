namespace Ghost.Ufbx
{
    /// <include file='ufbx_blob.xml' path='doc/member[@name="ufbx_blob"]/*' />
    public unsafe partial struct ufbx_blob
    {
        /// <include file='ufbx_blob.xml' path='doc/member[@name="ufbx_blob.data"]/*' />
        [NativeTypeName("const void *")]
        public void* data;

        /// <include file='ufbx_blob.xml' path='doc/member[@name="ufbx_blob.size"]/*' />
        [NativeTypeName("size_t")]
        public nuint size;
    }
}
