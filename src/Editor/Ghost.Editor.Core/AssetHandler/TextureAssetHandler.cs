using Ghost.Core;
using Ghost.Engine.AssetLoader;
using Ghost.Graphics.RHI;
using ImageMagick;
using System.Runtime.InteropServices;

namespace Ghost.Editor.Core.AssetHandler;

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

public class TextureAssetSettings : IAssetSettings
{
    public struct BasicSettings()
    {
        public TextureType TextureType
        {
            get; set;
        } = TextureType.Default;

        public TextureShape TextureShape
        {
            get; set;
        } = TextureShape.Texture2D;

        public int Columns
        {
            get; set;
        } = 1;

        public int Rows
        {
            get; set;
        } = 1;

        public bool IsSRGB
        {
            get; set;
        } = true;
    }

    public struct AdvancedSettings()
    {
        public bool StretchToPowerOfTwo
        {
            get; set;
        } = true;

        public bool VirtualTexture
        {
            get; set;
        } = false;

        public bool GenerateMipmaps
        {
            get; set;
        } = true;

        public uint MipmapLevelCount
        {
            get; set;
        } = 0; // 0 means generate full mipmap levels.

        public bool GammaCorrection
        {
            get; set;
        } = true;

        public bool PremultiplyAlpha
        {
            get; set;
        } = false;

        public MipmapFilter MipmapFilter
        {
            get; set;
        } = MipmapFilter.Kaiser;

        public TextureCompressionLevel CompressionLevel
        {
            get; set;
        } = TextureCompressionLevel.Normal;

        public bool UseBorderColor
        {
            get; set;
        } = false;

        public Color128 BorderColor
        {
            get; set;
        } = new Color128(0, 0, 0, 0);

        public bool ZeroAlphaBorder
        {
            get; set;
        } = false;

        public bool CutoutAlpha
        {
            get; set;
        } = false;

        public byte CutoutAlphaThreshold
        {
            get; set;
        } = 127;

        public bool ScaleAlphaForMipCoverage
        {
            get; set;
        } = false;

        public byte ScaleAlphaForMipCoverageThreshold
        {
            get; set;
        } = 127;

        public bool MipmapStreaming
        {
            get; set;
        } = false;
    }

    public struct SamplerSettings()
    {
        public TextureSize MaxSize
        {
            get; set;
        } = TextureSize.Size2048;

        public TextureFilterMode FilterMode
        {
            get; set;
        } = TextureFilterMode.Anisotropic;

        public TextureAddressMode WrapMode
        {
            get; set;
        } = TextureAddressMode.Repeat;
    }

    public BasicSettings Basic
    {
        get; set;
    } = new BasicSettings();

    public AdvancedSettings Advanced
    {
        get; set;
    } = new AdvancedSettings();

    public SamplerSettings Sampler
    {
        get; set;
    } = new SamplerSettings();
}

[CustomAssetHandler(_GUID, [".png", ".jpg", ".jpeg", ".tga", ".bmp", ".hdr"], 1)]
[Guid(_GUID)]
internal class TextureAssetHandler : IAssetHandler
{
    private const string _GUID = "27965FFF-860C-40EF-9123-1874D7DE9CDC";

    public IAssetSettings? CreateDefaultSettings()
    {
        return new TextureAssetSettings();
    }

    public async ValueTask<Result> ImportAsync(Stream sourceStream, Stream targetStream, Guid id, IAssetSettings? settings, CancellationToken token = default)
    {
        try
        {
            var textureSettings = settings as TextureAssetSettings ?? new TextureAssetSettings();
            using var image = new MagickImage(sourceStream);
            var bytes = image.ToByteArray();

            var (path, mip) = await TextureProcessor.CompressToCacheAsync(EditorApplication.ImportsFolderPath, id, bytes, image.Width, image.Height, image.Depth, textureSettings, token)
                .ConfigureAwait(false);

            targetStream.Seek(0, SeekOrigin.Begin);

            var contentHeader = new TextureContentHeader
            {
                width = image.Width,
                height = image.Height,
                depth = image.Depth,
                colorComponents = image.ChannelCount,
                mipLevels = (uint)mip,
                dimension = (int)TextureDimension.Texture2D // TODO: Implement dimension calculation
            };

            targetStream.Write(MemoryMarshal.AsBytes(new Span<TextureContentHeader>(ref contentHeader)));

            await targetStream.WriteAsync(bytes, token).ConfigureAwait(false);
            await targetStream.FlushAsync(token).ConfigureAwait(false);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to import texture asset: {ex.Message}");
        }
    }

    public ValueTask<Result> ExportAsync(Stream assetStream, Stream targetStream, IAssetExportOptions? options, CancellationToken token = default)
    {
        return ValueTask.FromResult(Result.Failure("Exporting texture assets is not supported yet."));
    }
}
