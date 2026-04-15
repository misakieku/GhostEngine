using Ghost.Core;
using Ghost.Editor.Core.Contracts;
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

public class TextureAsset : Asset
{
    internal const string _TYPE_ID = "0906F4EB-C3F0-431B-BCEA-132C88AB0C3F";
    internal static readonly Guid s_typeGuid = Guid.Parse(_TYPE_ID);

    private readonly byte[] _textureData;
    private readonly uint _width;
    private readonly uint _height;
    private readonly uint _depth;
    private readonly uint _colorComponents;

    public override Guid TypeID => s_typeGuid;

    /// <summary>
    /// Gets the raw texture data in a compressed format.
    /// </summary>
    public ReadOnlyMemory<byte> TextureData => _textureData;

    /// <summary>
    /// Gets the width of the texture in pixels.
    /// </summary>
    public uint Width => _width;

    /// <summary>
    /// Gets the height of the texture in pixels.
    /// </summary>
    public uint Height => _height;

    /// <summary>
    /// Gets the bit depth of the texture.
    /// </summary>
    public uint Depth => _depth;

    /// <summary>
    /// Gets the number of color components in the texture.
    /// </summary>
    public uint ColorComponents => _colorComponents;

    internal TextureAsset(byte[] data, ImageContentHeader header, Guid id)
        : base(id)
    {
        _textureData = data;
        _width = header.width;
        _height = header.height;
        _depth = header.depth;
        _colorComponents = header.colorComponents;
    }
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

[StructLayout(LayoutKind.Sequential, Size = 64)] // Leave extra space for future expansion without breaking compatibility
internal struct ImageContentHeader
{
    public uint width;
    public uint height;
    public uint depth;
    public uint colorComponents;
}

[CustomAssetHandler(TextureAsset._TYPE_ID, [ ".png", ".jpg", ".jpeg", ".tga", ".bmp", ".hdr" ], 1)]
internal class TextureAssetHandler : IImportableAssetHandler
{
    public IAssetSettings? CreateDefaultSettings()
    {
        return new TextureAssetSettings();
    }

    public async ValueTask<Result<Asset>> LoadAsync(Stream sourceStream, Guid id, IAssetRegistry assetRegistry, CancellationToken token = default)
    {
        try
        {
            // FIX: Should the sourceStream be the stream of the imported file or the raw asset file?
            // Or should we change our paramemters to inlcude more information and let each handler decide how to load the asset?
            // The problem of a single sourceStream is, for example, for texture assets, we don't even need to read the ".png" file at all,
            // but for some other asset types, we may don't even have imported intermediate files at all.

            // var path = assetRegistry.GetAssetPath(id);
            // if (string.IsNullOrEmpty(path))
            // {
            //     return Result.Failure("Asset path not found in registry.");
            // }
            //
            // var metadataPath = AssetMetaIO.GetMetaPath(path);
            // var meta = await AssetMetaIO.ReadAsync(metadataPath, token).ConfigureAwait(false);
            // Logger.DebugAssert(meta != null, $"Missing or invalid metadata for asset at {path}");



            var header = new ImageContentHeader();
            sourceStream.ReadExactly(MemoryMarshal.AsBytes(new Span<ImageContentHeader>(ref header)));

            var imageDataSize = (int)(sourceStream.Length - sourceStream.Position);
            var imageData = new byte[imageDataSize];
            await sourceStream.ReadExactlyAsync(imageData, token).ConfigureAwait(false);

            var asset = new TextureAsset(imageData, header, id);
            return asset;
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to load texture asset: {ex.Message}");
        }
    }

    public ValueTask<Result> SaveAsync(Asset asset, Stream targetStream, IAssetRegistry assetRegistry, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public async ValueTask<Result> ImportAsync(Stream sourceStream, Stream targetStream, Guid id, IAssetSettings? settings, CancellationToken token = default)
    {
        try
        {
            var textureSettings = settings as TextureAssetSettings ?? new TextureAssetSettings();
            using var image = new MagickImage(sourceStream);
            var bytes = image.ToByteArray();

            await TextureProcessor.CompressToCacheAsync(EditorApplication.LibraryFolderPath, id, bytes, image.Width, image.Height, image.Depth, textureSettings, token)
                .ConfigureAwait(false);

            targetStream.Seek(0, SeekOrigin.Begin);

            var contentHeader = new ImageContentHeader
            {
                width = image.Width,
                height = image.Height,
                depth = image.Depth,
                colorComponents = image.ChannelCount
            };

            targetStream.Write(MemoryMarshal.AsBytes(new Span<ImageContentHeader>(ref contentHeader)));

            await targetStream.WriteAsync(bytes, token).ConfigureAwait(false);
            await targetStream.FlushAsync(token).ConfigureAwait(false);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to import texture asset: {ex.Message}");
        }
    }
}
