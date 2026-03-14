namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_connection_list
    {
        public ufbx_connection* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
