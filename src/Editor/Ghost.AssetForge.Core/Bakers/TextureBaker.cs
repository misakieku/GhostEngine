using CommunityToolkit.Mvvm.ComponentModel;
using Ghost.AssetForge.Core.Attributes;
using Ghost.Core;
using Ghost.StbI;
using Misaki.HighPerformance.Mathematics;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace Ghost.AssetForge.Core.Bakers;

public enum TextureType : uint
{
    Default,
    Normal,
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

public partial class TextureBakeSettings : ObservableObject, IBakeSettings
{
    public partial class BasicSettings : ObservableObject
    {
        [ObservableProperty]
        public partial TextureType TextureType { get; set; } = TextureType.Default;

        [ObservableProperty]
        public partial TextureShape TextureShape { get; set; } = TextureShape.Texture2D;

        [ObservableProperty]
        [ShowWhen(nameof(TextureShape), TextureShape.Texture3D)]
        public partial int Columns { get; set; } = 1;

        [ObservableProperty]
        [ShowWhen(nameof(TextureShape), TextureShape.Texture3D)]
        public partial int Rows { get; set; } = 1;

        [ObservableProperty]
        [ShowWhen(nameof(TextureShape), TextureShape.Texture3D)]
        public partial int Depth { get; set; } = 1;

        [ObservableProperty]
        [ShowWhen(nameof(TextureType), TextureType.Default)]
        public partial bool IsSRGB { get; set; } = true;
    }

    public partial class AdvancedSettings : ObservableObject
    {
        [ObservableProperty]
        public partial TextureSize MaxSize { get; set; } = TextureSize.Size2048;

        [ObservableProperty]
        public partial bool StretchToPowerOfTwo { get; set; } = true;

        [ObservableProperty]
        public partial bool GenerateMipmaps { get; set; } = true;

        [ObservableProperty]
        [ShowWhen(nameof(GenerateMipmaps), true)]
        public partial uint MipmapLevelCount { get; set; } = 0; // 0 means generate full mipmap levels.

        [ObservableProperty]
        public partial bool PremultiplyAlpha { get; set; } = false;

        [ObservableProperty]
        public partial MipmapFilter MipmapFilter { get; set; } = MipmapFilter.Kaiser;

        [ObservableProperty]
        public partial TextureCompressionLevel CompressionLevel { get; set; } = TextureCompressionLevel.Normal;

        [ObservableProperty]
        public partial bool UseBorderColor { get; set; } = false;

        [ObservableProperty]
        [ShowWhen(nameof(UseBorderColor), true)]
        public partial float4 BorderColor { get; set; } = new float4(0, 0, 0, 0);

        [ObservableProperty]
        public partial bool ZeroAlphaBorder { get; set; } = false;

        [ObservableProperty]
        public partial bool CutoutAlpha { get; set; } = false;

        [ObservableProperty]
        [ShowWhen(nameof(CutoutAlpha), true)]
        [Slider(0, 255)]
        public partial byte CutoutAlphaThreshold { get; set; } = 127;

        [ObservableProperty]
        public partial bool ScaleAlphaForMipCoverage { get; set; } = false;

        [ObservableProperty]
        [ShowWhen(nameof(ScaleAlphaForMipCoverage), true)]
        [Slider(0, 255)]
        public partial byte ScaleAlphaForMipCoverageThreshold { get; set; } = 127;

        [ObservableProperty]
        public partial int4 CustomChannelMapping { get; set; } = new int4(0, 1, 2, 3); // Default mapping: R=0, G=1, B=2, A=3
    }

    public const int VERSION = 1;

    public int Version { get; set; } = VERSION; 

    [ObservableProperty]
    public partial BasicSettings Basic { get; set; } = new BasicSettings();

