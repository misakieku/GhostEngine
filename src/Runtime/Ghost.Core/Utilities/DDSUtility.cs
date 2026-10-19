using Ghost.Core.Graphics;

namespace Ghost.Core.Utilities;

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

    public static bool TryParseDdsHeader(Stream stream, long sizeInBytes, out DDS_HEADER header, out DDS_HEADER_DXT10? dxt10, out uint dataOffset)
    {
        header = default;
        dxt10 = default;
        dataOffset = 0;

        var startPosition = stream.CanSeek ? stream.Position : 0L;

        if (sizeInBytes < (sizeof(uint) + sizeof(DDS_HEADER)))
        {
            return false;
        }

        var magic = stream.Read<uint>();
        if (magic != DDS_MAGIC)
        {
            if (stream.CanSeek)
            {
                stream.Position = startPosition;
            }
            return false;
        }

        header = stream.Read<DDS_HEADER>();
        if (header.dwSize != sizeof(DDS_HEADER) || header.ddspf.dwSize != sizeof(DDS_PIXELFORMAT))
        {
            if (stream.CanSeek)
            {
                stream.Position = startPosition;
            }
            return false;
        }

        if ((header.ddspf.dwFlags & 0x00000004) != 0 && header.ddspf.dwFourCC == FOURCC_DX10)
        {
            var dxt10HeaderSize = sizeof(uint) + sizeof(DDS_HEADER) + sizeof(DDS_HEADER_DXT10);
            if (sizeInBytes < dxt10HeaderSize)
            {
                if (stream.CanSeek)
                {
                    stream.Position = startPosition;
                }
                return false;
            }

            dxt10 = stream.Read<DDS_HEADER_DXT10>();
            dataOffset = (uint)dxt10HeaderSize;
        }
        else
        {
            dataOffset = (uint)(sizeof(uint) + sizeof(DDS_HEADER));
        }

        return true;
    }

    public static void WriteDdsR16G16F(string path, int width, int height, ReadOnlySpan<Half> rawPixelData)
    {
        var header = new DDS_HEADER
        {
            dwSize = 124,
            dwFlags = 0x1007,           // CAPS | HEIGHT | WIDTH | PIXELFORMAT
            dwHeight = (uint)height,
            dwWidth = (uint)width,
            dwPitchOrLinearSize = (uint)(width * 4),
            dwDepth = 1,
            dwMipMapCount = 1,
            dwCaps = 0x1000,            // DDSCAPS_TEXTURE
            ddspf = new DDS_PIXELFORMAT
            {
                dwSize = 32,
                dwFlags = 0x4,          // DDPF_FOURCC
                dwFourCC = 'D' | ('X' << 8) | ('1' << 16) | ('0' << 24)
            }
        };

        var dx10Header = new DDS_HEADER_DXT10
        {
            dxgiFormat = 34,            // DXGI_FORMAT_R16G16_FLOAT
            resourceDimension = 3,      // Texture2D
            arraySize = 1
        };

        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);

        stream.Write(0x20534444); // "DDS "
        stream.Write(header);
        stream.Write(dx10Header);
        stream.Write(rawPixelData);
    }
}
