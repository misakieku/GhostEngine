namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_anim_prop
    {
        public ufbx_element* element;

        [NativeTypeName("uint32_t")]
        public uint _internal_key;

        public ufbx_string prop_name;

        public ufbx_anim_value* anim_value;
    }
}
