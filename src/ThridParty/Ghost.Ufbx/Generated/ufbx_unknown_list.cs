namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_unknown_list
    {
        public ufbx_unknown** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
