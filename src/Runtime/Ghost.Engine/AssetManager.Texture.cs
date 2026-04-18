using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Engine.AssetLoader;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Utilities;
using Misaki.HighPerformance.LowLevel.Buffer;

namespace Ghost.Engine;

public partial class AssetManager
{
    private partial class AssetEntry
    {
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

        private unsafe Result RecordTextureUpload(ICommandBuffer commandBuffer)
        {
            var pData = (byte*)_rawData.GetUnsafePtr();
            var reader = new BufferReader(pData, _rawData.Size);

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
                ResourceManager,
                ResourceDatabase,
                ResourceAllocator,
                commandBuffer,
                reader.CurrentAddress,
                reader.RemainingBytes,
                in textureDesc);

            if (newHandle.IsInvalid)
            {
                return Result.Failure("Failed to create GPU texture.");
            }

            var oldHandle = GetStorage<Handle<GPUTexture>>();
            SetStorage((oldHandle, newHandle));

            return Result.Success();
        }

        private void OnTextureUploadComplete()
        {
            var (oldHandle, newHandle) = GetStorage<(Handle<GPUTexture>, Handle<GPUTexture>)>();

            ResourceDatabase.Swap(oldHandle.AsResource(), newHandle.AsResource());
            ResourceDatabase.ReleaseResource(newHandle.AsResource()); // releases old fallback slot

            SetStorage((oldHandle, Handle<GPUTexture>.Invalid)); // Old handle is now the new handle, and the old fallback slot is released. Use Invalid handle to clear second slot.

            _rawData.Dispose();
            _rawData = default;
        }
    }

    private Handle<GPUTexture> AllocateTextureHandle()
    {
        // This will create a new slot in the database, but not allocation any GPU resource.
        // Everything in the slot will have the same value as the fallback texture, expect the slot will be marked as shared.
        return _resourceDatabase.CreateShared(_fallbackTexture.AsResource()).AsTexture();
    }

    public Handle<GPUTexture> ResolveTexture(Guid assetID)
    {
        if (assetID == Guid.Empty)
        {
            return _fallbackTexture;
        }

        var entry = GetOrCreateEntry(assetID);
        Logger.DebugAssert(entry.AssetType == AssetType.Texture);

        return entry.GetStorage<Handle<GPUTexture>>();
    }
}
