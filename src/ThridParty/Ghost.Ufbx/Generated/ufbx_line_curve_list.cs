namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_line_curve_list
    {
        public ufbx_line_curve** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
