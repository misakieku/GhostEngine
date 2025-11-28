using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Graphics.Contracts;

public struct ShaderCompileResult : IDisposable
{
    public UnsafeArray<byte> bytecode;
    public ShaderReflectionData reflectionData;

    public readonly bool IsCreated => bytecode.IsCreated;

    public void Dispose()
    {
        bytecode.Dispose();
    }
}

public struct GraphicsCompiledResult : IDisposable
{
    public ShaderCompileResult tsResult;
    public ShaderCompileResult msResult;
    public ShaderCompileResult psResult;

    public void Dispose()
    {
        tsResult.Dispose();
        msResult.Dispose();
        psResult.Dispose();
    }
}

public ref struct CompilerConfig
{
    public ReadOnlySpan<string> defines;
    public string? include;
    public string shaderPath;
    public string entryPoint;
    public ShaderStage stage;
    public CompilerTier tier;
    public CompilerOptimizeLevel optimizeLevel;
    public CompilerOption options;
}

public enum CompilerTier
{
    Tier0,
    Tier1,
    Tier2
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
    ComputeShader
}

public enum ShaderInputType
{
    ConstantBuffer,
    Texture,
    Sampler,
    UAV,
    StructuredBuffer,
    ByteAddressBuffer,
    RWStructuredBuffer,
    RWByteAddressBuffer
}

public struct ResourceBindingInfo
{
    public string Name
    {
        get; set;
    }

    public ShaderInputType Type
    {
        get; set;
    }

    public uint BindPoint
    {
        get; set;
    }

    public uint BindCount
    {
        get; set;
    }

    public uint Space
    {
        get; set;
    }

    public uint Size
    {
        get; set;
    }

    public IReadOnlyList<CBufferPropertyInfo>? Properties
    {
        get; set;
    }
}

public readonly struct ShaderReflectionData
{
    public List<ResourceBindingInfo> ResourcesBindings
    {
        get;
    }

    public ShaderReflectionData()
    {
        ResourcesBindings = new List<ResourceBindingInfo>();
    }
}

public interface IShaderCompiler : IDisposable
{
    Result<ShaderCompileResult> Compile(ref readonly CompilerConfig config, Allocator allocator);
    Result<GraphicsCompiledResult> CompilePass(IPassDescriptor descriptor, string? generatedCodePath);
    Result<GraphicsCompiledResult> LoadCompiledCache(ShaderPassKey key);
}
