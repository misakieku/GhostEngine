namespace Ghost.Ufbx
{
    /// <include file='ufbx_mesh_list.xml' path='doc/member[@name="ufbx_mesh_list"]/*' />
    public unsafe partial struct ufbx_mesh_list
    {
        /// <include file='ufbx_mesh_list.xml' path='doc/member[@name="ufbx_mesh_list.data"]/*' />
        public ufbx_mesh** data;

        /// <include file='ufbx_mesh_list.xml' path='doc/member[@name="ufbx_mesh_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
