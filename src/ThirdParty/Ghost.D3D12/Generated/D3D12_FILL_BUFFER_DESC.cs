namespace Ghost.D3D12;

public unsafe partial struct D3D12_FILL_BUFFER_DESC
{
    public ID3D12Resource* pBuffer;

    [NativeTypeName("UINT64")]
    public ulong Offset;

    public D3D12_CLEAR_DATA FillValue;

    public DXGI_FORMAT Format;

    public uint RawPatternSizeInBytes;

    public uint RepeatCount;
}
