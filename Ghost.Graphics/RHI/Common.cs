using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.D3D12.Utilities;
using Misaki.HighPerformance.Utilities;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop.DirectX;

namespace Ghost.Graphics.RHI;

public interface IRHIObject
{
    string Name
    {
        get; set;
    }
}

public readonly struct ShaderPassKey
{
    public readonly ulong value;

    public ShaderPassKey(ulong value)
    {
        this.value = value;
    }

    public ShaderPassKey(string passId)
    {
        var passIdSpan = passId.AsSpan();
        value = XxHash3.HashToUInt64(MemoryMarshal.AsBytes(passIdSpan));
    }

    public override string ToString()
    {
        return value.ToString("X16");
    }

    public override int GetHashCode()
    {
        return value.GetHashCode();
    }
}

public readonly struct GraphicsPipelineKey
{
    public const int KEY_STRING_LENGTH = 17; // 16 chars + null terminator

    public readonly ulong value;

    public GraphicsPipelineKey(ulong value)
    {
        this.value = value;
    }

    public Result GetString(Span<char> destination)
    {
        if (!value.TryFormat(destination, out _, "X16"))
        {
            return Result.Failure("Failed to format GraphicsPipelineKey to string.");
        }

        destination[16] = '\0';
        return Result.Success();
    }

    public override string ToString()
    {
        return value.ToString("X16");
    }

    public override int GetHashCode()
    {
        return value.GetHashCode();
    }
}

internal struct GraphicsPipelineHash
{
    [InlineArray(D3D12_SIMULTANEOUS_RENDER_TARGET_COUNT)]
    public struct rtv_array
    {
        public TextureFormat rtvFormats;
    }

    public ShaderPassKey Id
    {
        get; set;
    }

    public rtv_array RtvFormats;

    public uint RtvCount
    {
        get; set;
    }

    public TextureFormat DsvFormat
    {
        get; set;
    }

    // Do we need to store blend state?
    // TODO: Variants

    public GraphicsPipelineKey GetKey()
    {
        Span<ulong> data = stackalloc ulong[3 + D3D12_SIMULTANEOUS_RENDER_TARGET_COUNT];
        data[0] = Id.value;
        data[1] = RtvCount;
        data[2] = (ulong)DsvFormat;

        for (var i = 0; i < D3D12_SIMULTANEOUS_RENDER_TARGET_COUNT; i++)
        {
            data[3 + i] = (ulong)RtvFormats[i];
        }

        var bytes = MemoryMarshal.AsBytes(data);
        return new GraphicsPipelineKey(XxHash3.HashToUInt64(bytes));
    }
}

public ref struct GraphicsPSODescriptor
{
    public ShaderPassKey PassId
    {
        get; set;
    }

    public ZTestOptions ZTest
    {
        get; set;
    }

    public ZWriteOptions ZWrite
    {
        get; set;
    }

    public CullOptions Cull
    {
        get; set;
    }

    public BlendOptions Blend
    {
        get; set;
    }

    public uint ColorMask
    {
        get; set;
    }


    public ReadOnlySpan<TextureFormat> RtvFormats
    {
        get; set;
    }

    public TextureFormat DsvFormat
    {
        get; set;
    }
}

public readonly struct CBufferPropertyInfo
{
    public string Name
    {
        get; init;
    }

    public uint StartOffset
    {
        get; init;
    }

    public uint Size
    {
        get; init;
    }
}

public readonly struct CBufferInfo
{
    public string Name
    {
        get; init;
    }

    public uint RegisterSlot
    {
        get; init;
    }

    public uint RegisterSpace
    {
        get; init;
    }

    public uint SizeInBytes
    {
        get; init;
    }

    public IReadOnlyList<CBufferPropertyInfo> Properties
    {
        get; init;
    }
}

public struct ViewportDesc
{
    public float X
    {
        get; set;
    }

    public float Y
    {
        get; set;
    }

    public float Width
    {
        get; set;
    }

    public float Height
    {
        get; set;
    }

    public float MinDepth
    {
        get; set;
    }

    public float MaxDepth
    {
        get; set;
    }

}

public struct RectDesc
{
    public uint Left
    {
        get; set;
    }

    public uint Top
    {
        get; set;
    }

    public uint Right
    {
        get; set;
    }

    public uint Bottom
    {
        get; set;
    }

}

public struct SubResourceData
{
    public unsafe void* pData;
    public nint rowPitch;
    public nint slicePitch;
}

public struct PassRenderTargetDesc
{
    public Handle<Texture> Texture
    {
        get; set;
    }

