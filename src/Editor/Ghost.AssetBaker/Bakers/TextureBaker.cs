using Ghost.StbI;
using System.IO.MemoryMappedFiles;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Ghost.AssetBaker.Bakers;

public enum TextureType : uint
{
    Default,
    Normal,
    Lightmap,
    SingleChannel
}

public enum TextureShape : uint
{
    Texture2D,
    Texture3D,
    TextureCube
}

public enum TextureSize : uint
{
    Size256 = 256,
    Size512 = 512,
    Size1024 = 1024,
    Size2048 = 2048,
    Size4096 = 4096,
    Size8192 = 8192
}

public enum TextureCompressionLevel : uint
{
    Low,
    Normal,
    High
}

public enum MipmapFilter : uint
{
    Box,
    Triangle,
    Kaiser,
    MitchellNetravali
}

public enum TextureFilterMode
{
    Point,
    Bilinear,
    Trilinear,
    Anisotropic
}

public enum TextureAddressMode
{
    Repeat,
    Mirror,
    Clamp,
    Border,
    MirrorOnce
}

public class TextureBakeSettings : IBakeSettings
{
    public struct BasicSettings()
    {
        public TextureType TextureType { get; set; } = TextureType.Default;
        public TextureShape TextureShape { get; set; } = TextureShape.Texture2D;
        public int Columns { get; set; } = 1;
        public int Rows { get; set; } = 1;
        public int Depth { get; set; } = 1;
        public bool IsSRGB { get; set; } = true;
    }

    public struct AdvancedSettings()
    {
        public bool StretchToPowerOfTwo { get; set; } = true;
        public bool VirtualTexture { get; set; } = false;
        public bool GenerateMipmaps { get; set; } = true;
        public uint MipmapLevelCount { get; set; } = 0; // 0 means generate full mipmap levels.
        public bool PremultiplyAlpha { get; set; } = false;
        public MipmapFilter MipmapFilter { get; set; } = MipmapFilter.Kaiser;
        public TextureCompressionLevel CompressionLevel { get; set; } = TextureCompressionLevel.Normal;
        public bool UseBorderColor { get; set; } = false;
        public Vector4 BorderColor { get; set; } = new Vector4(0, 0, 0, 0);
        public bool ZeroAlphaBorder { get; set; } = false;
        public bool CutoutAlpha { get; set; } = false;
        public byte CutoutAlphaThreshold { get; set; } = 127;
        public bool ScaleAlphaForMipCoverage { get; set; } = false;
        public byte ScaleAlphaForMipCoverageThreshold { get; set; } = 127;
        public bool MipmapStreaming { get; set; } = false;
    }

    public struct SamplerSettings()
    {
        public TextureSize MaxSize { get; set; } = TextureSize.Size2048;
        public TextureFilterMode FilterMode { get; set; } = TextureFilterMode.Anisotropic;
        public TextureAddressMode WrapMode { get; set; } = TextureAddressMode.Repeat;
    }

    public BasicSettings Basic { get; set; } = new BasicSettings();
    public AdvancedSettings Advanced { get; set; } = new AdvancedSettings();
    public SamplerSettings Sampler { get; set; } = new SamplerSettings();
}

internal struct TextureInfo
{
    public IntPtr pixelData;
    public int width;
    public int height;
    public int depth;
    public int bitsPerChannel;
    public int colorComponents;
    public bool isHDR;
}

[AssetBaker(Extensions = [".png", ".jpg", ".jpeg", ".tga", ".bmp", ".hdr"])]
internal partial class TextureBaker : IAssetBacker
{
    private static TextureDimension GetTextureDimension(TextureBakeSettings settings)
    {
        if (settings.Basic.Columns > 1 && settings.Basic.Rows > 1)
        {
            if (settings.Basic.Depth > 1)
            {
                return TextureDimension.Texture3D;
            }
            return TextureDimension.Texture2DArray;
        }

        if (settings.Basic.Columns == 1 && settings.Basic.Rows == 1)
        {
            if (settings.Basic.Depth == 6)
            {
                return TextureDimension.TextureCube;
            }
            else if (settings.Basic.Depth > 6 && settings.Basic.Depth % 6 == 0)
            {
                return TextureDimension.TextureCubeArray;
            }
        }

        return TextureDimension.Texture2D;
    }

    private static unsafe TextureInfo GetImageInfo(string sourcePath, TextureBakeSettings settings)
    {
        try
        {
            using var mmf = MemoryMappedFile.CreateFromFile(sourcePath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
            using var accessor = mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            
            byte* ptr = null;
            try
            {
                var ext = Path.GetExtension(sourcePath);
                var isHDR = ext.Equals(".hdr", StringComparison.OrdinalIgnoreCase) || settings.Basic.TextureShape == TextureShape.TextureCube;

                accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);

                int imageWidth, imageHeight, colorComponents;
                var bufferSpan = new ReadOnlySpan<byte>(ptr, (int)accessor.Capacity);
                int bitsPerChannel = StbIApi.Is16BitFromMemory(bufferSpan) > 0 ? 16 : 8;

                void* pPixels;
                if (isHDR || bitsPerChannel > 8)
                {
                    pPixels = StbIApi.LoadfFromMemory(bufferSpan, &imageWidth, &imageHeight, &colorComponents, 4);
                }
                else
                {
                    pPixels = StbIApi.LoadFromMemory(bufferSpan, &imageWidth, &imageHeight, &colorComponents, 4);
                }

                if (pPixels == null)
                {
                    throw new Exception($"Failed to decode image using StbIApi: {sourcePath}");
                }

                return new TextureInfo
                {
                    pixelData = (IntPtr)pPixels,
                    width = imageWidth,
                    height = imageHeight,
                    depth = 1,
                    bitsPerChannel = bitsPerChannel,
                    colorComponents = 4, // Forced 4 channels in stbi call
                    isHDR = isHDR,
                };
            }
            finally
            {
                if (ptr != null)
                {
                    accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to load image info: {ex.Message}", ex);
        }
    }

    public async Task<Stream> BakeAssetAsync(string assetPath, IBakeSettings settings)
    {
        if (settings is not TextureBakeSettings textureSettings)
        {
            throw new ArgumentException("Invalid settings type. Expected TextureBakeSettings.", nameof(settings));
        }

        if (!File.Exists(assetPath))
        {
            throw new FileNotFoundException("Source file does not exist.", assetPath);
        }

        var info = GetImageInfo(assetPath, textureSettings);

        try
        {
            var (tempFilePath, mipCount) = await GenerateMipAndCompressAsync(info, textureSettings).ConfigureAwait(false);

            try
            {
                var targetStream = new MemoryStream();
                var header = new TextureContentHeader
                {
                    magic = TextureContentHeader.MAGIC,
                    version = TextureContentHeader.VERSION,
                    width = (uint)info.width,
                    height = (uint)info.height,
                    bpc = (uint)info.bitsPerChannel,
                    colorComponents = (uint)info.colorComponents,
                    mipLevels = (uint)mipCount,
                    dimension = (uint)GetTextureDimension(textureSettings)
                };

                // Write header
                targetStream.Write(MemoryMarshal.AsBytes(new Span<TextureContentHeader>(ref header)));

                // Write DDS payload
                using (var ddsStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    await ddsStream.CopyToAsync(targetStream).ConfigureAwait(false);
                }

                targetStream.Position = 0;
                return targetStream;
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    try { File.Delete(tempFilePath); } catch { }
                }
            }
        }
        finally
        {
            // Free the decoded pixels
            unsafe { StbIApi.ImageFree((void*)info.pixelData); }
        }
    }
}
