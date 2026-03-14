namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_nurbs_trim_boundary_list
    {
        public ufbx_nurbs_trim_boundary** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
