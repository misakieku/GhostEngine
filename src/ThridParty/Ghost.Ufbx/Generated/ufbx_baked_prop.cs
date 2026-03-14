namespace Ghost.Ufbx
{
    public partial struct ufbx_baked_prop
    {
        public ufbx_string name;

        [NativeTypeName("_Bool")]
        public bool constant_value;

        public ufbx_baked_vec3_list keys;
    }
}
