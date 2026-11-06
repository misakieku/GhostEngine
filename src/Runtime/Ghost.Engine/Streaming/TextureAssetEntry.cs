using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Graphics;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Ghost.Graphics.Utilities;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Engine.Streaming;

// How can we handle virtual texture streaming?
internal unsafe class TextureAssetEntry : AssetEntry, ILoadableAssetEntry, IUploadableAssetEntry
{
    private Handle<GPUTexture> _actualHandle;
    private Handle<GPUTexture> _tempHandle;

    private TextureDesc _desc;
    private AssetPayload _payload;
    private uint _dataOffset;

    public TextureAssetEntry(AssetManager manager, IResourceDatabase resourceDatabase, ResourceManager resourceManager, Guid assetId, Guid[] dependencies, string assetName)
        : base(manager, resourceDatabase, resourceManager, assetId, AssetType.Texture, dependencies, assetName)
    {
        _actualHandle = resourceDatabase.CreateEmpty(true, assetName).AsTexture();
    }

    private static TextureFormat GetTextureFormat(uint bpc, uint colorComponents)
    {
        return colorComponents switch
        {
            1 => bpc switch
            {
                8 => TextureFormat.R8_UNorm,
                16 => TextureFormat.R16_UNorm,
                32 => TextureFormat.R32_UInt,
                _ => TextureFormat.Unknown,
            },
            2 => bpc switch
            {
                8 => TextureFormat.R8G8_UNorm,
                16 => TextureFormat.R16G16_UNorm,
                32 => TextureFormat.R32G32_Float,
                _ => TextureFormat.Unknown,
            },
            3 or 4 => bpc switch
            {
                8 => TextureFormat.R8G8B8A8_UNorm,
                16 => TextureFormat.R16G16B16A16_Float,
                32 => TextureFormat.R32G32B32A32_Float,
                _ => TextureFormat.Unknown,
            },
            _ => TextureFormat.Unknown,
        };
    }

    public override void ReadAssetData(Span<byte> dst)
    {
        Logger.DebugAssert(dst.Length == sizeof(Handle<GPUTexture>));
        Logger.DebugAssert(_actualHandle.IsValid);

        ref var address = ref MemoryMarshal.GetReference(dst);
        Unsafe.WriteUnaligned(ref address, _actualHandle);
    }

    public override void ReadAssetData<T>(ref T dst)
    {
        Logger.DebugAssert(typeof(T) == typeof(Handle<GPUTexture>));
        Logger.DebugAssert(_actualHandle.IsValid);

        dst = Unsafe.BitCast<Handle<GPUTexture>, T>(_actualHandle);
    }

    protected override void OnReleaseResource()
    {
        ResourceDatabase.ReleaseResource(_actualHandle.AsResource());
        if (_tempHandle.IsValid)
        {
            ResourceDatabase.ReleaseResource(_tempHandle.AsResource());
        }
        _payload.Dispose();
    }

    public Result OnLoadContent([Owner] AssetPayload payload)
    {
        try
        {
            if (payload.Size < sizeof(TextureContentHeader))
            {
                payload.Dispose();
                return Result.Failure("Texture payload is too small to contain a header.");
            }

            var header = payload.Read<TextureContentHeader>(0);

            if (header.magic != TextureContentHeader.MAGIC)
            {
                payload.Dispose();
                return Result.Failure($"Unexpected texture header {header.magic}.");
            }

            if (header.version != TextureContentHeader.VERSION)
            {
                payload.Dispose();
                return Result.Failure($"Unsupported header version {header.version}.");
            }

            var ddsSliceLength = (int)Math.Min(0x7fffffffL, payload.Size - sizeof(TextureContentHeader));
            var ddsSpan = payload.Slice(sizeof(TextureContentHeader), ddsSliceLength);
            if (DDSUtility.TryParseDdsHeader(ddsSpan, out var ddsHeader, out var dxt10, out var dataOffset))
            {
                _desc = TextureDesc.FromDDSHeader(ddsHeader, dxt10);
                _desc.Usage = TextureUsage.ShaderResource;
                _dataOffset = dataOffset;
            }
            else
            {
                _dataOffset = 0;
                _desc = new TextureDesc
                {
                    Width = header.width,
                    Height = header.height,
                    MipLevels = header.mipLevels,
                    Slice = header.dimension == TextureDimension.TextureCube ? 6u : 1u,
                    Format = GetTextureFormat(header.bpc, header.colorComponents),
                    Dimension = header.dimension,
                    Usage = TextureUsage.ShaderResource,
                };
            }

            _payload = payload;

            return Result.Success();
        }
        catch (Exception)
        {
            payload.Dispose();
            throw;
        }
    }

    public Result OnRecordUploadCommands(scoped in RenderContext context, ICommandBuffer copyCommandBuffer)
    {
        var dataSize = _payload.Size - _dataOffset - sizeof(TextureContentHeader);
        var pData = _payload.Pointer + sizeof(TextureContentHeader) + _dataOffset;

        var newHandle = ResourceUtility.CreateTexture(
            context.ResourceManager,
            context.ResourceDatabase,
            context.ResourceAllocator,
            copyCommandBuffer,
            pData,
            (nuint)dataSize,
            in _desc);

        if (newHandle.IsInvalid)
        {
            return Result.Failure("Failed to create GPU texture.");
        }

        _tempHandle = newHandle;

        return Result.Success();
    }

    public void OnUploadComplete(scoped in RenderContext context, ICommandBuffer commandBuffer)
    {
        try
        {
            var actualHandle = context.ResourceDatabase.Replace(_actualHandle.AsResource(), _tempHandle.AsResource());
            Logger.DebugAssert(actualHandle.IsValid);

            commandBuffer.Barrier(BarrierDesc.Texture(actualHandle.AsTexture(), BarrierSync.None, BarrierSync.AllShading, BarrierAccess.Common, BarrierAccess.ShaderResource, BarrierLayout.Common, BarrierLayout.ShaderResource));

            _actualHandle = actualHandle.AsTexture();
            _tempHandle = Handle<GPUTexture>.Invalid;
        }
        finally
        {
            _payload.Dispose();
        }
    }
}
