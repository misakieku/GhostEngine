using Ghost.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.LowLevel.Utilities;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.InteropServices;

namespace Ghost.Graphics.Core;

public readonly struct ResourceContext
{
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

public sealed unsafe class RenderContext
{
    private const ulong UPLOAD_BUFFER_SIZE = 64 * 1024 * 1024; // 64 MB

    private ICommandBuffer? _commandBuffer;

    public ICommandBuffer CommandBuffer
    {
        get
        {
            Logger.DebugAssert(_commandBuffer != null, "Command buffer is not set. Call BeginFrame() before using the render context.");
            return _commandBuffer;
        }
    }

    public ResourceManager ResourceManager
    {
        get;
    }

    public IResourceAllocator ResourceAllocator
    {
        get;
    }

    public IResourceDatabase ResourceDatabase
    {
        get;
    }

    public IPipelineLibrary PipelineLibrary
    {
        get;
    }

    internal ShaderLibrary ShaderLibrary
    {
        get;
    }

    public ResourceContext ResourceContext => new ResourceContext
    {
        ResourceManager = ResourceManager,
        ResourceAllocator = ResourceAllocator,
        ResourceDatabase = ResourceDatabase,
        PipelineLibrary = PipelineLibrary,
        ShaderLibrary = ShaderLibrary,
    };

    internal RenderContext(ResourceManager resourceManager, IResourceAllocator resourceAllocator, IResourceDatabase resourceDatabase, IPipelineLibrary pipelineLibrary, ShaderLibrary shaderLibrary)
    {
        ResourceManager = resourceManager;
        ResourceAllocator = resourceAllocator;
        ResourceDatabase = resourceDatabase;
        PipelineLibrary = pipelineLibrary;
        ShaderLibrary = shaderLibrary;
    }

    internal void BeginFrame(ICommandBuffer commandBuffer)
    {
        _commandBuffer = commandBuffer;
    }

    public void UploadBuffer<T>(Handle<GPUBuffer> buffer, params ReadOnlySpan<T> data)
        where T : unmanaged
    {
        var r = ResourceDatabase.GetResourceDescription(buffer.AsResource());
        if (r.IsFailure)
        {
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

            CommandBuffer.CopyBuffer(buffer, uploadHandle, 0, srcOffset, sizeInBytes);
        }
    }

    /// <summary>
    /// Uploads a sub-range of data into an existing GPU buffer at the specified byte offset.
    /// Used for incremental uploads (e.g. dirty palette ranges).
    /// </summary>
    public void UploadBufferRange<T>(Handle<GPUBuffer> buffer, ReadOnlySpan<T> data, uint byteOffset)
        where T : unmanaged
    {
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

        CommandBuffer.CopyBuffer(buffer, uploadHandle, byteOffset, srcOffset, sizeInBytes);
    }

    public void DispatchCompute<T>(Handle<ComputeShader> compute, int entryIndex, scoped in T property, uint3 threadGroupCount)
        where T : unmanaged
    {
        ref var shader = ref ResourceManager.GetComputeShaderReference(compute).GetValueOrThrow();

        // TODO: Refactor this into a helper method.
        var (compiledHash, error) = ShaderLibrary.GetCompiledHash(shader.UniqueID, entryIndex);
        if (error.IsFailure)
        {
            // TODO: Fallback to an error material.
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

        CommandBuffer.SetPipelineState(pipelineKey);

        var propertySpan = MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in property));
        // TODO: Placed resource has 64k alignment requirement, which can waste lots of memory. We can allocate a large buffer and slice it for each dispatch to avoid this issue.
        var propertyBufferDesc = new BufferDesc
        {
            Size = (uint)propertySpan.Length,
            Stride = (uint)sizeof(T),
            Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
            HeapType = HeapType.Upload,
        };
        var properyBuffer = ResourceManager.CreateTransientBuffer(in propertyBufferDesc);

        var mappedData = ResourceDatabase.MapResource(properyBuffer.AsResource(), 0, null);
        Logger.DebugAssert(mappedData != null, "Failed to map property buffer.");

        fixed (byte* pData = propertySpan)
        {
            MemoryUtility.MemCpy(mappedData, pData, (nuint)propertySpan.Length);
        }

        error = ResourceDatabase.UnmapResource(properyBuffer.AsResource(), 0, null);
        Logger.DebugAssert(error.IsSuccess, $"Failed to unmap property buffer: {error}.");

        var pushConstant = new PushConstantsData
        {
            propertyBuffer = ResourceDatabase.GetBindlessIndex(properyBuffer.AsResource()),
        };

        CommandBuffer.SetComputeRoot32Constants(RootSignatureLayout.PUSH_CONSTANT_SLOT, pushConstant.AsUInts());
        CommandBuffer.DispatchCompute(threadGroupCount.x, threadGroupCount.y, threadGroupCount.z);
    }
}
