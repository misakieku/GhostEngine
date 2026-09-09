using Ghost.Core;
using Ghost.Graphics.Core;

namespace Ghost.Graphics;

/// <summary>
/// Identifies one graphics shader variant in the current runtime catalog generation.
/// </summary>
/// <remarks>
/// <see cref="DenseIndex"/> is runtime-only and must not be serialized.
/// </remarks>
public readonly struct ShaderVariantDispatchInfo
{
    /// <summary>
    /// Gets the dense slot used by classification and per-variant indirect buffers.
    /// </summary>
    public int DenseIndex { get; }

    /// <summary>
    /// Gets the stable shader resource handle for this catalog generation.
    /// </summary>
    public Handle<Shader> Shader { get; }

    /// <summary>
    /// Creates dispatch metadata for one shader variant.
    /// </summary>
    public ShaderVariantDispatchInfo(int denseIndex, Handle<Shader> shader)
    {
        DenseIndex = denseIndex;
        Shader = shader;
    }
}
