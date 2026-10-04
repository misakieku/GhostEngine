using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;

namespace Ghost.Graphics.RHI;

public static unsafe class DDSUtility
{
    public const uint DDS_MAGIC = 0x20534444; // "DDS "
    public const uint FOURCC_DX10 = 0x30315844; // "DX10"
    public const uint FOURCC_DXT1 = 0x31545844; // "DXT1"
    public const uint FOURCC_DXT3 = 0x33545844; // "DXT3"
    public const uint FOURCC_DXT5 = 0x35545844; // "DXT5"
    public const uint FOURCC_ATI1 = 0x31495441; // "ATI1"
    public const uint FOURCC_BC4U = 0x55344342; // "BC4U"
    public const uint FOURCC_ATI2 = 0x32495441; // "ATI2"
    public const uint FOURCC_BC5U = 0x55354342; // "BC5U"

    public const uint DDSCAPS2_CUBEMAP = 0x00000200;
    public const uint RESOURCE_MISC_TEXTURECUBE = 0x4;

    public static bool TryParseDdsHeader(Stream stream, long sizeInBytes, out TextureDesc desc, out uint dataOffset)
    {
        desc = default;
        dataOffset = 0;

        if (sizeInBytes < (sizeof(uint) + sizeof(DDS_HEADER)))
        {
            return false;
        }

        var magic = stream.Read<uint>();
        if (magic != DDS_MAGIC)
        {
            return false;
        }

        var pHeader = stream.Read<DDS_HEADER>();
        if (pHeader.dwSize != sizeof(DDS_HEADER) || pHeader.ddspf.dwSize != sizeof(DDS_PIXELFORMAT))
        {
            return false;
        }

        var width = pHeader.dwWidth;
        var height = pHeader.dwHeight;
        var mipLevels = Math.Max(1u, pHeader.dwMipMapCount);
        var dimension = TextureDimension.Texture2D;
        var slice = 1u;

        TextureFormat format;

        if ((pHeader.ddspf.dwFlags & 0x00000004) != 0 && pHeader.ddspf.dwFourCC == FOURCC_DX10)
        {
            var dxt10HeaderSize = sizeof(uint) + sizeof(DDS_HEADER) + sizeof(DDS_HEADER_DXT10);
            if (sizeInBytes < dxt10HeaderSize)
            {
                return false;
            }

            var pDxt10 = stream.Read<DDS_HEADER_DXT10>();
            dataOffset = (uint)dxt10HeaderSize;
            format = RHIUtility.FromDxgiFormat(pDxt10.dxgiFormat);

            if ((pDxt10.miscFlag & RESOURCE_MISC_TEXTURECUBE) != 0 || (pHeader.dwCaps2 & DDSCAPS2_CUBEMAP) != 0)
            {
                dimension = TextureDimension.TextureCube;
                slice = 6;
            }
            else if (pDxt10.arraySize > 1)
            {
                dimension = TextureDimension.Texture2DArray;
                slice = pDxt10.arraySize;
            }
            else if (pDxt10.resourceDimension == 4) // D3D10_RESOURCE_DIMENSION_TEXTURE3D
            {
                dimension = TextureDimension.Texture3D;
                slice = Math.Max(1u, pHeader.dwDepth);
            }
        }
        else
        {
            dataOffset = (uint)(sizeof(uint) + sizeof(DDS_HEADER));
            {
                format = pHeader.ddspf.dwFourCC switch
                {
                    FOURCC_DXT1 => TextureFormat.BC1_UNorm,
                    FOURCC_DXT3 => TextureFormat.BC2_UNorm,
                    FOURCC_DXT5 => TextureFormat.BC3_UNorm,
                    FOURCC_ATI1 or FOURCC_BC4U => TextureFormat.BC4_UNorm,
                    FOURCC_ATI2 or FOURCC_BC5U => TextureFormat.BC5_UNorm,
                    _ => TextureFormat.Unknown,
                };
            }

            if ((pHeader.dwCaps2 & DDSCAPS2_CUBEMAP) != 0)
            {
                dimension = TextureDimension.TextureCube;
                slice = 6;
            }
            else if (pHeader.dwDepth > 1)
            {
                dimension = TextureDimension.Texture3D;
                slice = pHeader.dwDepth;
            }
        }

        if (format == TextureFormat.Unknown)
        {
            return false;
        }

        desc = new TextureDesc
        {
            Width = width,
            Height = height,
            MipLevels = mipLevels,
            Slice = slice,
            Format = format,
            Dimension = dimension,
            Usage = TextureUsage.ShaderResource,
        };

        return true;
    }
}

