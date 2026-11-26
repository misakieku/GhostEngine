namespace Ghost.DXC;

public unsafe partial struct DxcBuffer
{
    [NativeTypeName("LPCVOID")]
    public void* Ptr;

    [NativeTypeName("SIZE_T")]
    public nuint Size;

    public uint Encoding;
}
