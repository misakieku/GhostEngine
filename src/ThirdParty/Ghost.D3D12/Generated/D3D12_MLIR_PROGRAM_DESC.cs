namespace Ghost.D3D12;

public unsafe partial struct D3D12_MLIR_PROGRAM_DESC
{
    [NativeTypeName("LPCWSTR")]
    public char* ProgramName;

    [NativeTypeName("const void *")]
    public void* pBytecode;

    [NativeTypeName("SIZE_T")]
    public nuint BytecodeSizeInBytes;
}
