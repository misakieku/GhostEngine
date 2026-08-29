namespace Ghost.Ufbx
{
    /// <include file='ufbx_metadata_object_list.xml' path='doc/member[@name="ufbx_metadata_object_list"]/*' />
    public unsafe partial struct ufbx_metadata_object_list
    {
        /// <include file='ufbx_metadata_object_list.xml' path='doc/member[@name="ufbx_metadata_object_list.data"]/*' />
        public ufbx_metadata_object** data;

        /// <include file='ufbx_metadata_object_list.xml' path='doc/member[@name="ufbx_metadata_object_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
