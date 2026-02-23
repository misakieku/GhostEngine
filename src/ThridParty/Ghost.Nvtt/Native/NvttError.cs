namespace Ghost.Nvtt.Native
{
    public enum NvttError
    {
        NVTT_Error_None,
        NVTT_Error_Unknown = NVTT_Error_None,
        NVTT_Error_InvalidInput,
        NVTT_Error_UnsupportedFeature,
        NVTT_Error_CudaError,
        NVTT_Error_FileOpen,
        NVTT_Error_FileWrite,
        NVTT_Error_UnsupportedOutputFormat,
        NVTT_Error_Messaging,
        NVTT_Error_OutOfHostMemory,
        NVTT_Error_OutOfDeviceMemory,
        NVTT_Error_OutputWrite,
        NVTT_Error_Count,
    }
}
