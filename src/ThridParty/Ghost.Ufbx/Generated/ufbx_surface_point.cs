namespace Ghost.Ufbx
{
    public partial struct ufbx_surface_point
    {
        [NativeTypeName("_Bool")]
        public bool valid;

        public ufbx_vec3 position;

        public ufbx_vec3 derivative_u;

        public ufbx_vec3 derivative_v;
    }
}
