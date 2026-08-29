namespace Ghost.Ufbx
{
    /// <include file='ufbx_material_feature_info.xml' path='doc/member[@name="ufbx_material_feature_info"]/*' />
    public partial struct ufbx_material_feature_info
    {
        /// <include file='ufbx_material_feature_info.xml' path='doc/member[@name="ufbx_material_feature_info.enabled"]/*' />
        [NativeTypeName("_Bool")]
        public bool enabled;

        /// <include file='ufbx_material_feature_info.xml' path='doc/member[@name="ufbx_material_feature_info.is_explicit"]/*' />
        [NativeTypeName("_Bool")]
        public bool is_explicit;
    }
}
