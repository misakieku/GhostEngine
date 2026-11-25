namespace Ghost.D3D12MA;

public unsafe partial struct D3D12MA_DEFRAGMENTATION_PASS_MOVE_INFO
{
    [NativeTypeName("UINT32")]
    public uint MoveCount;

    public D3D12MA_DEFRAGMENTATION_MOVE* pMoves;
}
