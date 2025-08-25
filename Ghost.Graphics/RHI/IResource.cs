namespace Ghost.Graphics.RHI;

/// <summary>
/// Base interface for all graphics resources
/// </summary>
public interface IResource : IDisposable
{
    /// <summary>
    /// Current resource state
    /// </summary>
    ResourceState CurrentState { get; }

    /// <summary>
    /// Resource name for debugging
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Size of the resource in bytes
    /// </summary>
    ulong Size { get; }
}

/// <summary>
/// Texture resource interface
/// </summary>
public interface ITexture : IResource
{
    /// <summary>
    /// Width of the texture in pixels
    /// </summary>
    uint Width { get; }

    /// <summary>
    /// Height of the texture in pixels
    /// </summary>
    uint Height { get; }

    /// <summary>
    /// Texture format
    /// </summary>
    TextureFormat Format { get; }

    /// <summary>
    /// Number of mip levels
    /// </summary>
    uint MipLevels { get; }
}

/// <summary>
/// Buffer resource interface
/// </summary>
public interface IBuffer : IResource
{
    /// <summary>
    /// Buffer usage type
    /// </summary>
    BufferUsage Usage { get; }

    /// <summary>
    /// Maps the buffer for CPU access
    /// </summary>
    /// <returns>Pointer to mapped memory</returns>
    unsafe void* Map();

    /// <summary>
    /// Unmaps the buffer from CPU access
    /// </summary>
    void Unmap();
}

/// <summary>
/// Render target interface for rendering operations
/// Supports either color OR depth rendering, not both
/// </summary>
public interface IRenderTarget : IDisposable
{
    /// <summary>
    /// Width of the render target
    /// </summary>
    uint Width { get; }

    /// <summary>
    /// Height of the render target
    /// </summary>
    uint Height { get; }

    /// <summary>
    /// Type of render target (color or depth)
    /// </summary>
    RenderTargetType Type { get; }

    /// <summary>
    /// Gets the target texture (either color or depth based on Type)
    /// </summary>
    ITexture Target { get; }
}

/// <summary>
/// Type of render target
/// </summary>
public enum RenderTargetType
{
    Color,
    Depth
}

/// <summary>
/// Texture format enumeration
/// </summary>
public enum TextureFormat
{
    Unknown,
    R8G8B8A8_UNorm,
    B8G8R8A8_UNorm,
    R16G16B16A16_Float,
    R32G32B32A32_Float,
    D24_UNorm_S8_UInt,
    D32_Float
}

/// <summary>
/// Buffer usage flags
/// </summary>
[Flags]
public enum BufferUsage
{
    None = 0,
    Vertex = 1 << 0,
    Index = 1 << 1,
    Constant = 1 << 2,
    Structured = 1 << 3,
    Raw = 1 << 4,
    Upload = 1 << 5,
    Readback = 1 << 6
}
