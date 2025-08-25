namespace Ghost.Graphics.RHI;

/// <summary>
/// Pipeline state object interface
/// </summary>
public interface IPipelineState : IDisposable
{
    /// <summary>
    /// Pipeline type (graphics or compute)
    /// </summary>
    PipelineType Type
    {
        get;
    }

    /// <summary>
    /// Pipeline name for debugging
    /// </summary>
    string Name
    {
        get; set;
    }
}

/// <summary>
/// Root signature interface
/// </summary>
public interface IRootSignature : IDisposable
{
    /// <summary>
    /// Root signature name for debugging
    /// </summary>
    string Name
    {
        get; set;
    }
}

/// <summary>
/// Pipeline types
/// </summary>
public enum PipelineType
{
    Graphics,
    Compute
}

/// <summary>
/// Render target description
/// Supports either color OR depth rendering, not both
/// </summary>
public struct RenderTargetDesc
{
    /// <summary>
    /// Width of the render target
    /// </summary>
    public uint Width;

    /// <summary>
    /// Height of the render target
    /// </summary>
    public uint Height;

    /// <summary>
    /// Type of render target (color or depth)
    /// </summary>
    public RenderTargetType Type;

    /// <summary>
    /// Target texture format
    /// </summary>
    public TextureFormat Format;

    /// <summary>
    /// Number of samples for MSAA
    /// </summary>
    public uint SampleCount;

    /// <summary>
    /// Creates a color render target
    /// </summary>
    public static RenderTargetDesc Color(uint width, uint height, TextureFormat format, uint sampleCount = 1)
    {
        return new RenderTargetDesc
        {
            Width = width,
            Height = height,
            Type = RenderTargetType.Color,
            Format = format,
            SampleCount = sampleCount
        };
    }

    /// <summary>
    /// Creates a depth render target
    /// </summary>
    public static RenderTargetDesc Depth(uint width, uint height, TextureFormat format = TextureFormat.D24_UNorm_S8_UInt, uint sampleCount = 1)
    {
        return new RenderTargetDesc
        {
            Width = width,
            Height = height,
            Type = RenderTargetType.Depth,
            Format = format,
            SampleCount = sampleCount
        };
    }
}

/// <summary>
/// Texture description
/// </summary>
public struct TextureDesc
{
    /// <summary>
    /// Width of the texture
    /// </summary>
    public uint Width;

    /// <summary>
    /// Height of the texture
    /// </summary>
    public uint Height;

    /// <summary>
    /// Texture format
    /// </summary>
    public TextureFormat Format;

    /// <summary>
    /// Number of mip levels
    /// </summary>
    public uint MipLevels;

    /// <summary>
    /// Texture usage flags
    /// </summary>
    public TextureUsage Usage;

    public TextureDesc(uint width, uint height, TextureFormat format, uint mipLevels = 1, TextureUsage usage = TextureUsage.ShaderResource)
    {
        Width = width;
        Height = height;
        Format = format;
        MipLevels = mipLevels;
        Usage = usage;
    }
}

/// <summary>
/// Buffer description
/// </summary>
public struct BufferDesc
{
    /// <summary>
    /// Size of the buffer in bytes
    /// </summary>
    public ulong Size;

    /// <summary>
    /// Buffer usage flags
    /// </summary>
    public BufferUsage Usage;

    /// <summary>
    /// Memory type for the buffer
    /// </summary>
    public MemoryType MemoryType;

    public BufferDesc(ulong size, BufferUsage usage, MemoryType memoryType = MemoryType.Default)
    {
        Size = size;
        Usage = usage;
        MemoryType = memoryType;
    }
}

/// <summary>
/// Texture usage flags
/// </summary>
[Flags]
public enum TextureUsage
{
    None = 0,
    ShaderResource = 1 << 0,
    RenderTarget = 1 << 1,
    DepthStencil = 1 << 2,
    UnorderedAccess = 1 << 3
}

/// <summary>
/// Memory types for resources
/// </summary>
public enum MemoryType
{
    Default,    // GPU memory
    Upload,     // CPU-to-GPU memory
    Readback    // GPU-to-CPU memory
}
