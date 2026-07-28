using Ghost.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.LowLevel.Utilities;

namespace Ghost.Graphics.Utilities;

public static unsafe class RenderingUtility
{
    public static Error UploadBuffer(ResourceManager resourceManager, IResourceDatabase resourceDatabase, ICommandBuffer cmd, Handle<GPUBuffer> buffer, void* pData, nuint sizeInBytes)
    {
        var (desc, error) = resourceDatabase.GetResourceDescription(buffer.AsResource());
        if (error.IsFailure)
        {
            return error;
        }

        Logger.DebugAssert(desc.Type == ResourceType.Buffer);

        var memoryType = desc.BufferDescriptor.HeapType;

        if (memoryType == HeapType.Upload)
        {
            var mappedData = resourceDatabase.MapResource(buffer.AsResource(), 0, null);
            MemoryUtility.MemCpy(mappedData, pData, sizeInBytes);
            resourceDatabase.UnmapResource(buffer.AsResource(), 0, null);
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
                return Error.OutOfMemory;
            }

            var mappedData = resourceDatabase.MapResource(uploadHandle.AsResource(), 0, null);
            MemoryUtility.MemCpy(mappedData, pData, sizeInBytes);
            resourceDatabase.UnmapResource(uploadHandle.AsResource(), 0, null);

            cmd.Barrier(BarrierDesc.Buffer(buffer, BarrierSync.Copy, BarrierAccess.CopyDest));
            cmd.CopyBuffer(buffer, uploadHandle, 0, 0, sizeInBytes);
            cmd.Barrier(BarrierDesc.Buffer(buffer, BarrierSync.None, BarrierAccess.Common));
        }

        return Error.None;
    }

    public static Error UploadBuffer<T>(ResourceManager resourceManager, IResourceDatabase resourceDatabase, ICommandBuffer cmd, Handle<GPUBuffer> buffer, params ReadOnlySpan<T> data)
        where T : unmanaged
    {
        fixed (T* pData = data)
        {
            return UploadBuffer(resourceManager, resourceDatabase, cmd, buffer, pData, (nuint)(data.Length * sizeof(T)));
        }
    }

    public static Handle<GPUBuffer> CreateBuffer(ResourceManager resourceManager, IResourceDatabase resourceDatabase, IResourceAllocator resourceAllocator, ICommandBuffer cmd, void* pData, nuint sizeInBytes, scoped in BufferDesc desc, string? name = null)
    {
        var error = Error.UnknownError;
        var bufferHandle = resourceAllocator.CreateBuffer(in desc, name);

        if (!bufferHandle.IsInvalid)
        {
            error = UploadBuffer(resourceManager, resourceDatabase, cmd, bufferHandle, pData, sizeInBytes);
        }

        if (error.IsSuccess)
        {
            return bufferHandle;
        }

        Logger.DebugAssert(error.IsSuccess);
        return Handle<GPUBuffer>.Invalid;
    }

    public static Handle<GPUBuffer> CreateBuffer<T>(ResourceManager resourceManager, IResourceAllocator resourceAllocator, IResourceDatabase resourceDatabase, ICommandBuffer cmd, ReadOnlySpan<T> data, scoped in BufferDesc desc, string? name = null)
        where T : unmanaged
    {
        fixed (T* pData = data)
        {
            return CreateBuffer(resourceManager, resourceDatabase, resourceAllocator, cmd, pData, (nuint)(data.Length * sizeof(T)), in desc, name);
        }
    }

    public static Error UploadTexture(ResourceManager resourceManager, IResourceDatabase resourceDatabase, ICommandBuffer cmd, Handle<GPUTexture> texture, void* pData, nuint sizeInBytes)
    {
        var (desc, error) = resourceDatabase.GetResourceDescription(texture.AsResource());
        if (error.IsFailure)
        {
            return error;
        }

        desc.TextureDescriptor.Format.GetSurfaceInfo(desc.TextureDescriptor.Width, desc.TextureDescriptor.Height, out var rowPitch, out var slicePitch, out _);

        var requiredSize = resourceDatabase.GetIntermediateResourceSize(texture.AsResource(), 0, 1);
        if (sizeInBytes < requiredSize)
        {
            return Error.InvalidArgument;
        }

        var uploadDesc = new BufferDesc
        {
            Size = requiredSize,
            Usage = BufferUsage.Upload,
            HeapType = HeapType.Upload,
        };

        var uploadHandle = resourceManager.CreateTransientBuffer(in uploadDesc);
        if (uploadHandle.IsInvalid)
        {
            return Error.OutOfMemory;
        }

        cmd.Barrier(BarrierDesc.Texture(texture, BarrierSync.Copy, BarrierAccess.CopyDest, BarrierLayout.CopyDest));

        var subresourceData = new SubResourceData
        {
            pData = pData,
            rowPitch = rowPitch,
            slicePitch = slicePitch
        };

        cmd.UpdateSubResources(texture.AsResource(), uploadHandle.AsResource(), subresourceData);
        cmd.Barrier(BarrierDesc.Texture(texture, BarrierSync.None, BarrierAccess.Common, BarrierLayout.Common));

        return Error.None;
    }

    public static Error UploadTexture<T>(ResourceManager resourceManager, IResourceDatabase resourceDatabase, ICommandBuffer cmd, Handle<GPUTexture> texture, ReadOnlySpan<T> data)
        where T : unmanaged
    {
        fixed (T* pData = data)
        {
            return UploadTexture(resourceManager, resourceDatabase, cmd, texture, pData, (nuint)(data.Length * sizeof(T)));
        }
    }

    public static Handle<GPUTexture> CreateTexture(ResourceManager resourceManager, IResourceDatabase resourceDatabase, IResourceAllocator resourceAllocator, ICommandBuffer cmd, void* pData, nuint sizeInBytes, scoped in TextureDesc desc, string? name = null)
    {
        var error = Error.UnknownError;

        var textureHandle = resourceAllocator.CreateTexture(in desc, name);
        if (!textureHandle.IsInvalid)
        {
            error = UploadTexture(resourceManager, resourceDatabase, cmd, textureHandle, pData, sizeInBytes);
        }

        if (error.IsSuccess)
        {
            return textureHandle;
        }

        Logger.DebugAssert(error.IsSuccess);
        return Handle<GPUTexture>.Invalid;
    }

    public static Handle<GPUTexture> CreateTexture<T>(ResourceManager resourceManager, IResourceDatabase resourceDatabase, IResourceAllocator resourceAllocator, ICommandBuffer cmd, ReadOnlySpan<T> data, scoped in TextureDesc desc, string? name = null)
        where T : unmanaged
    {
        fixed (T* pData = data)
        {
            return CreateTexture(resourceManager, resourceDatabase, resourceAllocator, cmd, pData, (nuint)(data.Length * sizeof(T)), in desc, name);
        }
    }
}
