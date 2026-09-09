namespace Ghost.Ufbx
{
    /// <include file='ufbx_mesh_part_list.xml' path='doc/member[@name="ufbx_mesh_part_list"]/*' />
    public unsafe partial struct ufbx_mesh_part_list
    {
        /// <include file='ufbx_mesh_part_list.xml' path='doc/member[@name="ufbx_mesh_part_list.data"]/*' />
        public ufbx_mesh_part* data;

        /// <include file='ufbx_mesh_part_list.xml' path='doc/member[@name="ufbx_mesh_part_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
