namespace Ghost.D3D12;

public unsafe partial struct D3D12_MLIR_PROGRAM_CPU_BINDING
{
    [NativeTypeName("const void *")]
    public void* pData;

    [NativeTypeName("UINT64")]
    public ulong DataSizeInBytes;
}
