using Ghost.Core.Utilities;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Core.Graphics;

public static class ShaderIdentity
{
    public const ulong ShaderIdMask = 0xFFFFFFFFFFFFFFF0ul;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong GetShaderId(string shaderName)
    {
        return XxHash64.HashToUInt64(MemoryMarshal.AsBytes(shaderName.AsSpan())) & ShaderIdMask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong GetPassId(ulong shaderId, int passIndex)
    {
        Logger.DebugAssert(passIndex >= 0 && passIndex < 16, "Pass index must be between 0 and 15 to fit within the shader ID mask.");
        return shaderId | ((ulong)passIndex & 0xFul);
    }
}

public enum ShaderModel
{
    SM_6_6,
    SM_6_7,
    SM_6_8
}

public enum PassSemantic : byte
{
    Forward = 0,
    Visibility = 1,
    Shadow = 2,
    DeferredTexturing = 3,
    Custom = 4,
    Count = 8
}

public static class PassSemanticExtensions
{
    public static PassSemantic FromName(string passName)
    {
        return passName switch
        {
            "Forward" => PassSemantic.Forward,
            "Visibility" => PassSemantic.Visibility,
            "Shadow" => PassSemantic.Shadow,
            "DeferredTexturing" => PassSemantic.DeferredTexturing,
            _ => PassSemantic.Custom,
        };
    }
}

public struct ShaderCode()
{
    public string code = string.Empty;
    public string entryPoint = string.Empty;

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

public struct PassDescriptor
{
    public GraphicsShaderDescriptor shader;

    public string name;
    public PassSemantic semantic;
    public ShaderStageMask stageMask;

    public ShaderCode amplificationShaderCode;
    public ShaderCode meshShaderCode;
    public ShaderCode pixelShaderCode;
    public ShaderCode computeShaderCode;
    public string[] defines;
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
}
