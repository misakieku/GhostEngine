namespace Ghost.D3D12MA;

public partial struct D3D12MA_Budget
{
    public D3D12MA_Statistics Stats;

    [NativeTypeName("UINT64")]
    public ulong UsageBytes;

    [NativeTypeName("UINT64")]
    public ulong BudgetBytes;
}
