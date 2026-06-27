using Ghost.AssetForge.Core.Attributes;
using Ghost.Core;
using Ghost.StbI;
using System.IO.MemoryMappedFiles;
using System.Numerics;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;

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

public enum TextureDimension : uint
{
    Unknown = unchecked((uint)-1),
    None = 0,
    Texture1D = 1,
    Texture2D = 2,
    Texture3D = 3,
    TextureCube = 4,
    Texture2DArray = 5,
    TextureCubeArray = 6
}

public partial class TextureBakeSettings : ObservableObject, IBakeSettings
{
    public partial class BasicSettings : ObservableObject
    {
        [ObservableProperty]
        private TextureType _textureType = TextureType.Default;

        [ObservableProperty]
        private TextureShape _textureShape = TextureShape.Texture2D;

        [ObservableProperty]
        [property: ShowWhen(nameof(TextureShape), TextureShape.Texture3D)]
        private int _columns = 1;

        [ObservableProperty]
        [property: ShowWhen(nameof(TextureShape), TextureShape.Texture3D)]
        private int _rows = 1;

        [ObservableProperty]
        [property: ShowWhen(nameof(TextureShape), TextureShape.Texture3D)]
        private int _depth = 1;

        [ObservableProperty]
        [property: ShowWhen(nameof(TextureType), TextureType.Default)]
        private bool _isSRGB = true;

        public bool IsTexture3D => TextureShape == TextureShape.Texture3D;
        public bool IsDefaultType => TextureType == TextureType.Default;

        partial void OnTextureShapeChanged(TextureShape value) => OnPropertyChanged(nameof(IsTexture3D));
        partial void OnTextureTypeChanged(TextureType value) => OnPropertyChanged(nameof(IsDefaultType));
    }

    public partial class AdvancedSettings : ObservableObject
    {
        [ObservableProperty]
        private TextureSize _maxSize = TextureSize.Size2048;

        [ObservableProperty]
        private bool _stretchToPowerOfTwo = true;

        [ObservableProperty]
        private bool _generateMipmaps = true;

        [ObservableProperty]
        [property: ShowWhen(nameof(GenerateMipmaps), true)]
        private uint _mipmapLevelCount = 0; // 0 means generate full mipmap levels.

        [ObservableProperty]
        private bool _premultiplyAlpha = false;

        [ObservableProperty]
        private MipmapFilter _mipmapFilter = MipmapFilter.Kaiser;

        [ObservableProperty]
        private TextureCompressionLevel _compressionLevel = TextureCompressionLevel.Normal;

        [ObservableProperty]
        private bool _useBorderColor = false;

        [ObservableProperty]
        [property: ShowWhen(nameof(UseBorderColor), true)]
        private Vector4 _borderColor = new Vector4(0, 0, 0, 0);

        [ObservableProperty]
        private bool _zeroAlphaBorder = false;

        [ObservableProperty]
        private bool _cutoutAlpha = false;

        [ObservableProperty]
        [property: ShowWhen(nameof(CutoutAlpha), true)]
        [property: Slider(0, 255)]
        private byte _cutoutAlphaThreshold = 127;

        [ObservableProperty]
        private bool _scaleAlphaForMipCoverage = false;

        [ObservableProperty]
        [property: ShowWhen(nameof(ScaleAlphaForMipCoverage), true)]
        [property: Slider(0, 255)]
        private byte _scaleAlphaForMipCoverageThreshold = 127;

        public bool IsGenerateMipmaps => GenerateMipmaps;
        public bool IsUseBorderColor => UseBorderColor;
        public bool IsCutoutAlpha => CutoutAlpha;
        public bool IsScaleAlphaForMipCoverage => ScaleAlphaForMipCoverage;

        partial void OnGenerateMipmapsChanged(bool value) => OnPropertyChanged(nameof(IsGenerateMipmaps));
        partial void OnUseBorderColorChanged(bool value) => OnPropertyChanged(nameof(IsUseBorderColor));
        partial void OnCutoutAlphaChanged(bool value) => OnPropertyChanged(nameof(IsCutoutAlpha));
        partial void OnScaleAlphaForMipCoverageChanged(bool value) => OnPropertyChanged(nameof(IsScaleAlphaForMipCoverage));
    }

    [ObservableProperty]
    private BasicSettings _basic = new();

    [ObservableProperty]
    private AdvancedSettings _advanced = new();
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

[AssetBaker(Extensions = [".png", ".jpg", ".jpeg", ".tga", ".bmp", ".hdr"], Type = AssetType.Texture, SettingsType = typeof(TextureBakeSettings))]
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
                var bitsPerChannel = StbIApi.Is16BitFromMemory(bufferSpan) > 0 ? 16 : 8;

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

    public async Task BakeAssetAsync(string src, Stream dst, IBakeSettings settings, CancellationToken cancellationToken)
    {
        if (settings is not TextureBakeSettings textureSettings)
        {
            throw new ArgumentException("Invalid settings type. Expected TextureBakeSettings.", nameof(settings));
        }

        var info = GetImageInfo(src, textureSettings);

        try
        {
            var (tempFilePath, mipCount) = await GenerateMipAndCompressAsync(info, textureSettings, cancellationToken).ConfigureAwait(false);

            try
            {
                var header = new TextureContentHeader
                {
                    width = (uint)info.width,
                    height = (uint)info.height,
                    bpc = (uint)info.bitsPerChannel,
                    colorComponents = (uint)info.colorComponents,
                    mipLevels = (uint)mipCount,
                    dimension = (uint)GetTextureDimension(textureSettings)
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
