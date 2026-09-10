using Ghost.Engine.Streaming;

namespace Ghost.Engine.RenderPipeline;

/// <summary>
/// Represents a container of pipeline resources (shaders, materials, work graphs, textures)
/// that are automatically resolved from the AssetManager and disposed during pipeline shutdown.
/// </summary>
public interface IPipelineResource : IDisposable
{
    /// <summary>
    /// Resolves all resources decorated with [ResolveAsset] using the provided AssetManager.
    /// </summary>
    void Resolve(AssetManager assetManager);

    /// <summary>
    /// Resolves all resources decorated with [ResolveAsset] using the cached AssetManager.
    /// </summary>
    void Resolve();
}
