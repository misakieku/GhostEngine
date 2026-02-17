using Ghost.Core;
using Ghost.Editor.Core.Contracts;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Image;
using System.Buffers;
using System.Runtime.CompilerServices;
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

public enum TextureCompressionEffort : uint
{
    Fastest,
    Normal,
    Production
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

    public override Guid TypeID => s_typeGuid;

    public TextureAsset(Guid id, Guid[] dependencies, IAssetSettings? settings)
        : base(id, dependencies, settings)
    {
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

        public TextureCompressionEffort CompressionEffort
        {
            get; set;
        } = TextureCompressionEffort.Normal;

        public bool UseBorderColor
        {
            get; set;
        } = false;

        public Color32 BorderColor
        {
            get; set;
        } = new Color32(0, 0, 0, 0);

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

    public async ValueTask<Result<long>> WriteToStreamAsync(Stream stream, CancellationToken token = default)
    {
        var size = Unsafe.SizeOf<BasicSettings>() + Unsafe.SizeOf<AdvancedSettings>() + Unsafe.SizeOf<SamplerSettings>();
        var tempArray = ArrayPool<byte>.Shared.Rent(size);

        try
        {
            ref byte address = ref MemoryMarshal.GetReference(tempArray);
            Unsafe.WriteUnaligned(ref address, Basic);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref address, Unsafe.SizeOf<BasicSettings>()), Advanced);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref address, Unsafe.SizeOf<BasicSettings>() + Unsafe.SizeOf<AdvancedSettings>()), Sampler);

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

    public async ValueTask<Result<IAssetSettings>> ReadFromStreamAsync(Stream stream, CancellationToken token = default)
    {
        var size = Unsafe.SizeOf<BasicSettings>() + Unsafe.SizeOf<AdvancedSettings>() + Unsafe.SizeOf<SamplerSettings>();
        var tempArray = ArrayPool<byte>.Shared.Rent(size);

        try
        {
            ref byte address = ref MemoryMarshal.GetReference(tempArray);
            await stream.ReadAsync(tempArray.AsMemory(0, size), token).ConfigureAwait(false);
            var basic = Unsafe.ReadUnaligned<BasicSettings>(ref address);
            var advanced = Unsafe.ReadUnaligned<AdvancedSettings>(ref Unsafe.Add(ref address, Unsafe.SizeOf<BasicSettings>()));
            var sampler = Unsafe.ReadUnaligned<SamplerSettings>(ref Unsafe.Add(ref address, Unsafe.SizeOf<BasicSettings>() + Unsafe.SizeOf<AdvancedSettings>()));

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
}

internal class TextureAssetHandler : IImportableAssetHandler
{
    private const int _CURRENT_VERSION = 1;

    public ValueTask<Result> ExportAsync(Stream assetStream, Stream targetStream, IAssetExportOptions? options, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public async ValueTask<Result> ImportAsync(Stream sourceStream, Stream targetStream, Guid id, CancellationToken token = default)
    {
        var info = ImageInfo.FromStream(sourceStream);
        if (info.BitsPerChannel <= 0)
        {
            return Result.Failure($"Unsupported image format with {info.BitsPerChannel} bits per channel.");
        }

        ref byte pData = ref Unsafe.NullRef<byte>();
        var imageSize = 0ul;
        var isFloat = info.BitsPerChannel > 8;

        if (isFloat)
        {
            using var image = ImageResultFloat.FromStream(sourceStream, info.ColorComponents);
            pData = ref MemoryMarshal.GetReference(MemoryMarshal.AsBytes(image.AsSpan()));
            imageSize = image.Size;
        }
        else
        {
            using var image = ImageResult.FromStream(sourceStream, info.ColorComponents);
            pData = ref MemoryMarshal.GetReference(MemoryMarshal.AsBytes(image.AsSpan()));
            imageSize = image.Size;
        }

        var header = new AssetMetadata(id, TextureAsset.s_typeGuid)
        {
            HandlerVersion = _CURRENT_VERSION,
            SettingsOffset = AssetMetadata.SIZE,
        };

        targetStream.Seek(0, SeekOrigin.Begin);
        AssetMetadata.WriteToStream(targetStream, ref header);

        targetStream.Seek(header.SettingsOffset, SeekOrigin.Begin);
        var settings = new TextureAssetSettings();
        var sizeResult = await settings.WriteToStreamAsync(targetStream, token).ConfigureAwait(false);
        if (sizeResult.IsFailure)
        {
            return Result.Failure($"Failed to write texture asset settings: {sizeResult.Message}");
        }

        header.SettingsSize = sizeResult.Value;
        header.ContentOffset = header.SettingsOffset + sizeResult.Value;
        header.ContentSize = (long)imageSize;

        targetStream.Seek(header.ContentOffset, SeekOrigin.Begin);

        var offset = 0;
        var tempArray = ArrayPool<byte>.Shared.Rent((int)Math.Min(imageSize, 40960ul));
        var remaining = imageSize;

        try
        {
            while (remaining > 0)
            {
                var chunkSize = (int)Math.Min(remaining, (ulong)tempArray.Length);
                Unsafe.CopyBlockUnaligned(ref tempArray[0], ref Unsafe.Add(ref pData, offset), (uint)chunkSize);
                
                await targetStream.WriteAsync(tempArray.AsMemory(0, chunkSize), token).ConfigureAwait(false);

                offset += chunkSize;
                remaining -= (ulong)chunkSize;
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to write texture asset content to stream: {ex.Message}");
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(tempArray);
        }
    }

    public ValueTask<Result<Asset>> LoadAsync(Stream sourceStream, IAssetRegistry assetDatabase, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<Result> SaveAsync(Asset asset, Stream targetStream, IAssetRegistry assetDatabase, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
}