    [ObservableProperty]
    public partial AdvancedSettings Advanced { get; set; } = new AdvancedSettings();
}

internal struct TextureInfo
{
    public IntPtr pixelData;
    public int width;
    public int height;
    public int depth;
    public int bitsPerChannel;
    public int colorComponents;
    public bool isCube;
}

[AssetBaker(Extensions = [".png", ".jpg", ".jpeg", ".tga", ".bmp", ".hdr"], Type = AssetType.Texture, SettingsType = typeof(TextureBakeSettings), SettingsVersion = TextureBakeSettings.VERSION)]
internal partial class TextureBaker : IAssetBaker
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

    private static unsafe void SwizzleChannels<T>(T* pixelData, int width, int height, int4 channelMapping)
        where T : unmanaged
    {
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var pixelIndex = (y * width + x) * 4;
                var r = pixelData[pixelIndex + channelMapping.x];
                var g = pixelData[pixelIndex + channelMapping.y];
                var b = pixelData[pixelIndex + channelMapping.z];
                var a = pixelData[pixelIndex + channelMapping.w];
                pixelData[pixelIndex] = r;
                pixelData[pixelIndex + 1] = g;
                pixelData[pixelIndex + 2] = b;
                pixelData[pixelIndex + 3] = a;
            }
        }
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
                var isCube = settings.Basic.TextureShape == TextureShape.TextureCube;

                accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);

                int imageWidth, imageHeight, colorComponents;
                var bufferSpan = new ReadOnlySpan<byte>(ptr, (int)new FileInfo(sourcePath).Length);
                var bitsPerChannel = StbIApi.Is16BitFromMemory(bufferSpan) > 0 ? 16 : 8;
                var isFloat = isCube || bitsPerChannel > 8;

                void* pPixels;
                if (isFloat)
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

                if (!settings.Advanced.CustomChannelMapping.Equals(new uint4(0, 1, 2, 3)))
                {
                    if (math.any(settings.Advanced.CustomChannelMapping > 3) || math.any(settings.Advanced.CustomChannelMapping < 0))
                    {
                        throw new ArgumentException("Invalid custom channel mapping. All values must be between 0 and 3.");
                    }

                    if (isFloat)
                    {
                        SwizzleChannels((float*)pPixels, imageWidth, imageHeight, settings.Advanced.CustomChannelMapping);
                    }
                    else
                    {
                        SwizzleChannels((byte*)pPixels, imageWidth, imageHeight, settings.Advanced.CustomChannelMapping);
                    }
                }

                return new TextureInfo
                {
                    pixelData = (IntPtr)pPixels,
                    width = imageWidth,
                    height = imageHeight,
                    depth = 1,
                    bitsPerChannel = isFloat ? 32 : 8,
                    colorComponents = 4, // Forced 4 channels in stbi call
                    isCube = isCube,
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

    public async Task BakeAssetAsync(string src, Stream dst, IBakeSettings settings, AssetBakerContext ctx, CancellationToken cancellationToken)
    {
        if (settings is not TextureBakeSettings textureSettings)
        {
            throw new ArgumentException("Invalid settings type. Expected TextureBakeSettings.", nameof(settings));
        }

        var info = GetImageInfo(src, textureSettings);

        try
        {
            var (tempFilePath, mipCount, finalWidth, finalHeight) = await GenerateMipAndCompressAsync(info, textureSettings, cancellationToken).ConfigureAwait(false);

            try
            {
                var header = new TextureContentHeader
                {
                    width = (uint)finalWidth,
                    height = (uint)finalHeight,
                    bpc = (uint)info.bitsPerChannel,
                    colorComponents = (uint)info.colorComponents,
                    mipLevels = (uint)mipCount,
                    dimension = GetTextureDimension(textureSettings)
                };

                // Write header
                dst.Write(MemoryMarshal.AsBytes(new Span<TextureContentHeader>(ref header)));

                // Write DDS payload
                using var ddsStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                await ddsStream.CopyToAsync(dst, cancellationToken).ConfigureAwait(false);
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
