using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Engine.AssetLoader;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Utilities;

namespace Ghost.Engine;

public partial class AssetManager
{
    private Handle<GPUTexture> AllocateTextureHandle()
    {
        // This will create a new slot in the database, but not allocation any GPU resource.
        // Everything in the slot will have the same value as the fallback texture, expect the slot will be marked as shared.
        return _resourceDatabase.CreateShared(_fallbackTexture.AsResource()).AsTexture();
    }

    private static TextureFormat GetTextureFormat(uint depth, uint colorComponents)
    {
        return colorComponents switch
        {
            1 => depth switch
            {
                8 => TextureFormat.R8_UNorm,
                16 => TextureFormat.R16_UNorm,
                32 => TextureFormat.R32_UInt,
                _ => TextureFormat.Unknown,
            },
            2 => depth switch
            {
                8 => TextureFormat.R8G8_UNorm,
                16 => TextureFormat.R16G16_UNorm,
                32 => TextureFormat.R32G32_Float,
                _ => TextureFormat.Unknown,
            },
            3 or 4 => depth switch
            {
                8 => TextureFormat.R8G8B8A8_UNorm,
                16 => TextureFormat.R16G16B16A16_Float,
                32 => TextureFormat.R32G32B32A32_Float,
                _ => TextureFormat.Unknown,
            },
            _ => TextureFormat.Unknown,
        };
    }

    private unsafe Result UploadTexture(AssetEntry entry)
    {
        var pData = (byte*)entry.rawData.GetUnsafePtr();
        var reader = new BufferReader(pData, entry.rawData.Size);

        var header = reader.Read<TextureContentHeader>();

        var textureDesc = new TextureDesc
        {
            Width = header.width,
            Height = header.height,
            MipLevels = header.mipLevels,
            Slice = 1,
            Format = GetTextureFormat(header.depth, header.colorComponents),
            Dimension = (TextureDimension)header.dimension,
            Usage = TextureUsage.ShaderResource,
        };

        var newHandle = RenderingUtility.CreateTexture(
            _resourceManager,
            _resourceDatabase,
            _resourceAllocator,
            _uploadedBatch.CommandBuffer,
            reader.Position,
            in textureDesc);

        if (newHandle.IsInvalid)
        {
            return Result.Failure("Failed to create GPU texture.");
        }

        // FIX: We can not Swap right now, we must wait on the GPU to finish the upload.
        var oldHandle = entry.GetStorage<Handle<GPUTexture>>();
        _resourceDatabase.Swap(oldHandle.AsResource(), newHandle.AsResource());
        // Release the new handle since it now contains the old handle's resource.
        // Because the old handle is shared, it will only release the slot in the database, not the actuall GPU resource, which is the fallback texture in this case.
        _resourceDatabase.ReleaseResource(newHandle.AsResource());

        return Result.Success();
    }

    public Handle<GPUTexture> ResolveTexture(Guid assetID)
    {
        if (assetID == Guid.Empty)
        {
            return _fallbackTexture;
        }

        var entry = GetOrCreateEntry(assetID);
        Logger.DebugAssert(entry.assetType == AssetType.Texture);

        return entry.GetStorage<Handle<GPUTexture>>();
    }
}
