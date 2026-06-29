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
        if (!IsCreated)
        {
            return 0;
        }

        return Hash.Combine64(XxHash64.HashToUInt64(MemoryMarshal.AsBytes(code.AsSpan())), XxHash64.HashToUInt64(MemoryMarshal.AsBytes(entryPoint.AsSpan())));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly int GetHashCode()
    {
        if (!IsCreated)
        {
            return 0;
        }

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
    public required string Name
    {
        get; init;
    }

    public required uint PropertyBufferSize
    {
        get; init;
    }

    public required ShaderModel ShaderModel
    {
        get; init;
    }

    public required PassDescriptor[] Passes
    {
        get; init;
    }
}

public class ComputeShaderDescriptor
{
    public required string Name
    {
        get; init;
    }

    public required uint PropertyBufferSize
    {
        get; init;
    }

    public required ShaderModel ShaderModel
    {
        get; init;
    }

    public required ShaderCode[] ShaderCodes
    {
        get; init;
    }

    public required string[] Defines
    {
        get; init;
    }

    public required KeywordsGroup[] Keywords
    {
        get; init;
    }
}
