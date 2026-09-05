using Ghost.Core.Graphics;

namespace Ghost.Graphics;

/// <summary>
/// Provides allocation-free semantic shader rosters to the render thread.
/// </summary>
public interface IShaderVariantSource
{
    /// <summary>
    /// Returns the variants that implement the requested pass semantic.
    /// </summary>
    ReadOnlySpan<ShaderVariantDispatchInfo> GetDispatchVariants(PassSemantic semantic);

    /// <summary>
    /// Returns whether all bytecode for the specified dense variant has been atomically published.
    /// </summary>
    bool IsBytecodeReady(int denseIndex);
}
