namespace Ghost.Ufbx
{
    public partial struct ufbx_color_set
    {
        public ufbx_string name;

        [NativeTypeName("uint32_t")]
        public uint index;

        public ufbx_vertex_vec4 vertex_color;
    }
}
