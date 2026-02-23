namespace Ghost.Nvtt.Native
{
    public enum NvttEncodeFlags
    {
        NVTT_EncodeFlags_None = 0,
        NVTT_EncodeFlags_UseGPU = 1 << 0,
        NVTT_EncodeFlags_OutputToGPUMem = 1 << 1,
        NVTT_EncodeFlags_Opaque = 1 << 2,
    }
}
