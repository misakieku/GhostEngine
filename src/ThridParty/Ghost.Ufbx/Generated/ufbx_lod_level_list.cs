namespace Ghost.Ufbx
{
    /// <include file='ufbx_lod_level_list.xml' path='doc/member[@name="ufbx_lod_level_list"]/*' />
    public unsafe partial struct ufbx_lod_level_list
    {
        /// <include file='ufbx_lod_level_list.xml' path='doc/member[@name="ufbx_lod_level_list.data"]/*' />
        public ufbx_lod_level* data;

        /// <include file='ufbx_lod_level_list.xml' path='doc/member[@name="ufbx_lod_level_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
