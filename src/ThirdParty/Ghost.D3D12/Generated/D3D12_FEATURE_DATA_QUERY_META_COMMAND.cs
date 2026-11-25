using System;

namespace Ghost.D3D12;

public unsafe partial struct D3D12_FEATURE_DATA_QUERY_META_COMMAND
{
    public Guid CommandId;

    public uint NodeMask;

    [NativeTypeName("const void *")]
    public void* pQueryInputData;

    [NativeTypeName("SIZE_T")]
    public nuint QueryInputDataSizeInBytes;

    public void* pQueryOutputData;

    [NativeTypeName("SIZE_T")]
    public nuint QueryOutputDataSizeInBytes;
}
