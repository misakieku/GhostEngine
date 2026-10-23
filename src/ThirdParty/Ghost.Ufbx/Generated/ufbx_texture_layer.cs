namespace Ghost.Ufbx
{
    /// <include file='ufbx_texture_layer.xml' path='doc/member[@name="ufbx_texture_layer"]/*' />
    public unsafe partial struct ufbx_texture_layer
    {
        /// <include file='ufbx_texture_layer.xml' path='doc/member[@name="ufbx_texture_layer.texture"]/*' />
        public ufbx_texture* texture;

        /// <include file='ufbx_texture_layer.xml' path='doc/member[@name="ufbx_texture_layer.blend_mode"]/*' />
        public ufbx_blend_mode blend_mode;

        /// <include file='ufbx_texture_layer.xml' path='doc/member[@name="ufbx_texture_layer.alpha"]/*' />
        [NativeTypeName("ufbx_real")]
        public float alpha;
    }
}
