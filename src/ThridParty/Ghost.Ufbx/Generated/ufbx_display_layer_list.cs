namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_display_layer_list
    {
        public ufbx_display_layer** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
