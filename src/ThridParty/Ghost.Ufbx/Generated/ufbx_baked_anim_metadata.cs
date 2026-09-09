namespace Ghost.Ufbx
{
    /// <include file='ufbx_baked_anim_metadata.xml' path='doc/member[@name="ufbx_baked_anim_metadata"]/*' />
    public partial struct ufbx_baked_anim_metadata
    {
        /// <include file='ufbx_baked_anim_metadata.xml' path='doc/member[@name="ufbx_baked_anim_metadata.result_memory_used"]/*' />
        [NativeTypeName("size_t")]
        public nuint result_memory_used;

        /// <include file='ufbx_baked_anim_metadata.xml' path='doc/member[@name="ufbx_baked_anim_metadata.temp_memory_used"]/*' />
        [NativeTypeName("size_t")]
        public nuint temp_memory_used;

        /// <include file='ufbx_baked_anim_metadata.xml' path='doc/member[@name="ufbx_baked_anim_metadata.result_allocs"]/*' />
        [NativeTypeName("size_t")]
        public nuint result_allocs;

        /// <include file='ufbx_baked_anim_metadata.xml' path='doc/member[@name="ufbx_baked_anim_metadata.temp_allocs"]/*' />
        [NativeTypeName("size_t")]
        public nuint temp_allocs;
    }
}
