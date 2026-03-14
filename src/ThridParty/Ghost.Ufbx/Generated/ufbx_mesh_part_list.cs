namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_mesh_part_list
    {
        public ufbx_mesh_part* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
