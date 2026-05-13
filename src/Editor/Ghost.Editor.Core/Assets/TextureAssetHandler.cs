using Ghost.Core;
using Ghost.Engine;
using Ghost.Engine.Streaming;
using Ghost.Graphics.RHI;
using Ghost.StbI;
using Misaki.HighPerformance.LowLevel;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Editor.Core.Assets;

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
public unsafe class TextureAsset : IAsset
{
    public const string GUID = "27965FFF-860C-40EF-9123-1874D7DE9CDC";

    private static readonly Guid s_typeID = Guid.Parse(GUID);

    private readonly Guid _id;
    private readonly IAssetSettings _settings;

    private readonly IntPtr _textureData;
    private readonly uint _width;
    private readonly uint _height;
    private readonly uint _depth;
    private readonly uint _colorComponents;
    private readonly uint _dimension;

    public Guid ID => _id;
    public Guid TypeID => typeof(TextureAsset).GUID;
    public IAssetSettings Settings => _settings;

    public IntPtr TextureData => _textureData;
    public uint Width => _width;
    public uint Height => _height;
    public uint Depth => _depth;
    public uint Dimension => _dimension;
    public uint ColorComponents => _colorComponents;

    internal TextureAsset([OwnershipTransfer] IntPtr data, TextureContentHeader header, Guid id, IAssetSettings settings)
    {
        _id = id;
        _settings = settings;

        _textureData = data;
        _width = header.width;
        _height = header.height;
        _depth = header.bpc;
        _dimension = header.dimension;
        _colorComponents = header.colorComponents;
    }

    ~TextureAsset()
    {
        Dispose();
    }

