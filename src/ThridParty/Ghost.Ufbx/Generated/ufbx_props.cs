namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_props
    {
        public ufbx_prop_list props;

        [NativeTypeName("size_t")]
        public nuint num_animated;

        public ufbx_props* defaults;
    }
}
