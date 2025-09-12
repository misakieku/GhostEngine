namespace Ghost.Graphics.RHI;

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
}

/// <summary>
/// Command buffer types matching D3D12 command list types
/// </summary>
public enum CommandBufferType
{
    Graphics,
    Compute,
    Copy
}
