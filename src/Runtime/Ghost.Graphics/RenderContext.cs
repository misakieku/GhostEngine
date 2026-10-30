using Ghost.Core;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.LowLevel.Utilities;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.InteropServices;

namespace Ghost.Graphics;

public readonly struct ResourceContext
{
    public int FrameBufferCount
    {
        get; init;
    }

    public ResourceManager ResourceManager
    {
        get; init;
    }

    public IResourceAllocator ResourceAllocator
    {
        get; init;
    }

    public IResourceDatabase ResourceDatabase
    {
        get; init;
    }

    public IPipelineLibrary PipelineLibrary
    {
        get; init;
    }

    internal ShaderLibrary ShaderLibrary
    {
        get; init;
    }
}


public readonly unsafe struct RenderContext
{
    public required FrameScheduler FrameScheduler
    {
        get; init;
    }

    public required ICommandAllocator CopyCommandAllocator
    {
        get; init;
    }

    public required ICommandAllocator GraphicsCommandAllocator
    {
        get; init;
    }

    public required ResourceManager ResourceManager
    {
        get; init;
    }

    public required IResourceDatabase ResourceDatabase
    {
        get; init;
    }

    public required IResourceAllocator ResourceAllocator
    {
        get; init;
    }

    public required IPipelineLibrary PipelineLibrary
    {
        get; init;
    }

    public required ShaderLibrary ShaderLibrary
    {
        get; init;
    }

    public required int FrameIndex
    {
        get; init;
    }

    public required int FrameBufferCount
    {
        get; init;
    }

    public ResourceContext ResourceContext => new ResourceContext
    {
        FrameBufferCount = FrameBufferCount,
        ResourceManager = ResourceManager,
        ResourceAllocator = ResourceAllocator,
        ResourceDatabase = ResourceDatabase,
        PipelineLibrary = PipelineLibrary,
        ShaderLibrary = ShaderLibrary,
    };

    public void UploadBuffer<T>(ICommandBuffer commandBuffer, Handle<GPUBuffer> buffer, params ReadOnlySpan<T> data)
        where T : unmanaged
    {
        Logger.DebugAssert(commandBuffer.Type != CommandBufferType.Compute, "UploadBuffer can not be called from a compute command buffer.");

        var r = ResourceDatabase.GetResourceDescription(buffer.AsResource());
        if (r.IsFailure)
        {
            Logger.Error($"Failed to get resource description for buffer {buffer}. Error: {r.Error}");
            return;
        }

        Logger.DebugAssert(r.Value.Type == ResourceType.Buffer);

        var sizeInBytes = (nuint)(data.Length * sizeof(T));
        var heapType = r.Value.BufferDescriptor.HeapType;

        if (heapType == HeapType.Upload)
        {
            fixed (T* pData = data)
            {
                var mappedData = ResourceDatabase.MapResource(buffer.AsResource(), 0, null);
                MemoryUtility.MemCpy(mappedData, pData, sizeInBytes);
                ResourceDatabase.UnmapResource(buffer.AsResource(), 0, null);
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

            var uploadHandle = ResourceManager.CreateTransientUploadBuffer(uploadDesc, out var srcOffset);

            fixed (T* pData = data)
            {
                var mappedData = ResourceDatabase.MapResource(uploadHandle.AsResource(), 0, null);
                MemoryUtility.MemCpy((byte*)mappedData + srcOffset, pData, sizeInBytes);
                ResourceDatabase.UnmapResource(uploadHandle.AsResource(), 0, null);
            }

            commandBuffer.CopyBuffer(buffer, uploadHandle, 0, srcOffset, sizeInBytes);
        }
    }

    /// <summary>
    /// Uploads a sub-range of data into an existing GPU buffer at the specified byte offset.
    /// Used for incremental uploads (e.g. dirty palette ranges).
    /// </summary>
    public void UploadBufferRange<T>(ICommandBuffer commandBuffer, Handle<GPUBuffer> buffer, ReadOnlySpan<T> data, uint byteOffset)
        where T : unmanaged
    {
        Logger.DebugAssert(commandBuffer.Type != CommandBufferType.Compute, "UploadBuffer can not be called from a compute command buffer.");

        if (data.IsEmpty)
        {
            return;
        }

        var sizeInBytes = (nuint)(data.Length * sizeof(T));
        var uploadDesc = new BufferDesc
        {
            Size = sizeInBytes,
            Usage = BufferUsage.Upload,
            HeapType = HeapType.Upload,
        };

        var uploadHandle = ResourceManager.CreateTransientUploadBuffer(uploadDesc, out var srcOffset);

        fixed (T* pData = data)
        {
            var mappedData = ResourceDatabase.MapResource(uploadHandle.AsResource(), 0, null);
            MemoryUtility.MemCpy((byte*)mappedData + srcOffset, pData, sizeInBytes);
            ResourceDatabase.UnmapResource(uploadHandle.AsResource(), 0, null);
        }

        commandBuffer.CopyBuffer(buffer, uploadHandle, byteOffset, srcOffset, sizeInBytes);
    }

    public void DispatchCompute<T>(ICommandBuffer commandBuffer, Handle<ComputeShader> compute, int entryIndex, scoped in T property, uint3 threadGroupCount)
        where T : unmanaged
    {
        Logger.DebugAssert(commandBuffer.Type != CommandBufferType.Copy, "UploadBuffer can not be called from a copy command buffer.");

        ref var shader = ref ResourceManager.GetComputeShaderReference(compute).GetValueOrThrow();

        var (compiledHash, error) = ShaderLibrary.GetCompiledHash(shader.UniqueID, entryIndex);
        if (error.IsFailure)
        {
            Logger.Debug($"No compiled shader found for compute shader {shader.UniqueID} with entry point {entryIndex}.");
            return;
        }

        var passId = shader.GetEntryID(entryIndex);
        var pipelineKey = RHIUtility.CreateComputePipelineKey(passId, compiledHash);

        if (!PipelineLibrary.HasPipelineStateObject(pipelineKey))
        {
            var compiledCacheResult = ShaderLibrary.GetCompiledCache(shader.UniqueID, entryIndex);
            if (compiledCacheResult.IsFailure)
            {
                Logger.Warning($"Failed to load compiled shader cache for compute pipeline {pipelineKey}. Skipping compute dispatch.");
                return;
            }

            var cache = compiledCacheResult.Value;
            Logger.DebugAssert(cache.compiledHash == compiledHash);

            ShaderLibrary.ParseCacheData(cache.byteCode, out _, out var byteCodeOffsets, out var byteCodes);
            Logger.DebugAssert(byteCodeOffsets.Length == 1);

            var psoDes = new ComputePSODesc
            {
                CompiledHash = compiledHash,
                PassId = passId,

                CsCode = byteCodes.Slice((int)byteCodeOffsets[0]),
            };

            PipelineLibrary.CreateComputePipeline(in psoDes).GetValueOrThrow();
        }

        commandBuffer.SetPipelineState(pipelineKey);

        var propertySpan = MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in property));
        var propertyBufferDesc = new BufferDesc
        {
            Size = (uint)propertySpan.Length,
            Stride = 256,
            Usage = BufferUsage.Upload,
            HeapType = HeapType.Upload,
        };

        var properyBuffer = ResourceManager.CreateTransientUploadBuffer(in propertyBufferDesc, 256, out var offset);

        var mappedData = ResourceDatabase.MapResource(properyBuffer.AsResource(), 0, null);
        Logger.DebugAssert(mappedData != null, "Failed to map property buffer.");

        fixed (byte* pData = propertySpan)
        {
            MemoryUtility.MemCpy((byte*)mappedData + offset, pData, (nuint)propertySpan.Length);
        }

        error = ResourceDatabase.UnmapResource(properyBuffer.AsResource(), 0, null);
        Logger.DebugAssert(error.IsSuccess, $"Failed to unmap property buffer: {error}.");

        commandBuffer.SetConstantBufferView(RootSignatureLayout.PASS_PROPERTIES_CBV_SLOT, properyBuffer, offset);
        commandBuffer.DispatchCompute(threadGroupCount.x, threadGroupCount.y, threadGroupCount.z);
    }
}