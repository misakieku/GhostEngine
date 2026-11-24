namespace Ghost.D3D12;

public unsafe partial struct D3D12_MLIR_PROGRAM_BINDINGS
{
    public uint NumBindings;

    [NativeTypeName("const D3D12_MLIR_PROGRAM_BINDING *")]
    public D3D12_MLIR_PROGRAM_BINDING* pBindings;
}