    public void Dispose()
    {
        StbIApi.ImageFree((void*)_textureData);
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

[CustomAssetHandler(AssetTypeId = TextureAsset.GUID, RuntimeAssetType = AssetType.Texture, Extensions = new[] { ".png", ".jpg", ".jpeg", ".tga", ".bmp", ".hdr" })]
internal class TextureAssetHandler : IImportableAssetHandler, IPackableAssetHandler
{
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

    public IAssetSettings? CreateDefaultSettings(string ext)
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

    private static unsafe Result<TextureInfo> GetImageInfo(string sourcePath, TextureAssetSettings settings)
    {
        using var mmf = MemoryMappedFile.CreateFromFile(sourcePath, FileMode.Open);
        using var accessor = mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

        byte* ptr = null;

        try
        {
            var ext = Path.GetExtension(sourcePath);
            var isHDR = ext.Equals(".hdr", StringComparison.OrdinalIgnoreCase) || settings.Basic.TextureShape == TextureShape.TextureCube;

            accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);

            int imageWidth, imageHeight, bitsPerChannel, colorComponents;

            var bufferSpan = new ReadOnlySpan<byte>(ptr, (int)accessor.Capacity);
            bitsPerChannel = StbIApi.Is16BitFromMemory(bufferSpan) > 0 ? 16 : 8;

            void* pPixels;
            if (isHDR || bitsPerChannel > 8)
            {
                pPixels = StbIApi.LoadfFromMemory(bufferSpan, &imageWidth, &imageHeight, &colorComponents, 4);
            }
            else
            {
                pPixels = StbIApi.LoadFromMemory(bufferSpan, &imageWidth, &imageHeight, &colorComponents, 4);
            }

            return new TextureInfo
            {
                pixelData = (IntPtr)pPixels,
                width = imageWidth,
                height = imageHeight,
                depth = 1,
                bitsPerChannel = bitsPerChannel,
                colorComponents = 4, // We forced req_comp to 4
                isHDR = isHDR,
            };
        }
        catch (Exception ex)
        {
            return Result<TextureInfo>.Failure($"Failed to get image info: {ex.Message}");
        }
        finally
        {
            if (ptr != null)
            {
                accessor.SafeMemoryMappedViewHandle.ReleasePointer();
            }
        }
    }

    public ValueTask<Result<IAsset>> LoadAssetAsync(string assetPath, Guid id, IAssetSettings? settings, CancellationToken token = default)
    {
        try
        {
            var textureSettings = settings as TextureAssetSettings ?? new TextureAssetSettings();
            var infoResult = GetImageInfo(assetPath, textureSettings);
            if (infoResult.IsFailure)
            {
                return ValueTask.FromResult(Result<IAsset>.Failure(infoResult.Message));
            }

            var info = infoResult.Value;
            var contentHeader = new TextureContentHeader
            {
                width = (uint)info.width,
                height = (uint)info.height,
                bpc = (uint)info.bitsPerChannel,
                colorComponents = (uint)info.colorComponents,
                dimension = (uint)GetTextureDimension(textureSettings),
            };

            return ValueTask.FromResult(Result.Success<IAsset>(new TextureAsset(info.pixelData, contentHeader, id, textureSettings)));
        }
        catch (Exception ex)
        {
            return ValueTask.FromResult(Result<IAsset>.Failure(ex.Message));
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe void WriteCallback(void* context, void* data, int size)
    {
        var stream = (Stream)GCHandle.FromIntPtr((IntPtr)context).Target!;
        var buffer = new ReadOnlySpan<byte>(data, size);
        stream.Write(buffer);
    }

    public async ValueTask<Result> SaveAssetAsync(string targetPath, IAsset asset, CancellationToken token = default)
    {
        if (asset is not TextureAsset textureAsset)
        {
            return Result.Failure("Asset type is not TextureAsset");
        }

        await using var targetStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
        return await Task.Run(() =>
        {
            // It will be safe here to pass the gc handle to c because c will not use it, c will only pass it back to c# in the callback, and we will free the handle after the write operation is done.
            var gcHandle = GCHandle.Alloc(targetStream, GCHandleType.Normal);

            try
            {
                var ext = Path.GetExtension(targetStream.Name);
                var result = 0;

                unsafe
                {
                    switch (ext)
                    {
                        case ".png":
                            result = StbIApi.WritePngToFunc(&WriteCallback, (void*)GCHandle.ToIntPtr(gcHandle), (int)textureAsset.Width, (int)textureAsset.Height, (int)textureAsset.ColorComponents, (void*)textureAsset.TextureData, 0);
                            break;

                        case ".jpg":
                            result = StbIApi.WriteJpgToFunc(&WriteCallback, (void*)GCHandle.ToIntPtr(gcHandle), (int)textureAsset.Width, (int)textureAsset.Height, (int)textureAsset.ColorComponents, (void*)textureAsset.TextureData, 90);
                            break;

                        // TODO: Add support for other image formats

                        default:
                            return Result.Failure($"Unsupported image format: {ext}");
                    }
                }

                return result != 0 ? Result.Success() : Result.Failure("Failed to write image data.");
            }
            catch (Exception ex)
            {
                return Result.Failure(ex.Message);
            }
            finally
            {
                gcHandle.Free();
            }
        }, token).ConfigureAwait(false);
    }

    public async ValueTask<Result<ImportedSubAsset[]>> ImportAsync(string sourcePath, string targetPath, Guid id, IAssetSettings? settings, CancellationToken token = default)
    {
        if (!File.Exists(sourcePath))
        {
            return Result.Failure("Source file does not exist.");
        }

        try
        {
            var textureSettings = settings as TextureAssetSettings ?? new TextureAssetSettings();
            var infoResult = GetImageInfo(sourcePath, textureSettings);
            if (!infoResult.IsSuccess)
            {
                return Result.Failure(infoResult.Message);
            }

            var info = infoResult.Value;
            var result = await TextureProcessor.GenerateMipAndCompressAsync(EditorApplication.CacheFolderPath, id,
                info,
                textureSettings, token)
            .ConfigureAwait(false);

            if (result.IsFailure)
            {
                return Result.Failure(result.Message);
            }

            var (cachePath, mip) = result.Value;

            await using var targetStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
            var header = new TextureContentHeader
            {
                width = (uint)info.width,
                height = (uint)info.height,
                bpc = (uint)info.bitsPerChannel,
                colorComponents = (uint)info.colorComponents,
                mipLevels = (uint)mip,
                dimension = (uint)GetTextureDimension(textureSettings)
            };

            targetStream.Write(MemoryMarshal.AsBytes(new Span<TextureContentHeader>(ref header)));

            await using var ddsStream = new FileStream(cachePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            await ddsStream.CopyToAsync(targetStream, token).ConfigureAwait(false);
            await targetStream.FlushAsync(token).ConfigureAwait(false);

            return Result.Success(Array.Empty<ImportedSubAsset>());
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to import texture asset: {ex.Message}");
        }
    }

    public ValueTask<Result> PackAsync(string assetPath, MemoryStream targetStream, CancellationToken token = default)
    {
        return ValueTask.FromResult(Result.Failure("Packing texture assets is not supported yet."));
    }
}
