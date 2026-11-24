namespace Ghost.D3D12;

public partial struct D3D12_CPU_DESCRIPTOR_HANDLE
{
    public D3D12_CPU_DESCRIPTOR_HANDLE Offset(int offsetInDescriptors, uint descriptorIncrementSize)
    {
        ptr = unchecked((nuint)((long)ptr + (long)offsetInDescriptors * descriptorIncrementSize));
        return this;
    }
}
