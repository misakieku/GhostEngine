namespace Ghost.Ufbx
{
    public partial struct ufbx_prop_override
    {
        [NativeTypeName("uint32_t")]
        public uint element_id;

        [NativeTypeName("uint32_t")]
        public uint _internal_key;

        public ufbx_string prop_name;

        [NativeTypeName("ufbx_vec4")]
        public Misaki.HighPerformance.Mathematics.float4 value;

        public ufbx_string value_str;

        [NativeTypeName("int64_t")]
        public long value_int;
    }
}
