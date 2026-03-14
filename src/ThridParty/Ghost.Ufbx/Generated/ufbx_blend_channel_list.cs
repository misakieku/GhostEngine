namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_blend_channel_list
    {
        public ufbx_blend_channel** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
