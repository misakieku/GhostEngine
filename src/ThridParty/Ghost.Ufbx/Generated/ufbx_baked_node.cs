namespace Ghost.Ufbx
{
    /// <include file='ufbx_baked_node.xml' path='doc/member[@name="ufbx_baked_node"]/*' />
    public partial struct ufbx_baked_node
    {
        /// <include file='ufbx_baked_node.xml' path='doc/member[@name="ufbx_baked_node.typed_id"]/*' />
        [NativeTypeName("uint32_t")]
        public uint typed_id;

        /// <include file='ufbx_baked_node.xml' path='doc/member[@name="ufbx_baked_node.element_id"]/*' />
        [NativeTypeName("uint32_t")]
        public uint element_id;

        /// <include file='ufbx_baked_node.xml' path='doc/member[@name="ufbx_baked_node.constant_translation"]/*' />
        [NativeTypeName("_Bool")]
        public bool constant_translation;

        /// <include file='ufbx_baked_node.xml' path='doc/member[@name="ufbx_baked_node.constant_rotation"]/*' />
        [NativeTypeName("_Bool")]
        public bool constant_rotation;

        /// <include file='ufbx_baked_node.xml' path='doc/member[@name="ufbx_baked_node.constant_scale"]/*' />
        [NativeTypeName("_Bool")]
        public bool constant_scale;

        /// <include file='ufbx_baked_node.xml' path='doc/member[@name="ufbx_baked_node.translation_keys"]/*' />
        public ufbx_baked_vec3_list translation_keys;

        /// <include file='ufbx_baked_node.xml' path='doc/member[@name="ufbx_baked_node.rotation_keys"]/*' />
        public ufbx_baked_quat_list rotation_keys;

        /// <include file='ufbx_baked_node.xml' path='doc/member[@name="ufbx_baked_node.scale_keys"]/*' />
        public ufbx_baked_vec3_list scale_keys;
    }
}
