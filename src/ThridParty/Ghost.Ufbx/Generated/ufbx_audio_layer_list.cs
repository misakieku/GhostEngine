namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_audio_layer_list
    {
        public ufbx_audio_layer** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
