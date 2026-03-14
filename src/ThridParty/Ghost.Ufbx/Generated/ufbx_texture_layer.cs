namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_texture_layer
    {
        public ufbx_texture* texture;

        public ufbx_blend_mode blend_mode;

        [NativeTypeName("ufbx_real")]
        public float alpha;
    }
}
