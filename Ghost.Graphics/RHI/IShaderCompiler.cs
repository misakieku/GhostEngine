using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Graphics.RHI;

public struct CompileResult : IDisposable
{
    public UnsafeArray<byte> bytecode;

    public readonly bool IsCreated => bytecode.IsCreated;

    public void Dispose()
    {
        bytecode.Dispose();
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
    WarnAsError = 1 << 2
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

public unsafe interface IShaderCompiler
{
    Result<CompileResult> Compile(ref readonly CompilerConfig config, Allocator allocator, void** ppReflection);
    Result<ShaderReflectionData> PerformDXCReflection<T>(T* pReflectionBlob) where T : unmanaged;
}
