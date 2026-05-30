using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Editor.Core.Contracts;

public unsafe struct ComputeCompileResult
{
    public fixed ulong resultHash[8];
    public readonly int count;

    public ulong HashCode
    {
        get
        {
            var a = Hash.Combine64(resultHash[0], resultHash[1], resultHash[2], resultHash[3]);
            var b = Hash.Combine64(resultHash[4], resultHash[5], resultHash[6], resultHash[7]);
            return Hash.Combine64(a, b);
        }
    }
}

public ref struct ShaderCompilationConfig
{
    public ReadOnlySpan<string> defines;
    public string shaderCode;
    public string entryPoint;
    public ShaderStage stage;
    public ShaderModel model;
    public CompilerOptimizeLevel optimizeLevel;
    public CompilerOption options;
}

public enum CompilerOptimizeLevel
{
    O0,
    O1,
    O2,
    O3
}

[Flags]
public enum CompilerOption
{
    None = 0,
    KeepDebugInfo = 1 << 0,
    KeepReflections = 1 << 1,
    WarnAsError = 1 << 2,
    SpirvCrossCompile = 1 << 3
}

public enum ShaderStage
{
    TaskShader,
    MeshShader,
    PixelShader,
    ComputeShader,
    Library // For ray tracing shaders or work graph shaders that don't fit into the traditional shader stages
}

internal interface IShaderCompiler : IDisposable
{
    Result<UnsafeArray<byte>> Compile(ref readonly ShaderCompilationConfig config, AllocationHandle handle);
}
