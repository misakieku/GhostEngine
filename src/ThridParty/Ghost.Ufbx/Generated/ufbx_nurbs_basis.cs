namespace Ghost.Ufbx
{
    public partial struct ufbx_nurbs_basis
    {
        [NativeTypeName("uint32_t")]
        public uint order;

        public ufbx_nurbs_topology topology;

        public ufbx_real_list knot_vector;

        [NativeTypeName("ufbx_real")]
        public float t_min;

        [NativeTypeName("ufbx_real")]
        public float t_max;

        public ufbx_real_list spans;

        [NativeTypeName("_Bool")]
        public bool is_2d;

        [NativeTypeName("size_t")]
        public nuint num_wrap_control_points;

        [NativeTypeName("_Bool")]
        public bool valid;
    }
}
