namespace Ghost.D3D12MA;

public unsafe partial struct D3D12MA_DEFRAGMENTATION_MOVE
{
    public D3D12MA_DEFRAGMENTATION_MOVE_OPERATION Operation;

    public D3D12MA_Allocation* pSrcAllocation;

    public D3D12MA_Allocation* pDstTmpAllocation;
}
