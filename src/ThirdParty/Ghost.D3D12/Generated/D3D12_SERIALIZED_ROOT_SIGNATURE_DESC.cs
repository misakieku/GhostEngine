namespace Ghost.D3D12;

public unsafe partial struct D3D12_SERIALIZED_ROOT_SIGNATURE_DESC
{
    [NativeTypeName("const void *")]
    public void* pSerializedBlob;

    [NativeTypeName("SIZE_T")]
    public nuint SerializedBlobSizeInBytes;
}
