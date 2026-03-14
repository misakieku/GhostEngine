namespace Ghost.Ufbx
{
    public partial struct ufbx_baked_node
    {
        [NativeTypeName("uint32_t")]
        public uint typed_id;

        [NativeTypeName("uint32_t")]
        public uint element_id;

        [NativeTypeName("_Bool")]
        public bool constant_translation;

        [NativeTypeName("_Bool")]
        public bool constant_rotation;

        [NativeTypeName("_Bool")]
        public bool constant_scale;

        public ufbx_baked_vec3_list translation_keys;

        public ufbx_baked_quat_list rotation_keys;

        public ufbx_baked_vec3_list scale_keys;
    }
}
