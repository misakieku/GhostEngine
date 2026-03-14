namespace Ghost.Ufbx
{
    public partial struct ufbx_uv_set
    {
        public ufbx_string name;

        [NativeTypeName("uint32_t")]
        public uint index;

        public ufbx_vertex_vec2 vertex_uv;

        public ufbx_vertex_vec3 vertex_tangent;

        public ufbx_vertex_vec3 vertex_bitangent;
    }
}
