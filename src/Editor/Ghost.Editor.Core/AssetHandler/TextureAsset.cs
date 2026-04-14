using Ghost.Core;
using Ghost.Editor.Core.Contracts;
using Ghost.Graphics.RHI;
using ImageMagick;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ghost.Editor.Core.AssetHandler.TextureAssetSettings;

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

    private readonly Handle<GPUTexture> _texture;

    public override Guid TypeID => s_typeGuid;
    public Handle<GPUTexture> Texture => _texture;

    public TextureAsset(Guid id, Guid[] dependencies, IAssetSettings? settings, Handle<GPUTexture> texture)
        : base(id, dependencies, settings)
    {
        _texture = texture;
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

[CustomAssetHandler(ID = TextureAsset._TYPE_ID, SupportedExtensions = new[] { ".png", ".jpg", ".jpeg", ".tga", ".bmp", ".hdr" })]
internal class TextureAssetHandler : IImportableAssetHandler
{
    private const int _CURRENT_VERSION = 1;

    private struct ImageContentHeader
    {
        public uint width;
        public uint height;
        public uint depth;
        public uint colorComponents;
    }

    private static async ValueTask<Result<long>> WriteSettingsToStreamAsync(TextureAssetSettings settings, Stream stream, CancellationToken token = default)
    {
        var size = Unsafe.SizeOf<BasicSettings>() + Unsafe.SizeOf<AdvancedSettings>() + Unsafe.SizeOf<SamplerSettings>();
        var tempArray = ArrayPool<byte>.Shared.Rent(size);

        try
        {
            ref var address = ref MemoryMarshal.GetReference(tempArray);
            Unsafe.WriteUnaligned(ref address, settings.Basic);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref address, Unsafe.SizeOf<BasicSettings>()), settings.Advanced);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref address, Unsafe.SizeOf<BasicSettings>() + Unsafe.SizeOf<AdvancedSettings>()), settings.Sampler);

            await stream.WriteAsync(tempArray.AsMemory(0, size), token).ConfigureAwait(false);

            return Result.Success<long>(size);
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to write texture asset settings to stream: {ex.Message}");
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(tempArray);
        }
    }

    private static async ValueTask<Result<IAssetSettings>> ReadSettingsFromStreamAsync(Stream stream, CancellationToken token = default)
    {
        var size = Unsafe.SizeOf<BasicSettings>() + Unsafe.SizeOf<AdvancedSettings>() + Unsafe.SizeOf<SamplerSettings>();
        var tempArray = ArrayPool<byte>.Shared.Rent(size);

        try
        {
            await stream.ReadAsync(tempArray.AsMemory(0, size), token).ConfigureAwait(false);

            // Use index-based reads after the await to avoid 'ref across await' errors.
            var basic = Unsafe.ReadUnaligned<BasicSettings>(ref tempArray[0]);
            var advanced = Unsafe.ReadUnaligned<AdvancedSettings>(ref tempArray[Unsafe.SizeOf<BasicSettings>()]);
            var sampler = Unsafe.ReadUnaligned<SamplerSettings>(ref tempArray[Unsafe.SizeOf<BasicSettings>() + Unsafe.SizeOf<AdvancedSettings>()]);

            var settings = new TextureAssetSettings
            {
                Basic = basic,
                Advanced = advanced,
                Sampler = sampler
            };

            return Result.Success<IAssetSettings>(settings);
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to read texture asset settings from stream: {ex.Message}");
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(tempArray);
        }
    }

    public ValueTask<Result> ExportAsync(Stream assetStream, Stream targetStream, IAssetExportOptions? options, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public async ValueTask<Result> ImportAsync(Stream sourceStream, Stream targetStream, Guid id, CancellationToken token = default)
    {
        using var image = new MagickImage(sourceStream);
        var bytes = image.ToByteArray();

        var settings = new TextureAssetSettings();
        await TextureProcessor.CompressToCacheAsync(EditorApplication.LibraryFolderPath, id, bytes, image.Width, image.Height, image.Depth, settings, token).ConfigureAwait(false);

        var header = new AssetMetadata(id, TextureAsset.s_typeGuid)
        {
            HandlerVersion = _CURRENT_VERSION,
            SettingsOffset = AssetMetadata.SIZE,
        };

        targetStream.Seek(header.SettingsOffset, SeekOrigin.Begin);
        var sizeResult = await WriteSettingsToStreamAsync(settings, targetStream, token).ConfigureAwait(false);
        if (sizeResult.IsFailure)
        {
            return Result.Failure($"Failed to write texture asset settings: {sizeResult.Message}");
        }

        // Content layout (all little-endian):
        //   uint32     width
        //   uint32     height
        //   uint32     depth
        //   uint32     colorComponents
        //   byte[]     pixelBytes

        header.SettingsSize = sizeResult.Value;
        header.ContentOffset = header.SettingsOffset + sizeResult.Value;
        unsafe
        {
            header.ContentSize = sizeof(ImageContentHeader) + image.Width * image.Height * (image.Depth / 8) * image.ChannelCount;
        }

        // Write raw image content
        targetStream.Seek(header.ContentOffset, SeekOrigin.Begin);

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

        // Patch header now that all sizes are known
        targetStream.Seek(0, SeekOrigin.Begin);
        AssetMetadata.WriteToStream(targetStream, ref header);

        return Result.Success();
    }

    public ValueTask<Result<Asset>> LoadAsync(Stream sourceStream, IAssetRegistry assetRegistry, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<Result> SaveAsync(Asset asset, Stream targetStream, IAssetRegistry assetRegistry, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
}
