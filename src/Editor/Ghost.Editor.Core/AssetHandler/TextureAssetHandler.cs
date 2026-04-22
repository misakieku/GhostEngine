using Ghost.Core;
using Ghost.Engine;
using Ghost.Graphics.RHI;
using ImageMagick;
using Misaki.HighPerformance.LowLevel;
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

[Guid(GUID)]
public class TextureAsset : IAsset
{
    public const string GUID = "27965FFF-860C-40EF-9123-1874D7DE9CDC";

    private static readonly Guid s_typeID = Guid.Parse(GUID);

    private readonly Guid _id;
    private readonly IAssetSettings _settings;

    private readonly MagickImage _textureData;
    private readonly uint _width;
    private readonly uint _height;
    private readonly uint _depth;
    private readonly uint _colorComponents;
    private readonly uint _dimension;

    public Guid ID => _id;
    public Guid TypeID => s_typeID;
    public IAssetSettings Settings => _settings;

    public MagickImage TextureData => _textureData;
    public uint Width => _width;
    public uint Height => _height;
    public uint Depth => _depth;
    public uint Dimension => _dimension;
    public uint ColorComponents => _colorComponents;

    internal TextureAsset([OwnershipTransfer] MagickImage data, TextureContentHeader header, Guid id, IAssetSettings settings)
    {
        _id = id;
        _settings = settings;

        _textureData = data;
        _width = header.width;
        _height = header.height;
        _depth = header.depth;
        _dimension = header.dimension;
        _colorComponents = header.colorComponents;
    }

    ~TextureAsset()
    {
        Dispose();
    }

    public void Dispose()
    {
        _textureData.Dispose();
        GC.SuppressFinalize(this);
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

        public int Depth
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

[CustomAssetHandler(TextureAsset.GUID, [".png", ".jpg", ".jpeg", ".tga", ".bmp", ".hdr"], 1)]
internal class TextureAssetHandler : IAssetHandler
{
    public AssetType TargetAssetType => AssetType.Texture;

    public IAssetSettings? CreateDefaultSettings()
    {
        return new TextureAssetSettings();
    }

    private static TextureDimension GetTextureDimension(TextureAssetSettings settings)
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

        // If none of the above conditions are met, we will treat it as a regular 2D texture.
        return TextureDimension.Texture2D;
    }

    public ValueTask<Result<IAsset>> LoadAssetAsync(Stream assetStream, Guid id, IAssetSettings? settings, CancellationToken token = default)
    {
        try
        {
            var image = new MagickImage(assetStream);

            var textureSettings = settings as TextureAssetSettings ?? new TextureAssetSettings();
            var contentHeader = new TextureContentHeader
            {
                width = image.Width,
                height = image.Height,
                depth = image.Depth,
                colorComponents = image.ChannelCount,
                dimension = (uint)GetTextureDimension(textureSettings)
            };

            return ValueTask.FromResult(Result.Success<IAsset>(new TextureAsset(image, contentHeader, id, textureSettings)));
        }
        catch (Exception ex)
        {
            return ValueTask.FromResult(Result<IAsset>.Failure(ex.Message));
        }
    }

    public async ValueTask<Result> SaveAssetAsync(Stream targetStream, IAsset asset, CancellationToken token = default)
    {
        if (asset is not TextureAsset textureAsset)
        {
            return Result.Failure("Asset type is not TextureAsset");
        }

        try
        {
            await textureAsset.TextureData.WriteAsync(targetStream, token);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    public async ValueTask<Result> ImportAsync(Stream sourceStream, Stream targetStream, Guid id, IAssetSettings? settings, CancellationToken token = default)
    {
        try
        {
            using var image = new MagickImage(sourceStream);
            var pixels = image.GetPixelsUnsafe().GetAreaPointer(0, 0, image.Width, image.Height);
            if (pixels == 0)
            {
                return Result.Failure("Failed to retrieve pixel data from the source image.");
            }

            var textureSettings = settings as TextureAssetSettings ?? new TextureAssetSettings();
            var (path, mip) = await TextureProcessor.CompressToCacheAsync(EditorApplication.CacheFolderPath, id, pixels, image.Width, image.Height, image.Depth, textureSettings, token)
                .ConfigureAwait(false);

            targetStream.Seek(0, SeekOrigin.Begin);

            var contentHeader = new TextureContentHeader
            {
                width = image.Width,
                height = image.Height,
                depth = image.Depth,
                colorComponents = image.ChannelCount,
                mipLevels = (uint)mip,
                dimension = (uint)GetTextureDimension(textureSettings)
            };

            targetStream.Write(MemoryMarshal.AsBytes(new Span<TextureContentHeader>(ref contentHeader)));

            await using var ddsStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            await ddsStream.CopyToAsync(targetStream, token).ConfigureAwait(false);
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
