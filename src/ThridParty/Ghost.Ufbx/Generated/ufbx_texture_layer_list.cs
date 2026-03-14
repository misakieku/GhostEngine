namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_texture_layer_list
    {
        public ufbx_texture_layer* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
