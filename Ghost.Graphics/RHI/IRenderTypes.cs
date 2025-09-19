using System.Runtime.CompilerServices;

namespace Ghost.Graphics.RHI;

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
    public uint Width
    {
        get;
        set;
    }

    /// <summary>
    /// Height of the render target
    /// </summary>
    public uint Height
    {
        get;
        set;
    }

    /// <summary>
    /// Slice of the render target
    /// </summary>
    public uint Slice
    {
        get;
        set;
    }

    /// <summary>
    /// Type of render target
    /// </summary>
    public RenderTargetType Type
    {
        get;
        set;
    }

    /// <summary>
    /// Target texture format
    /// </summary>
    public TextureFormat Format
    {
        get;
        set;
    }

    /// <summary>
    /// Texture dimension
    /// </summary>
    public TextureDimension Dimension
    {
        get;
        set;
    }

    /// <summary>
    /// Creation flags for the render target
    /// </summary>
    public RenderTargetCreationFlags CreationFlags
    {
        get;
        set;
    }

    /// <summary>
    /// Number of mip levels. 0 to generate full mip chain
    /// </summary>
    public uint MipLevels
    {
        get;
        set;
    }

    /// <summary>
    /// Number of samples for MSAA
    /// </summary>
    public uint SampleCount
    {
        get;
        set;
    }

    /// <summary>
    /// Creates a color render target
    /// </summary>
    public static RenderTargetDesc Color(uint width, uint height, uint slice = 1,
        TextureFormat format = TextureFormat.R8G8B8A8_UNorm, TextureDimension dimension = TextureDimension.Texture2D,
        RenderTargetCreationFlags creationFlags = RenderTargetCreationFlags.AllowUAV | RenderTargetCreationFlags.DynamicallyScalable | RenderTargetCreationFlags.GenerateMips,
        uint mipLevels = 0u, uint sampleCount = 1)
    {
        return new RenderTargetDesc
        {
            Width = width,
            Height = height,
            Slice = slice,
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
    public static RenderTargetDesc Depth(uint width, uint height, uint slice = 1,
        TextureFormat format = TextureFormat.D24_UNorm_S8_UInt, TextureDimension dimension = TextureDimension.Texture2D,
        RenderTargetCreationFlags creationFlags = RenderTargetCreationFlags.AllowUAV | RenderTargetCreationFlags.DynamicallyScalable,
        uint mipLevels = 0u, uint sampleCount = 1)
    {
        return new RenderTargetDesc
        {
            Width = width,
            Height = height,
            Slice = slice,
            Type = RenderTargetType.Depth,
            Format = format,
            Dimension = dimension,
            CreationFlags = creationFlags,
            MipLevels = mipLevels,
            SampleCount = sampleCount
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TextureDesc ToTextureDescriptor(RenderTargetDesc desc)
    {
        var usage = desc.Type == RenderTargetType.Color ? TextureUsage.RenderTarget : TextureUsage.DepthStencil;
        if (desc.CreationFlags.HasFlag(RenderTargetCreationFlags.AllowUAV))
        {
            usage |= TextureUsage.UnorderedAccess;
        }

        return new TextureDesc
        {
            Width = desc.Width,
            Height = desc.Height,
            Slice = desc.Slice,
            Format = desc.Format,
            Dimension = desc.Dimension,
            MipLevels = desc.MipLevels,
            Usage = usage,
            CreationFlags = TextureCreationFlags.Bindless
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
    public uint Width
    {
        get;
        set;
    }

    /// <summary>
    /// Height of the texture
    /// </summary>
    public uint Height
    {
        get;
        set;
    }

    /// <summary>
    /// Slice of the texture
    /// </summary>
    public uint Slice
    {
        get;
        set;
    }

    /// <summary>
    /// Texture format
    /// </summary>
    public TextureFormat Format
    {
        get;
        set;
    }

    /// <summary>
    /// Texture dimension
    /// </summary>
    public TextureDimension Dimension
    {
        get;
        set;
    }

    /// <summary>
    /// Number of mip levels. 0 to generate full mip chain
    /// </summary>
    public uint MipLevels
    {
        get;
        set;
    }

    /// <summary>
    /// Texture usage flags
    /// </summary>
    public TextureUsage Usage
    {
        get;
        set;
    }

    /// <summary>
    /// Texture creation flags
    /// </summary>
    public TextureCreationFlags CreationFlags
    {
        get;
        set;
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
    public ulong Size
    {
        get;
        set;
    }

    public uint Stride
    {
        get;
        set;
    }

    /// <summary>
    /// Buffer usage flags
    /// </summary>
    public BufferUsage Usage
    {
        get;
        set;
    }

    public BufferCreationFlags CreationFlags
    {
        get;
        set;
    }

    /// <summary>
    /// Memory type for the buffer
    /// </summary>
    public MemoryType MemoryType
    {
        get;
        set;
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

[Flags]
public enum TextureCreationFlags
{
    None = 0,
    Bindless = 1 << 0
}