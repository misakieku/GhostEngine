namespace Ghost.Ufbx
{
    /// <include file='ufbx_material_texture.xml' path='doc/member[@name="ufbx_material_texture"]/*' />
    public unsafe partial struct ufbx_material_texture
    {
        /// <include file='ufbx_material_texture.xml' path='doc/member[@name="ufbx_material_texture.material_prop"]/*' />
        public ufbx_string material_prop;

        /// <include file='ufbx_material_texture.xml' path='doc/member[@name="ufbx_material_texture.shader_prop"]/*' />
        public ufbx_string shader_prop;

        /// <include file='ufbx_material_texture.xml' path='doc/member[@name="ufbx_material_texture.texture"]/*' />
        public ufbx_texture* texture;
    }
}
