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
    Int64Atomics = 1 << 7,
}

public readonly struct DeviceFetureSupport
{
    public FeatureSupport SupportedFeatures
    {
        get; init;
    }

    public uint MaxGPUVirtualAddressBitsPerResource
    {
        get; init;
    }
}

/// <summary>
/// D3D12-native render device interface for creating graphics resources
/// </summary>
public interface IRenderDevice : IRHIObject
{
    /// <summary>
    /// Graphics command queue for rendering operations
    /// </summary>
    ICommandQueue GraphicsQueue
    {
        get;
    }

    /// <summary>
    /// Compute command queue for compute shader operations
    /// </summary>
    ICommandQueue ComputeQueue
    {
        get;
    }

    /// <summary>
    /// Copy command queue for data transfer operations
    /// </summary>
    ICommandQueue CopyQueue
    {
        get;
    }

    /// <summary>
    /// Device feature support information
    /// </summary>
    DeviceFetureSupport DeviceFeture
    {
        get;
    }
}