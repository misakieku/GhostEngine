namespace Ghost.D3D12;

public unsafe partial struct D3D12_SHADER_BYTECODE
{
    [NativeTypeName("const void *")]
    public void* pShaderBytecode;

    [NativeTypeName("SIZE_T")]
    public nuint BytecodeLength;
}
