namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_marker_list
    {
        public ufbx_marker** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
