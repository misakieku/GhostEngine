namespace Ghost.D3D12;

public unsafe partial struct D3D12_SHADER_BYTECODE
{
    public D3D12_SHADER_BYTECODE(void* pShaderBytecode, nuint bytecodeLength)
    {
        this.pShaderBytecode = pShaderBytecode;
        BytecodeLength = bytecodeLength;
    }
}
