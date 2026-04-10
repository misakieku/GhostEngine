using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Graphics.RHI;

public struct ShaderCompileResult : IDisposable
{
    public UnsafeArray<byte> bytecode;
    public ulong hashCode;

    public readonly bool IsCreated => bytecode.IsCreated;

    public void Dispose()
    {
        bytecode.Dispose();
    }
}

public struct GraphicsCompiledResult
{
    public ulong tsResultHash;
    public ulong msResultHash;
    public ulong psResultHash;

    public readonly ulong HashCode => Hash.Combine64(tsResultHash, msResultHash, psResultHash);
}

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
    Result<Key64<ShaderCompileResult>> Compile(ref readonly ShaderCompilationConfig config);
    Result<GraphicsCompiledResult> CompilePass(ref readonly PassDescriptor descriptor, ref readonly ShaderCompilationConfig additionalConfig, ref readonly LocalKeywordSet keywords);
    Result<ShaderCompileResult, Error> GetCompiledCache(Key64<ShaderCompileResult> key);
}
