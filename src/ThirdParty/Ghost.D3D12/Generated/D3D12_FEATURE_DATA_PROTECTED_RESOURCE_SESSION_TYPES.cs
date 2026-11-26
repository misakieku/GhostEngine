namespace Ghost.D3D12;

public unsafe partial struct D3D12_FEATURE_DATA_PROTECTED_RESOURCE_SESSION_TYPES
{
    public uint NodeIndex;

    public uint Count;

    public Guid* pTypes;
}
