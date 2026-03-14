namespace Ghost.Ufbx
{
    public partial struct ufbx_skin_vertex
    {
        [NativeTypeName("uint32_t")]
        public uint weight_begin;

        [NativeTypeName("uint32_t")]
        public uint num_weights;

        [NativeTypeName("ufbx_real")]
        public float dq_weight;
    }
}
