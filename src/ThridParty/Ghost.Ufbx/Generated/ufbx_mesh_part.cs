namespace Ghost.Ufbx
{
    /// <include file='ufbx_mesh_part.xml' path='doc/member[@name="ufbx_mesh_part"]/*' />
    public partial struct ufbx_mesh_part
    {
        /// <include file='ufbx_mesh_part.xml' path='doc/member[@name="ufbx_mesh_part.index"]/*' />
        [NativeTypeName("uint32_t")]
        public uint index;

        /// <include file='ufbx_mesh_part.xml' path='doc/member[@name="ufbx_mesh_part.num_faces"]/*' />
        [NativeTypeName("size_t")]
        public nuint num_faces;

        /// <include file='ufbx_mesh_part.xml' path='doc/member[@name="ufbx_mesh_part.num_triangles"]/*' />
        [NativeTypeName("size_t")]
        public nuint num_triangles;

        /// <include file='ufbx_mesh_part.xml' path='doc/member[@name="ufbx_mesh_part.num_empty_faces"]/*' />
        [NativeTypeName("size_t")]
        public nuint num_empty_faces;

        /// <include file='ufbx_mesh_part.xml' path='doc/member[@name="ufbx_mesh_part.num_point_faces"]/*' />
        [NativeTypeName("size_t")]
        public nuint num_point_faces;

        /// <include file='ufbx_mesh_part.xml' path='doc/member[@name="ufbx_mesh_part.num_line_faces"]/*' />
        [NativeTypeName("size_t")]
        public nuint num_line_faces;

        /// <include file='ufbx_mesh_part.xml' path='doc/member[@name="ufbx_mesh_part.face_indices"]/*' />
        public ufbx_uint32_list face_indices;
    }
}
