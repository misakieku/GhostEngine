namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_anim_layer_list
    {
        public ufbx_anim_layer** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
