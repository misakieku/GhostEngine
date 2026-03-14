namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_mesh_list
    {
        public ufbx_mesh** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
