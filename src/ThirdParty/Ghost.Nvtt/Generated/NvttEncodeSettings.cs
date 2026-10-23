namespace Ghost.Nvtt
{
    public unsafe partial struct NvttEncodeSettings
    {
        [NativeTypeName("uint32_t")]
        public uint sType;

        public NvttFormat format;

        public NvttQuality quality;

        public NvttPixelType rgb_pixel_type;

        public NvttTimingContext* timing_context;

        [NativeTypeName("uint32_t")]
        public uint encode_flags;
    }
}
