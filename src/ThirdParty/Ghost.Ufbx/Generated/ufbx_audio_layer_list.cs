namespace Ghost.Ufbx
{
    /// <include file='ufbx_audio_layer_list.xml' path='doc/member[@name="ufbx_audio_layer_list"]/*' />
    public unsafe partial struct ufbx_audio_layer_list
    {
        /// <include file='ufbx_audio_layer_list.xml' path='doc/member[@name="ufbx_audio_layer_list.data"]/*' />
        public ufbx_audio_layer** data;

        /// <include file='ufbx_audio_layer_list.xml' path='doc/member[@name="ufbx_audio_layer_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
