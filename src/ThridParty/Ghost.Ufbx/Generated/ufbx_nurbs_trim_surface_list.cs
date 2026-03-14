namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_nurbs_trim_surface_list
    {
        public ufbx_nurbs_trim_surface** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
