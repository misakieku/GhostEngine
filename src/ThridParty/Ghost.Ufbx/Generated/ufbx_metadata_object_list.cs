namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_metadata_object_list
    {
        public ufbx_metadata_object** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
