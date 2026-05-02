using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Graphics;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Utilities;
using System.Runtime.InteropServices;

namespace Ghost.Engine;

[StructLayout(LayoutKind.Sequential, Size = 64)] // Leave extra space for future expansion without breaking compatibility
public struct TextureContentHeader
{
    public uint width;
    public uint height;
    public uint bpc;
    public uint mipLevels;
    public uint dimension; // 1 for 1D, 2 for 2D, 3 for 3D
    public uint colorComponents;
}

internal partial class AssetEntry
{
    private unsafe class TextureData
    {
        public TextureDesc desc;
        public TextureContentHeader header;
        public byte* pData;
        public nuint dataSize;
    }

    private static void RegisterTextureCallback()
    {
        s_onCreation[(int)AssetType.Texture] = static (e) =>
        {
            // This will create a new slot in the database, but not allocation any GPU resource.
            // Everything in the slot will have the same value as the fallback texture, expect the slot will be marked as shared.
            var handle = e._resourceDatabase.CreateEmpty().AsTexture();
            e.SetStorage(handle);
        };

        s_onParseRawData[(int)AssetType.Texture] = static (e) => e.ParseTextureData();
        s_onRecordUpload[(int)AssetType.Texture] = static (e, ctx) => e.RecordTextureUpload(ctx);
        s_onUploadComplete[(int)AssetType.Texture] = static (e, ctx) => e.OnTextureUploadComplete(ctx);
        s_onReleaseResource[(int)AssetType.Texture] = static (e) =>
        {
            var handle = e.GetStorage<Handle<GPUTexture>>();
            e._resourceDatabase.ReleaseResource(handle.AsResource());
        };
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

    private unsafe Result ParseTextureData()
    {
        var pData = (byte*)_rawData.GetUnsafePtr();
        Logger.DebugAssert(pData != null);

        var reader = new BufferReader(pData, _rawData.Size);

        var header = reader.Read<TextureContentHeader>();
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

        // Will the gc be fine here?
        var textureData = new TextureData
        {
            desc = textureDesc,
            header = header,
            pData = reader.CurrentAddress,
            dataSize = reader.RemainingBytes,
        };

        _parsedObject = textureData;

        return Result.Success();
    }

    private unsafe Result RecordTextureUpload(ResourceStreamingContext context)
    {
        var textureData = _parsedObject as TextureData;
        Logger.DebugAssert(textureData != null);

        var newHandle = RenderingUtility.CreateTexture(
            context.ResourceManager,
            context.ResourceDatabase,
            context.ResourceAllocator,
            context.CopyPipeline.GetCommandBuffer(),
            textureData.pData,
            textureData.dataSize,
            in textureData.desc);

        if (newHandle.IsInvalid)
        {
            return Result.Failure("Failed to create GPU texture.");
        }

        var oldHandle = GetStorage<Handle<GPUTexture>>();
        SetStorage((oldHandle, newHandle));

        return Result.Success();
    }

    private void OnTextureUploadComplete(ResourceStreamingContext context)
    {
        var (oldHandle, newHandle) = GetStorage<(Handle<GPUTexture>, Handle<GPUTexture>)>();
        var actualHandle = context.ResourceDatabase.Replace(oldHandle.AsResource(), newHandle.AsResource());

        context.GraphicsCommandBuffer.Barrier(BarrierDesc.Texture(oldHandle.AsResource(), BarrierSync.AllShading, BarrierAccess.ShaderResource, BarrierLayout.ShaderResource));

        SetStorage((actualHandle, Handle<GPUTexture>.Invalid));

        _rawData.Dispose();
        _parsedObject = null;
    }
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

        return entry.GetStorage<Handle<GPUTexture>>();
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
