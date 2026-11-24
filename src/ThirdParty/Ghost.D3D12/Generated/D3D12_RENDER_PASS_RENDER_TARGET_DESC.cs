namespace Ghost.D3D12;

public partial struct D3D12_RENDER_PASS_RENDER_TARGET_DESC
{
    public D3D12_CPU_DESCRIPTOR_HANDLE cpuDescriptor;

    public D3D12_RENDER_PASS_BEGINNING_ACCESS BeginningAccess;

    public D3D12_RENDER_PASS_ENDING_ACCESS EndingAccess;
}
