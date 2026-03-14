namespace Ghost.Ufbx
{
    public partial struct ufbx_transform_override
    {
        [NativeTypeName("uint32_t")]
        public uint node_id;

        public ufbx_transform transform;
    }
}