    public Color128 ClearColor
    {
        get; set;
    }

}

public struct PassDepthStencilDesc
{
    public Handle<Texture> Texture
    {
        get; set;
    }

    public float ClearDepth
    {
        get; set;
    }

    public byte ClearStencil
    {
        get; set;
    }

}


public struct ResourceDesc
{
    [StructLayout(LayoutKind.Explicit)]
    private struct resource_union
    {
        [FieldOffset(0)]
        public TextureDesc textureDescription;
        [FieldOffset(0)]
        public BufferDesc bufferDescription;
    }

    private resource_union _desc;

    public TextureDesc TextureDescription
    {
        readonly get => _desc.textureDescription;
        set => _desc.textureDescription = value;
    }

    public BufferDesc BufferDescription
    {
        readonly get => _desc.bufferDescription;
        set => _desc.bufferDescription = value;
    }

    public static ResourceDesc Buffer(BufferDesc desc)
    {
        return new ResourceDesc
        {
            BufferDescription = desc
        };
    }

    public static ResourceDesc Texture(TextureDesc desc)
    {
        return new ResourceDesc
        {
            TextureDescription = desc
        };
    }

    internal static ResourceDesc FromD3D12(D3D12_RESOURCE_DESC desc)
    {
        if (desc.Dimension == D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_BUFFER)
        {
            return Buffer(new BufferDesc
            {
                Size = (uint)desc.Width,
                Stride = 0,
                Usage = BufferUsage.None,
                MemoryType = ResourceMemoryType.Default
            });
        }
        else
        {
            return Texture(new TextureDesc
            {
                Width = (uint)desc.Width,
                Height = desc.Height,
                Slice = desc.DepthOrArraySize,
                Format = desc.Format.ToTextureFormat(),
                Dimension = desc.Dimension.ToTextureDimension(),
                MipLevels = desc.MipLevels,
                Usage = TextureUsage.None,
            });
        }
    }
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
        get; set;
    }

    /// <summary>
    /// Height of the render target
    /// </summary>
    public uint Height
    {
        get; set;
    }

    /// <summary>
    /// Slice of the render target
    /// </summary>
    public uint Slice
    {
        get; set;
    }

    /// <summary>
    /// Type of render target
    /// </summary>
    public RenderTargetType Type
    {
        get; set;
    }

    /// <summary>
    /// Target texture format
    /// </summary>
    public TextureFormat Format
    {
        get; set;
    }

    /// <summary>
    /// Texture dimension
    /// </summary>
    public TextureDimension Dimension
    {
        get; set;
    }

    /// <summary>
    /// Creation flags for the render target
    /// </summary>
    public RenderTargetCreationFlags CreationFlags
    {
        get; set;
    }

    /// <summary>
    /// Number of mip levels. 0 to generate full mip chain
    /// </summary>
    public uint MipLevels
    {
        get; set;
    }

    /// <summary>
    /// Number of samples for MSAA
    /// </summary>
    public uint SampleCount
    {
        get; set;
    }

    /// <summary>
    /// Creates a color render target
    /// </summary>
    public static RenderTargetDesc Color(uint width, uint height, uint slice = 1,
        TextureFormat format = TextureFormat.R8G8B8A8_UNorm, TextureDimension dimension = TextureDimension.Texture2D,
        RenderTargetCreationFlags creationFlags = RenderTargetCreationFlags.AllowUAV | RenderTargetCreationFlags.DynamicallyResolution | RenderTargetCreationFlags.GenerateMips,
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
        RenderTargetCreationFlags creationFlags = RenderTargetCreationFlags.AllowUAV | RenderTargetCreationFlags.DynamicallyResolution,
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
    public TextureDesc ToTextureDescripton()
    {
        var usage = Type == RenderTargetType.Color ? TextureUsage.RenderTarget : TextureUsage.DepthStencil;
        if (CreationFlags.HasFlag(RenderTargetCreationFlags.AllowUAV))
        {
            usage |= TextureUsage.UnorderedAccess;
        }

        return new TextureDesc
        {
            Width = Width,
            Height = Height,
            Slice = Slice,
            Format = Format,
            Dimension = Dimension,
            MipLevels = MipLevels,
            Usage = usage,
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
        get; set;
    }

    /// <summary>
    /// Height of the texture
    /// </summary>
    public uint Height
    {
        get; set;
    }

    /// <summary>
    /// Slice of the texture
    /// </summary>
    public uint Slice
    {
        get; set;
    }

    /// <summary>
    /// Texture format
    /// </summary>
    public TextureFormat Format
    {
        get; set;
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
        get; set;
    }

    /// <summary>
    /// Texture usage flags
    /// </summary>
    public TextureUsage Usage
    {
        get; set;
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
        get; set;
    }

    public uint Stride
    {
        get; set;
    }

    /// <summary>
    /// Buffer usage flags
    /// </summary>
    public BufferUsage Usage
    {
        get; set;
    }

    /// <summary>
    /// Memory type for the buffer
    /// </summary>
    public ResourceMemoryType MemoryType
    {
        get; set;
    }
}

/// <summary>
/// Swap chain description
/// </summary>
public struct SwapChainDesc
{
    /// <summary>
    /// Width of the swap chain
    /// </summary>
    public uint width;

    /// <summary>
    /// Height of the swap chain
    /// </summary>
    public uint height;

    /// <summary>
    /// Back buffer format
    /// </summary>
    public TextureFormat format;

    /// <summary>
    /// Target for presentation (window handle or composition target)
    /// </summary>
    public SwapChainTarget target;

    public SwapChainDesc(uint width, uint height, SwapChainTarget target, TextureFormat format = TextureFormat.B8G8R8A8_UNorm, uint bufferCount = 2)
    {
        this.width = width;
        this.height = height;
        this.format = format;
        this.target = target;
    }
}

/// <summary>
/// Swap chain target (window handle or composition surface)
/// </summary>
public struct SwapChainTarget
{
    /// <summary>
    /// Target type
    /// </summary>
    public SwapChainTargetType type;

    /// <summary>
    /// Window handle for HWND targets
    /// </summary>
    public nint windowHandle;

    /// <summary>
    /// Composition surface for UWP/WinUI targets
    /// </summary>
    public object? compositionSurface;

    public static SwapChainTarget FromWindowHandle(nint hwnd)
    {
        return new SwapChainTarget
        {
            type = SwapChainTargetType.WindowHandle,
            windowHandle = hwnd,
            compositionSurface = null
        };
    }

    public static SwapChainTarget FromCompositionSurface(object surface)
    {
        return new SwapChainTarget
        {
            type = SwapChainTargetType.Composition,
            windowHandle = nint.Zero,
            compositionSurface = surface
        };
    }
}

public enum SwapChainTargetType
{
    WindowHandle,
    Composition
}


[Flags]
public enum ResourceState
{
    Common = 0,
    VertexAndConstantBuffer = 1 << 0,
    IndexBuffer = 1 << 1,
    RenderTarget = 1 << 2,
    UnorderedAccess = 1 << 3,
    DepthWrite = 1 << 4,
    DepthRead = 1 << 5,
    PixelShaderResource = 1 << 6,
    CopyDest = 1 << 7,
    CopySource = 1 << 8,
    GenericRead = 1 << 9,
    IndirectArgument = 1 << 10,
    Present = 0,
}

public enum CommandQueueType
{
    Graphics,
    Compute,
    Copy
}

public enum CommandBufferType
{
    Graphics,
    Compute,
    Copy
}

public enum PipelineType
{
    Graphics,
    Compute
}

[Flags]
public enum RenderTargetCreationFlags
{
    None = 0,
    AllowUAV = 1 << 0,
    AllowMSAA = 1 << 1,
    DynamicallyResolution = 1 << 2,
    GenerateMips = 1 << 3
}

public enum ResourceMemoryType
{
    Default,    // GPU memory
    Upload,     // CPU-to-GPU memory
    Readback    // GPU-to-CPU memory
}

[Flags]
public enum TextureUsage
{
    None = 0,
    ShaderResource = 1 << 0,
    RenderTarget = 1 << 1,
    DepthStencil = 1 << 2,
    UnorderedAccess = 1 << 3
}

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

public enum RenderTargetType
{
    Color,
    Depth
}

// TODO: Support compressed formats (BCn, ASTC, ETC2, etc)
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

[Flags]
public enum BufferUsage
{
    None = 0,
    Vertex = 1 << 0,
    Index = 1 << 1,
    IndirectArgument = 1 << 7,
    Constant = 1 << 2,
    ShaderResource = 1 << 3,
    UnorderedAccess = 1 << 4,
    Structured = 1 << 5,
    Raw = 1 << 6,
    Upload = 1 << 8,
    Readback = 1 << 9,
}

public enum IndexType
{
    UInt16,
    UInt32
}

public enum PrimitiveTopology
{
    Point,
    Line,
    Triangle,
}
