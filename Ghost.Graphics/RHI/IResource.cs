using Win32.Graphics.Dxgi.Common;

namespace Ghost.Graphics.RHI;

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
    Readback = 1 << 6,
    IndirectArgument = 1 << 7,

    ShaderResource = Vertex | Index | Constant
}

[Flags]
public enum BufferCreationFlags
{
    None = 0,
    Bindless = 1 << 0
}

public enum IndexType
{
    UInt16,
    UInt32
}

internal static class TextureFormatExtensions
{
    public static Format ToD3D12Format(this TextureFormat format)
    {
        return format switch
        {
            TextureFormat.R8G8B8A8_UNorm => Format.R8G8B8A8Unorm,
            TextureFormat.B8G8R8A8_UNorm => Format.B8G8R8A8Unorm,
            TextureFormat.R16G16B16A16_Float => Format.R16G16B16A16Float,
            TextureFormat.R32G32B32A32_Float => Format.R32G32B32A32Float,
            TextureFormat.D24_UNorm_S8_UInt => Format.D24UnormS8Uint,
            TextureFormat.D32_Float => Format.D32Float,
            _ => throw new NotSupportedException($"Texture format {format} is not supported."),
        };
    }

    public static TextureFormat ToTextureFormat(this Format format)
    {
        return format switch
        {
            Format.R8G8B8A8Unorm => TextureFormat.R8G8B8A8_UNorm,
            Format.B8G8R8A8Unorm => TextureFormat.B8G8R8A8_UNorm,
            Format.R16G16B16A16Float => TextureFormat.R16G16B16A16_Float,
            Format.R32G32B32A32Float => TextureFormat.R32G32B32A32_Float,
            Format.D24UnormS8Uint => TextureFormat.D24_UNorm_S8_UInt,
            Format.D32Float => TextureFormat.D32_Float,
            _ => TextureFormat.Unknown,
        };
    }

    public static int GetBytesPerPixel(this TextureFormat format)
    {
        return format switch
        {
            TextureFormat.R8G8B8A8_UNorm => 4,
            TextureFormat.B8G8R8A8_UNorm => 4,
            TextureFormat.R16G16B16A16_Float => 8,
            TextureFormat.R32G32B32A32_Float => 16,
            TextureFormat.D24_UNorm_S8_UInt => 4,
            TextureFormat.D32_Float => 4,
            _ => throw new NotSupportedException($"Texture format {format} is not supported."),
        };
    }

    public static void GetSurfaceInfo(this TextureFormat format, int width, int height, out int rowPitch, out int slicePitch, out int rowCount)
    {
        var bc = false;
        var packed = false;
        var planar = false;
        var bpe = 0;

        //switch (format)
        //{
        //    case Format.BC1Typeless:
        //    case Format.BC1Unorm:
        //    case Format.BC1UnormSrgb:
        //    case Format.BC4Typeless:
        //    case Format.BC4Unorm:
        //    case Format.BC4Snorm:
        //        bc = true;
        //        bpe = 8;
        //        break;

        //    case Format.BC2Typeless:
        //    case Format.BC2Unorm:
        //    case Format.BC2UnormSrgb:
        //    case Format.BC3Typeless:
        //    case Format.BC3Unorm:
        //    case Format.BC3UnormSrgb:
        //    case Format.BC5Typeless:
        //    case Format.BC5Unorm:
        //    case Format.BC5Snorm:
        //    case Format.BC6HTypeless:
        //    case Format.BC6HUF16:
        //    case Format.BC6HSF16:
        //    case Format.BC7Typeless:
        //    case Format.BC7Unorm:
        //    case Format.BC7UnormSrgb:
        //        bc = true;
        //        bpe = 16;
        //        break;

        //    case Format.R8G8_B8G8Unorm:
        //    case Format.G8R8_G8B8Unorm:
        //    case Format.YUY2:
        //        packed = true;
        //        bpe = 4;
        //        break;

        //    case Format.Y210:
        //    case Format.Y216:
        //        packed = true;
        //        bpe = 8;
        //        break;

        //    case Format.NV12:
        //    case Format.Opaque420:
        //    case Format.P208:
        //        planar = true;
        //        bpe = 2;
        //        break;

        //    case Format.P010:
        //    case Format.P016:
        //        planar = true;
        //        bpe = 4;
        //        break;

        //    default:
        //        break;
        //}

        if (bc)
        {
            var numBlocksWide = 0;
            if (width > 0)
            {
                numBlocksWide = Math.Max(1, (width + 3) / 4);
            }
            var numBlocksHigh = 0;
            if (height > 0)
            {
                numBlocksHigh = Math.Max(1, (height + 3) / 4);
            }
            rowPitch = numBlocksWide * bpe;
            rowCount = numBlocksHigh;
            slicePitch = rowPitch * numBlocksHigh;
        }
        else if (packed)
        {
            rowPitch = ((width + 1) >> 1) * bpe;
            rowCount = height;
            slicePitch = rowPitch * height;
        }
        else if (planar)
        {
            rowPitch = ((width + 1) >> 1) * bpe;
            slicePitch = (rowPitch * height) + ((rowPitch * height + 1) >> 1);
            rowCount = (int)(height + ((height + 1u) >> 1));
        }
        else
        {
            var bpp = GetBytesPerPixel(format) * 8;
            rowPitch = (width * bpp + 7) / 8; // round up to nearest byte
            rowCount = height;
            slicePitch = rowPitch * height;
        }
    }
}