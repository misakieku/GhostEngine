namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_string_list
    {
        public ufbx_string* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
