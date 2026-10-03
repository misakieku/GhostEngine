using Ghost.Core;
using System.Runtime.InteropServices;

namespace Ghost.Graphics.RHI;

[StructLayout(LayoutKind.Sequential)]
public struct DDS_PIXELFORMAT
{
    public uint dwSize;
    public uint dwFlags;
    public uint dwFourCC;
    public uint dwRGBBitCount;
    public uint dwRBitMask;
    public uint dwGBitMask;
    public uint dwBBitMask;
    public uint dwABitMask;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct DDS_HEADER
{
    public uint dwSize;
    public uint dwFlags;
    public uint dwHeight;
    public uint dwWidth;
    public uint dwPitchOrLinearSize;
    public uint dwDepth;
    public uint dwMipMapCount;
    public fixed uint dwReserved1[11];
    public DDS_PIXELFORMAT ddspf;
    public uint dwCaps;
    public uint dwCaps2;
    public uint dwCaps3;
    public uint dwCaps4;
    public uint dwReserved2;
}

[StructLayout(LayoutKind.Sequential)]
public struct DDS_HEADER_DXT10
{
    public uint dxgiFormat;
    public uint resourceDimension;
    public uint miscFlag;
    public uint arraySize;
    public uint miscFlags2;
}

public static unsafe class DdsUtility
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

    public static bool TryParseDdsHeader(void* pData, nuint sizeInBytes, out TextureDesc desc, out uint dataOffset)
    {
        desc = default;
        dataOffset = 0;

        if (sizeInBytes < (nuint)(sizeof(uint) + sizeof(DDS_HEADER)) || pData == null)
        {
            return false;
        }

        var magic = *(uint*)pData;
        if (magic != DDS_MAGIC)
        {
            return false;
        }

        var pHeader = (DDS_HEADER*)((byte*)pData + sizeof(uint));
        if (pHeader->dwSize != sizeof(DDS_HEADER) || pHeader->ddspf.dwSize != sizeof(DDS_PIXELFORMAT))
        {
            return false;
        }

        var width = pHeader->dwWidth;
        var height = pHeader->dwHeight;
        var mipLevels = Math.Max(1u, pHeader->dwMipMapCount);
        var format = TextureFormat.Unknown;
        var dimension = TextureDimension.Texture2D;
        var slice = 1u;

        if ((pHeader->ddspf.dwFlags & 0x00000004) != 0 && pHeader->ddspf.dwFourCC == FOURCC_DX10)
        {
            var dxt10HeaderSize = (nuint)(sizeof(uint) + sizeof(DDS_HEADER) + sizeof(DDS_HEADER_DXT10));
            if (sizeInBytes < dxt10HeaderSize)
            {
                return false;
            }

            var pDxt10 = (DDS_HEADER_DXT10*)((byte*)pData + sizeof(uint) + sizeof(DDS_HEADER));
            dataOffset = (uint)dxt10HeaderSize;
            format = RHIUtility.FromDxgiFormat(pDxt10->dxgiFormat);

            if ((pDxt10->miscFlag & RESOURCE_MISC_TEXTURECUBE) != 0 || (pHeader->dwCaps2 & DDSCAPS2_CUBEMAP) != 0)
            {
                dimension = TextureDimension.TextureCube;
                slice = 6;
            }
            else if (pDxt10->arraySize > 1)
            {
                dimension = TextureDimension.Texture2DArray;
                slice = pDxt10->arraySize;
            }
            else if (pDxt10->resourceDimension == 4) // D3D10_RESOURCE_DIMENSION_TEXTURE3D
            {
                dimension = TextureDimension.Texture3D;
                slice = Math.Max(1u, pHeader->dwDepth);
            }
        }
        else
        {
            dataOffset = (uint)(sizeof(uint) + sizeof(DDS_HEADER));
            if ((pHeader->ddspf.dwFlags & 0x00000004) != 0)
            {
                format = pHeader->ddspf.dwFourCC switch
                {
                    FOURCC_DXT1 => TextureFormat.BC1_UNorm,
                    FOURCC_DXT3 => TextureFormat.BC2_UNorm,
                    FOURCC_DXT5 => TextureFormat.BC3_UNorm,
                    FOURCC_ATI1 or FOURCC_BC4U => TextureFormat.BC4_UNorm,
                    FOURCC_ATI2 or FOURCC_BC5U => TextureFormat.BC5_UNorm,
                    _ => TextureFormat.Unknown,
                };
            }

            if ((pHeader->dwCaps2 & DDSCAPS2_CUBEMAP) != 0)
            {
                dimension = TextureDimension.TextureCube;
                slice = 6;
            }
            else if (pHeader->dwDepth > 1)
            {
                dimension = TextureDimension.Texture3D;
                slice = pHeader->dwDepth;
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

