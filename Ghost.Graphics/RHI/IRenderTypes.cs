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
    /// Texture dimension
    /// </summary>
    public TextureDimension Dimension;

    /// <summary>
    /// Creation flags for the render target
    /// </summary>
    public RenderTargetCreationFlags CreationFlags;

    /// <summary>
    /// Number of mip levels. 0 to generate full mip chain
    /// </summary>
    public uint MipLevels;

    /// <summary>
    /// Number of samples for MSAA
    /// </summary>
    public uint SampleCount;

    /// <summary>
    /// Creates a color render target
    /// </summary>
    public static RenderTargetDesc Color(uint width, uint height,
        TextureFormat format, TextureDimension dimension = TextureDimension.Texture2D,
        RenderTargetCreationFlags creationFlags = RenderTargetCreationFlags.AllowUAV | RenderTargetCreationFlags.DynamicallyScalable | RenderTargetCreationFlags.GenerateMips,
        uint mipLevels = 0u, uint sampleCount = 1)
    {
        return new RenderTargetDesc
        {
            Width = width,
            Height = height,
            Type = RenderTargetType.Color,
            Format = format,
            Dimension = dimension,
            CreationFlags = creationFlags,
            MipLevels = mipLevels,
            SampleCount = sampleCount
        };
    }

    /// <summary>
    /// Creates a depth render target
    /// </summary>
    public static RenderTargetDesc Depth(uint width, uint height,
        TextureFormat format = TextureFormat.D24_UNorm_S8_UInt, TextureDimension dimension = TextureDimension.Texture2D,
        RenderTargetCreationFlags creationFlags = RenderTargetCreationFlags.AllowUAV | RenderTargetCreationFlags.DynamicallyScalable,
        uint mipLevels = 0u, uint sampleCount = 1)
    {
        return new RenderTargetDesc
        {
            Width = width,
            Height = height,
            Type = RenderTargetType.Depth,
            Format = format,
            Dimension = dimension,
            CreationFlags = creationFlags,
            MipLevels = mipLevels,
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
    /// Texture dimension
    /// </summary>
    public TextureDimension Dimension;

    /// <summary>
    /// Number of mip levels. 0 to generate full mip chain
    /// </summary>
    public uint MipLevels;

    /// <summary>
    /// Texture usage flags
    /// </summary>
    public TextureUsage Usage;

    public TextureDesc(uint width, uint height, TextureFormat format, TextureDimension dimension = TextureDimension.Texture2D, uint mipLevels = 0u, TextureUsage usage = TextureUsage.ShaderResource)
    {
        Width = width;
        Height = height;
        Format = format;
        Dimension = dimension;
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
/// Dimensions of the texture
/// </summary>
public enum TextureDimension
{
    Unknown = -1,
    None = 0,
    Texture2D = 1,
    Texture3D = 2,
    TextureCube = 3,
    Texture2DArray = 4,
    TextureCubeArray = 5
}

/// <summary>
/// Render target creation flags
/// </summary>
[Flags]
public enum RenderTargetCreationFlags
{
    None = 0,
    AllowUAV = 1 << 0,
    AllowMSAA = 1 << 1,
    DynamicallyScalable = 1 << 2,
    GenerateMips = 1 << 3
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
