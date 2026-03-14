namespace Ghost.Ufbx
{
    public partial struct ufbx_subdivision_weight_range
    {
        [NativeTypeName("uint32_t")]
        public uint weight_begin;

        [NativeTypeName("uint32_t")]
        public uint num_weights;
    }
}
