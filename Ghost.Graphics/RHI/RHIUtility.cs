namespace Ghost.Graphics.RHI;

internal static class RHIUtility
{
    public static uint GetBytesPerPixel(this TextureFormat format)
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

    public static uint GetTotalBytes(this TextureDesc desc)
    {
        return desc.Format.GetBytesPerPixel() * desc.Width * desc.Height * desc.Slice;
    }

    public static void GetSurfaceInfo(this TextureFormat format, uint width, uint height, out uint rowPitch, out uint slicePitch, out uint rowCount)
    {
        var bc = false;
        var packed = false;
        var planar = false;
        var bpe = 0u;
        
        //switch (Format)
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
            var numBlocksWide = 0u;
            if (width > 0)
            {
                numBlocksWide = Math.Max(1u, (width + 3) / 4u);
            }

            var numBlocksHigh = 0u;
            if (height > 0)
            {
                numBlocksHigh = Math.Max(1u, (height + 3) / 4u);
            }

            rowPitch = numBlocksWide * bpe;
            rowCount = numBlocksHigh;
            slicePitch = rowPitch * numBlocksHigh;
        }
        else if (packed)
        {
            rowPitch = ((width + 1u) >> 1) * bpe;
            rowCount = height;
            slicePitch = rowPitch * height;
        }
        else if (planar)
        {
            rowPitch = ((width + 1u) >> 1) * bpe;
            slicePitch = (rowPitch * height) + ((rowPitch * height + 1) >> 1);
            rowCount = height + ((height + 1u) >> 1);
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
