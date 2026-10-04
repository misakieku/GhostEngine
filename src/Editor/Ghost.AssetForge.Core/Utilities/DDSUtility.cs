using Ghost.Core.Graphics;
using Ghost.Core.Utilities;

namespace Ghost.AssetForge.Core.Utilities;

internal static class DDSUtility
{
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
