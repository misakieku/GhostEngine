namespace Ghost.Graphics.RHI;

[Flags]
public enum FeatureSupport
{
    None = 0,
    RayTracing = 1 << 0,
    VariableRateShading = 1 << 1,
    MeshShaders = 1 << 2,
    SamplerFeedback = 1 << 3,
    BindlessResources = 1 << 4,
    WorkGraphs = 1 << 5,
    AliasBuffersAndTextures = 1 << 6,
}

/// <summary>
/// D3D12-native render device interface for creating graphics resources
/// </summary>
public interface IRenderDevice : IDisposable
{
    /// <summary>
    /// Graphics command queue for rendering operations
    /// </summary>
    public ICommandQueue GraphicsQueue
    {
        get;
    }

    /// <summary>
    /// Compute command queue for compute shader operations
    /// </summary>
    public ICommandQueue ComputeQueue
    {
        get;
    }

    /// <summary>
    /// Copy command queue for data transfer operations
    /// </summary>
    public ICommandQueue CopyQueue
    {
        get;
    }

    public FeatureSupport FeatureSupport
    {
        get;
    }
}