namespace Ghost.Ufbx
{
    public partial struct ufbx_mesh_part
    {
        [NativeTypeName("uint32_t")]
        public uint index;

        [NativeTypeName("size_t")]
        public nuint num_faces;

        [NativeTypeName("size_t")]
        public nuint num_triangles;

        [NativeTypeName("size_t")]
        public nuint num_empty_faces;

        [NativeTypeName("size_t")]
        public nuint num_point_faces;

        [NativeTypeName("size_t")]
        public nuint num_line_faces;

        public ufbx_uint32_list face_indices;
    }
}
