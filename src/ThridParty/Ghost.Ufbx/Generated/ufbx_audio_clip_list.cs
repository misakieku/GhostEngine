namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_audio_clip_list
    {
        public ufbx_audio_clip** data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
