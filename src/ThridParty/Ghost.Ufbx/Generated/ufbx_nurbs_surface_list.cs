namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_nurbs_surface_list
    {
        public ufbx_nurbs_surface** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
