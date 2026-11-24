namespace Ghost.D3D12;

public unsafe partial struct D3D12_DXIL_LIBRARY_DESC
{
    public D3D12_SHADER_BYTECODE DXILLibrary;

    public uint NumExports;

    [NativeTypeName("const D3D12_EXPORT_DESC *")]
    public D3D12_EXPORT_DESC* pExports;
}
