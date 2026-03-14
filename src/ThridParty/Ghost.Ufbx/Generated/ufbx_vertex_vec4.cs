namespace Ghost.Ufbx
{
    public partial struct ufbx_vertex_vec4
    {
        [NativeTypeName("_Bool")]
        public bool exists;

        public ufbx_vec4_list values;

        public ufbx_uint32_list indices;

        [NativeTypeName("size_t")]
        public nuint value_reals;

        [NativeTypeName("_Bool")]
        public bool unique_per_vertex;

        public ufbx_real_list values_w;
    }
}
