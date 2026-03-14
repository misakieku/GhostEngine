namespace Ghost.Ufbx
{
    public partial struct ufbx_baked_element
    {
        [NativeTypeName("uint32_t")]
        public uint element_id;

        public ufbx_baked_prop_list props;
    }
}
