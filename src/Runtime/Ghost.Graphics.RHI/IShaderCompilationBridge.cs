using Ghost.Core;

namespace Ghost.Graphics.RHI;

public interface IShaderCompilationBridge
{
    /// <summary>
    /// Request the bridge to recompile a shader variant or handle cache misses.
    /// This is typically called by the ShaderLibrary when a variant hash is not found.
    /// </summary>
    void RequestCompilation(ulong shaderId, int passIndex, Key64<ShaderVariant> variantKey);

    /// <summary>
    /// Event triggered when a shader variant has been successfully compiled and updated.
    /// </summary>
    event Action<Key64<ShaderVariant>, ulong> OnShaderVariantCompiled;
}
