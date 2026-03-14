namespace Ghost.Ufbx
{
    public partial struct ufbx_skin_weight
    {
        [NativeTypeName("uint32_t")]
        public uint cluster_index;

        [NativeTypeName("ufbx_real")]
        public float weight;
    }
}
