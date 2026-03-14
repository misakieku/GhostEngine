namespace Ghost.Ufbx
{
    public partial struct ufbx_subdivision_weight
    {
        [NativeTypeName("ufbx_real")]
        public float weight;

        [NativeTypeName("uint32_t")]
        public uint index;
    }
}
