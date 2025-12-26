using System.Runtime.InteropServices;

namespace Ghost.Shader.Concept;

/// <summary>
/// Unique identifier for a shader variant based on keyword combination.
/// Used as key for shader compilation cache.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct ShaderVariantKey : IEquatable<ShaderVariantKey>
{
    public readonly int ShaderProgramId;
    public readonly ulong KeywordHash;

    public ShaderVariantKey(int shaderProgramId, ulong keywordHash)
    {
        ShaderProgramId = shaderProgramId;
        KeywordHash = keywordHash;
    }

    public bool Equals(ShaderVariantKey other) =>
        ShaderProgramId == other.ShaderProgramId && KeywordHash == other.KeywordHash;

    public override bool Equals(object? obj) => obj is ShaderVariantKey other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(ShaderProgramId, KeywordHash);
}

/// <summary>
/// Unique identifier for a graphics pipeline state object.
/// Combines shader variant, render state, and pass information.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct GraphicsPipelineKey : IEquatable<GraphicsPipelineKey>
{
    public readonly ShaderVariantKey VariantKey;
    public readonly ulong RenderStateHash;
    public readonly int PassId;

    public GraphicsPipelineKey(ShaderVariantKey variantKey, ulong renderStateHash, int passId)
    {
        VariantKey = variantKey;
        RenderStateHash = renderStateHash;
        PassId = passId;
    }

    public bool Equals(GraphicsPipelineKey other) =>
        VariantKey.Equals(other.VariantKey) &&
        RenderStateHash == other.RenderStateHash &&
        PassId == other.PassId;

    public override bool Equals(object? obj) => obj is GraphicsPipelineKey other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(VariantKey, RenderStateHash, PassId);
}

/// <summary>
/// Mock interface for shader compiler (assumed to exist)
/// </summary>
public interface IShaderCompiler
{
    /// <summary>Compiles a shader variant for the given keyword set</summary>
    IntPtr CompileVariant(ShaderVariantKey key, in KeywordSet keywords);
}

/// <summary>
/// Mock interface for pipeline library (assumed to exist)
/// </summary>
public interface IPipelineLibrary
{
    /// <summary>Gets or creates a PSO for the given pipeline key</summary>
    IntPtr GetOrCreatePipeline(in GraphicsPipelineKey key);
}
