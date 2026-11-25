namespace Ghost.D3D12;

public partial struct D3D12_GPU_DESCRIPTOR_HANDLE
{
    public D3D12_GPU_DESCRIPTOR_HANDLE Offset(int offsetInDescriptors, uint descriptorIncrementSize)
    {
        ptr = unchecked((ulong)((long)ptr + offsetInDescriptors * descriptorIncrementSize));
        return this;
    }
}
