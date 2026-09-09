namespace Ghost.Ufbx
{
    /// <include file='ufbx_material_texture_list.xml' path='doc/member[@name="ufbx_material_texture_list"]/*' />
    public unsafe partial struct ufbx_material_texture_list
    {
        /// <include file='ufbx_material_texture_list.xml' path='doc/member[@name="ufbx_material_texture_list.data"]/*' />
        public ufbx_material_texture* data;

        /// <include file='ufbx_material_texture_list.xml' path='doc/member[@name="ufbx_material_texture_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
