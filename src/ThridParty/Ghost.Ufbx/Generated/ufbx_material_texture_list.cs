namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_material_texture_list
    {
        public ufbx_material_texture* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
