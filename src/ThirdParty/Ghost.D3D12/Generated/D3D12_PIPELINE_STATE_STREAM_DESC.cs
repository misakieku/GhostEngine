namespace Ghost.D3D12;

public unsafe partial struct D3D12_PIPELINE_STATE_STREAM_DESC
{
    [NativeTypeName("SIZE_T")]
    public nuint SizeInBytes;

    public void* pPipelineStateSubobjectStream;
}
