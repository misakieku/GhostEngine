using Ghost.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Utilities;
using Ghost.Graphics.Services;

namespace Ghost.Graphics.Utilities;

public static unsafe class RenderingUtility
{
    public static void UploadBuffer<T>(ResourceManager resourceManager, IResourceDatabase resourceDatabase, ICommandBuffer cmd, Handle<GPUBuffer> buffer, params ReadOnlySpan<T> data)
        where T : unmanaged
    {
        var r = resourceDatabase.GetResourceDescription(buffer.AsResource());
        if (r.IsFailure)
        {
            return;
        }

        Logger.DebugAssert(r.Value.Type == ResourceType.Buffer);

        var sizeInBytes = (nuint)(data.Length * sizeof(T));
        var memoryType = r.Value.BufferDescriptor.HeapType;

        if (memoryType == HeapType.Upload)
        {
            fixed (T* pData = data)
            {
                var mappedData = resourceDatabase.MapResource(buffer.AsResource(), 0, null);
                MemoryUtility.MemCpy(mappedData, pData, sizeInBytes);
                resourceDatabase.UnmapResource(buffer.AsResource(), 0, null);
            }
        }
        else
        {
            var uploadDesc = new BufferDesc
            {
                Size = sizeInBytes,
                Usage = BufferUsage.Upload,
                HeapType = HeapType.Upload,
            };

            var uploadHandle = resourceManager.CreateTransientBuffer(in uploadDesc);
            if (uploadHandle.IsInvalid)
            {
                throw new OutOfMemoryException("Failed to create upload buffer for buffer data.");
            }

            fixed (T* pData = data)
            {
                var mappedData = resourceDatabase.MapResource(uploadHandle.AsResource(), 0, null);
                MemoryUtility.MemCpy(mappedData, pData, sizeInBytes);
                resourceDatabase.UnmapResource(uploadHandle.AsResource(), 0, null);
            }

            cmd.CopyBuffer(buffer, uploadHandle, 0, 0, sizeInBytes);
        }
    }

    public static void UploadTexture<T>(ResourceManager resourceManager, IResourceDatabase resourceDatabase, ICommandBuffer cmd, Handle<GPUTexture> texture, ReadOnlySpan<T> data)
        where T : unmanaged
    {
        var desc = resourceDatabase.GetResourceDescription(texture.AsResource()).GetValueOrThrow();
        desc.TextureDescriptor.Format.GetSurfaceInfo(desc.TextureDescriptor.Width, desc.TextureDescriptor.Height, out var rowPitch, out var slicePitch, out _);

        var requiredSize = resourceDatabase.GetIntermediateResourceSize(texture.AsResource(), 0, 1);
        var uploadDesc = new BufferDesc
        {
            Size = requiredSize,
            Usage = BufferUsage.Upload,
            HeapType = HeapType.Upload,
        };

        var uploadHandle = resourceManager.CreateTransientBuffer(in uploadDesc);
        if (uploadHandle.IsInvalid)
        {
            throw new OutOfMemoryException("Failed to create upload buffer for texture data.");
        }

        cmd.Barrier(BarrierDesc.Texture(texture.AsResource(), BarrierSync.Copy, BarrierAccess.CopyDest, BarrierLayout.CopyDest));

        fixed (T* pData = data)
        {
            var subresourceData = new SubResourceData
            {
                pData = pData,
                rowPitch = rowPitch,
                slicePitch = slicePitch
            };

            cmd.UpdateSubResources(texture.AsResource(), uploadHandle.AsResource(), subresourceData);
        }
    }
}
