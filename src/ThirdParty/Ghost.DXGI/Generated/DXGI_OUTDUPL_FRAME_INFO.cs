namespace Ghost.DXGI;

public partial struct DXGI_OUTDUPL_FRAME_INFO
{
    public LARGE_INTEGER LastPresentTime;

    public LARGE_INTEGER LastMouseUpdateTime;

    public uint AccumulatedFrames;

    [NativeTypeName("BOOL")]
    public int RectsCoalesced;

    [NativeTypeName("BOOL")]
    public int ProtectedContentMaskedOut;

    public DXGI_OUTDUPL_POINTER_POSITION PointerPosition;

    public uint TotalMetadataBufferSize;

    public uint PointerShapeBufferSize;
}
