using Ghost.Core;

namespace Ghost.Graphics.RHI;

public unsafe struct ShaderByteCode
{
    public byte* pCode;
    public ulong size;
}

public unsafe delegate void ShaderVariantCompiledHandler(ulong shaderId, int passIndex, Key64<ShaderVariant> variantKey, ReadOnlySpan<ShaderByteCode> byteCodes);

public interface IShaderCompilationBridge : IDisposable
{
    /// <summary>
    /// Request the bridge to recompile a shader variant or handle cache misses.
    /// This is typically called by the ShaderLibrary when a variant hash is not found.
    /// </summary>
    void RequestCompilation(ulong shaderId, int passIndex, Key64<ShaderVariant> variantKey, LocalKeywordSet keywordMask);

    /// <summary>
    /// Event triggered when a shader variant has been successfully compiled.
    /// </summary>
    event ShaderVariantCompiledHandler OnShaderVariantCompiled;

    /// <summary>
    /// Event triggered when a shader source has been imported or modified, requiring cache invalidation.
    /// </summary>
    event Action<ulong> OnShaderInvalidated;
}

