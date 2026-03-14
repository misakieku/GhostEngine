namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_bool_list
    {
        [NativeTypeName("_Bool *")]
        public bool* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
