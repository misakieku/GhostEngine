namespace Ghost.Graphics;

public interface IShaderCompilationBridge
{
    bool TryGetBytecode(ulong manifestKey, out ReadOnlyMemory<byte> bytecode);
    bool IsCompiling(ulong manifestKey);
}

// NOTE: For testing only.
internal sealed class NullShaderCompilationBridge : IShaderCompilationBridge
{
    public bool TryGetBytecode(ulong manifestKey, out ReadOnlyMemory<byte> bytecode)
    {
        bytecode = default;
        return false; // Always fall through to ShaderLibrary's disk cache
    }

    public bool IsCompiling(ulong manifestKey) => false;
}
