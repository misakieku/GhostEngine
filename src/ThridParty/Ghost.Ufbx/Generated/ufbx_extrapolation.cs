namespace Ghost.Ufbx
{
    public partial struct ufbx_extrapolation
    {
        public ufbx_extrapolation_mode mode;

        [NativeTypeName("int32_t")]
        public int repeat_count;
    }
}
