namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_warning_list
    {
        public ufbx_warning* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
