namespace Ghost.Ufbx
{
    /// <include file='ufbx_material_list.xml' path='doc/member[@name="ufbx_material_list"]/*' />
    public unsafe partial struct ufbx_material_list
    {
        /// <include file='ufbx_material_list.xml' path='doc/member[@name="ufbx_material_list.data"]/*' />
        public ufbx_material** data;

        /// <include file='ufbx_material_list.xml' path='doc/member[@name="ufbx_material_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
