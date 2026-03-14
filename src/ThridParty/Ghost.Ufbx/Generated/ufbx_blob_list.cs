namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_blob_list
    {
        public ufbx_blob* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
