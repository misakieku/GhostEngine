using Ghost.Core.Utilities;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Core.Graphics;

public enum ShaderModel
{
    SM_6_6,
    SM_6_7,
    SM_6_8
}

public enum KeywordSpace
{
    Local,
    Global,
}

public struct ShaderCode
{
    public string code;
    public string entryPoint;

    public readonly bool IsCreated => !string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(entryPoint);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ulong GetHashCode64()
    {
        return Hash.Combine64(XxHash64.HashToUInt64(MemoryMarshal.AsBytes(code.AsSpan())), XxHash64.HashToUInt64(MemoryMarshal.AsBytes(entryPoint.AsSpan())));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(code, entryPoint);
    }
}

public struct KeywordsGroup
{
    public KeywordSpace space;
    public List<string> keywords;
}

public struct PassDescriptor
{
    public GraphicsShaderDescriptor shader;

    public string name;

    public ShaderCode amplificationShaderCode;
    public ShaderCode meshShaderCode;
    public ShaderCode pixelShaderCode;
    public string[] defines;
    public KeywordsGroup[] keywords;
    public PipelineState localPipeline;
}

public class GraphicsShaderDescriptor
{
    public required string name = string.Empty;
    public required uint propertyBufferSize;
    public required ShaderModel shaderModel;
    public required PassDescriptor[] passes = Array.Empty<PassDescriptor>();
}

public class ComputeShaderDescriptor
{
    public required string name = string.Empty;
    public required uint propertyBufferSize;
    public required ShaderModel shaderModel;
    public required ShaderCode[] shaderCodes;
    public required string[] defines;
    public required KeywordsGroup[] keywords;
}
