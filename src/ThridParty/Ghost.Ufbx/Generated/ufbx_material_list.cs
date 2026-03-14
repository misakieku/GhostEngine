namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_material_list
    {
        public ufbx_material** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
