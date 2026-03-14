namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_material_texture
    {
        public ufbx_string material_prop;

        public ufbx_string shader_prop;

        public ufbx_texture* texture;
    }
}
