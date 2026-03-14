namespace Ghost.Ufbx
{
    public partial struct ufbx_dom_value
    {
        public ufbx_dom_value_type type;

        public ufbx_string value_str;

        public ufbx_blob value_blob;

        [NativeTypeName("int64_t")]
        public long value_int;

        public double value_float;
    }
}
