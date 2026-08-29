namespace Ghost.Ufbx
{
    /// <include file='ufbx_baked_vec3_list.xml' path='doc/member[@name="ufbx_baked_vec3_list"]/*' />
    public unsafe partial struct ufbx_baked_vec3_list
    {
        /// <include file='ufbx_baked_vec3_list.xml' path='doc/member[@name="ufbx_baked_vec3_list.data"]/*' />
        public ufbx_baked_vec3* data;

        /// <include file='ufbx_baked_vec3_list.xml' path='doc/member[@name="ufbx_baked_vec3_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
