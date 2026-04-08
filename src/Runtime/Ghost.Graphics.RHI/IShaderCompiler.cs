using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Graphics.RHI;

public struct ShaderCompileResult : IDisposable
{
    public UnsafeArray<byte> bytecode;
    public ShaderReflectionData reflectionData;
    public ulong hashCode;

    public readonly bool IsCreated => bytecode.IsCreated;

    public void Dispose()
    {
        bytecode.Dispose();
    }
}

public struct GraphicsCompiledResult : IDisposable
{
    private ulong _hashCode;

    public ShaderCompileResult tsResult;
    public ShaderCompileResult msResult;
    public ShaderCompileResult psResult;

    public Key64<GraphicsCompiledResult> HashCode
    {
        get
        {
            if (_hashCode == 0)
            {
                _hashCode = Hash.Combine64(tsResult.hashCode, msResult.hashCode, psResult.hashCode);
            }

            return _hashCode;
        }
    }

    public void Dispose()
    {
        tsResult.Dispose();
        msResult.Dispose();
        psResult.Dispose();
    }
}

public ref struct ShaderCompilationConfig
{
    public ReadOnlySpan<string> defines;
    public ReadOnlySpan<string> includes;
    public string shaderPath;
    public string entryPoint;
    public string? injectedCode;
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
    Result<ShaderCompileResult> Compile(ref readonly ShaderCompilationConfig config, Allocator allocator);
    Result<GraphicsCompiledResult> CompilePass(ref readonly PassDescriptor descriptor, ref readonly ShaderCompilationConfig additionalConfig, ref readonly LocalKeywordSet keywords);
    Result<GraphicsCompiledResult, Error> LoadCompiledCache(Key64<ShaderVariant> key);
}
