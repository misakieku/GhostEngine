namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_nurbs_curve_list
    {
        public ufbx_nurbs_curve** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
