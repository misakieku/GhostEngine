namespace Ghost.D3D12;

public partial struct D3D12_RANGE
{
    [NativeTypeName("SIZE_T")]
    public nuint Begin;

    [NativeTypeName("SIZE_T")]
    public nuint End;
}
