namespace Ghost.D3D12;

public unsafe partial struct D3D12_CACHED_PIPELINE_STATE
{
    [NativeTypeName("const void *")]
    public void* pCachedBlob;

    [NativeTypeName("SIZE_T")]
    public nuint CachedBlobSizeInBytes;
}
