namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_empty_list
    {
        public ufbx_empty** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
