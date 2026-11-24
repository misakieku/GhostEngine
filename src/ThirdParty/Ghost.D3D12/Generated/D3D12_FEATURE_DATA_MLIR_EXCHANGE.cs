namespace Ghost.D3D12;

public unsafe partial struct D3D12_FEATURE_DATA_MLIR_EXCHANGE
{
    public D3D12_MLIR_EXCHANGE_TYPE Type;

    public D3D12_VERSION_NUMBER IRVersion;

    [NativeTypeName("const void *")]
    public void* pInputData;

    [NativeTypeName("SIZE_T")]
    public nuint InputDataSizeInBytes;

    public void* pOutputData;

    [NativeTypeName("SIZE_T *")]
    public nuint* OutputDataSizeInBytes;
}
