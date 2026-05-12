using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Graphics;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Ghost.Graphics.Utilities;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;
using System.Runtime.InteropServices;

namespace Ghost.Engine.Streaming;

[StructLayout(LayoutKind.Sequential, Size = 64)] // Leave extra space for future expansion without breaking compatibility
public struct TextureContentHeader
{
    public const uint MAGIC = 0x58455447; // GTEX
    public const uint VERSION = 1;

    public uint magic;
    public uint version;

    public uint width;
    public uint height;
    public uint bpc;
    public uint mipLevels;
    public uint dimension; // 1 for 1D, 2 for 2D, 3 for 3D
    public uint colorComponents;
}

internal partial class AssetManager
{
    public Handle<GPUTexture> ResolveTexture(Guid assetID)
    {
        if (assetID == Guid.Empty)
        {
            return Handle<GPUTexture>.Invalid;
        }

        var entry = GetOrCreateEntry(assetID);
        Logger.DebugAssert(entry.AssetType == AssetType.Texture);

        return ((TextureAssetEntry)entry).TextureHandle;
    }

    public int ReleaseTexture(Guid assetID)
    {
        if (assetID == Guid.Empty)
        {
            return 0;
        }

        if (!_entries.TryGetValue(assetID, out var entry) || entry.AssetType != AssetType.Texture)
        {
            return 0;
        }

        return entry.Release();
    }
}

internal unsafe class TextureAssetEntry : UploadableAssetEntry
{
    private MemoryBlock _rawData;

    private TextureDesc _desc;
    private byte* _pData;
    private nuint _dataSize;

    private Handle<GPUTexture> _actualHandle;
    private Handle<GPUTexture> _tempHandle;

    public Handle<GPUTexture> TextureHandle => _actualHandle;

    public TextureAssetEntry(AssetManager manager, IResourceDatabase resourceDatabase, ResourceManager resourceManager, Guid assetId, Guid[] dependencies)
        : base(manager, resourceDatabase, resourceManager, assetId, AssetType.Texture, dependencies)
    {
        _actualHandle = resourceDatabase.CreateEmpty().AsTexture();
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

    public override Result OnLoadRawData([OwnershipTransfer] ref MemoryBlock memory)
    {
        _rawData = memory;

        var pData = (byte*)memory.GetUnsafePtr();
        Logger.DebugAssert(pData != null);

        var reader = new BufferReader(pData, memory.Size);

        var header = reader.Read<TextureContentHeader>();

        if (header.magic != TextureContentHeader.MAGIC)
        {
            return Result.Failure($"Unexpected texture header {header.magic}.");
        }

        if (header.version != TextureContentHeader.VERSION)
        {
            return Result.Failure($"Unsupported header version {header.version}.");
        }

        var textureDesc = new TextureDesc
        {
            Width = header.width,
            Height = header.height,
            MipLevels = header.mipLevels,
            Slice = 1,
            Format = GetTextureFormat(header.bpc, header.colorComponents),
            Dimension = (TextureDimension)header.dimension,
            Usage = TextureUsage.ShaderResource,
        };

        _desc = textureDesc;
        _pData = reader.CurrentAddress;
        _dataSize = reader.RemainingBytes;

        return Result.Success();
    }

    public override Result OnRecordUploadCommands(ResourceStreamingContext context)
    {
        Logger.DebugAssert(_pData != null);

        var newHandle = RenderingUtility.CreateTexture(
            context.ResourceManager,
            context.ResourceDatabase,
            context.ResourceAllocator,
            context.CopyPipeline.GetCommandBuffer(),
            _pData,
            _dataSize,
            in _desc);

        if (newHandle.IsInvalid)
        {
            return Result.Failure("Failed to create GPU texture.");
        }

        _tempHandle = newHandle;
        return Result.Success();
    }

    public override void OnUploadComplete(ResourceStreamingContext context)
    {
        var actualHandle = context.ResourceDatabase.Replace(_actualHandle.AsResource(), _tempHandle.AsResource());
        Logger.DebugAssert(actualHandle.IsValid);

        context.CommandBuffer.Barrier(BarrierDesc.Texture(actualHandle, BarrierSync.AllShading, BarrierAccess.ShaderResource, BarrierLayout.ShaderResource));

        _actualHandle = Handle<GPUTexture>.Invalid;
        _tempHandle = actualHandle.AsTexture();

        _rawData.Dispose();
        _pData = null;
    }

    public override void OnReleaseResource()
    {
        ResourceDatabase.ReleaseResource(_tempHandle.AsResource());
    }
}
