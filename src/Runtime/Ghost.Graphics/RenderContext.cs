using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.LowLevel.Utilities;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.InteropServices;

namespace Ghost.Graphics;

internal readonly ref struct GraphicsPipelineResolveDesc
{
    public ulong ShaderId { get; init; }
    public int PassIndex { get; init; }
    public PipelineState PipelineOption { get; init; }
    public ReadOnlySpan<TextureFormat> RtvFormats { get; init; }
    public TextureFormat DsvFormat { get; init; }
    public KeywordMask128 ActiveKeywords { get; init; }
}

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

    public required SwapChainManager SwapChainManager
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
        SetPassProperties(commandBuffer, in property);
        commandBuffer.DispatchCompute(threadGroupCount.x, threadGroupCount.y, threadGroupCount.z);
    }

    public void SetPassProperties<T>(ICommandBuffer commandBuffer, scoped in T property) where T : unmanaged
    {
        var propertySpan = MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in property));
        var propertyBufferDesc = new BufferDesc
        {
            Size = (uint)propertySpan.Length,
            Stride = 256,
            Usage = BufferUsage.Upload,
            HeapType = HeapType.Upload,
        };

        var propertyBuffer = ResourceManager.CreateTransientUploadBuffer(in propertyBufferDesc, 256, out var offset);
        var mappedData = ResourceDatabase.MapResource(propertyBuffer.AsResource(), 0, null);
        Logger.DebugAssert(mappedData != null, "Failed to map property buffer.");

        fixed (byte* pData = propertySpan)
        {
            MemoryUtility.MemCpy((byte*)mappedData + offset, pData, (nuint)propertySpan.Length);
        }

        var error = ResourceDatabase.UnmapResource(propertyBuffer.AsResource(), 0, null);
        Logger.DebugAssert(error.IsSuccess, $"Failed to unmap property buffer: {error}.");

        commandBuffer.SetConstantBufferView(RootSignatureLayout.PASS_PROPERTIES_CBV_SLOT, propertyBuffer, offset);
    }

    public bool TrySetActiveGraphicsShaderPass(ICommandBuffer commandBuffer, Handle<Shader> shaderHandle, int passIndex, ReadOnlySpan<TextureFormat> rtvFormats, TextureFormat dsvFormat = TextureFormat.Unknown, KeywordMask128 activeKeywords = default)
    {
        if (passIndex < 0)
        {
            return false;
        }

        var shaderResult = ResourceManager.GetShaderReference(shaderHandle);
        if (shaderResult.IsFailure)
        {
            return false;
        }

        ref readonly var shader = ref shaderResult.Value;
        if ((uint)passIndex >= (uint)shader.PassCount)
        {
            return false;
        }

        ref readonly var pass = ref shader.GetPassReference(passIndex);
        var resolveDesc = new GraphicsPipelineResolveDesc
        {
            ShaderId = shader.UniqueID,
            PassIndex = passIndex,
            PipelineOption = pass.DefaultState,
            RtvFormats = rtvFormats,
            DsvFormat = dsvFormat,
            ActiveKeywords = activeKeywords,
        };

        if (!TryResolveGraphicsPipeline(ShaderLibrary, PipelineLibrary, in pass, in resolveDesc, out var pipelineKey))
        {
            return false;
        }

        commandBuffer.SetPipelineState(pipelineKey);
        return true;
    }

    internal static bool TryResolveGraphicsPipeline(ShaderLibrary shaderLibrary, IPipelineLibrary pipelineLibrary, scoped in ShaderPass pass, scoped in GraphicsPipelineResolveDesc desc, out Key128<PipelineState> pipelineKey)
    {
        pipelineKey = default;
        const ShaderStageMask requiredStages = ShaderStageMask.Mesh;
        if ((pass.StageMask & ShaderStageMask.Compute) != 0 || (pass.StageMask & requiredStages) != requiredStages)
        {
            Logger.Warning($"Shader pass 0x{pass.Key.Value:X16} does not contain a valid graphics stage topology.");
            return false;
        }

        var (compiledHash, error) = shaderLibrary.GetCompiledHash(desc.ShaderId, desc.PassIndex, desc.ActiveKeywords);
        if (error.IsFailure)
        {
            return false;
        }

        var passAttachmentHash = new PassAttachmentHash(desc.RtvFormats, desc.DsvFormat);
        pipelineKey = RHIUtility.CreateGraphicsPipelineKey(pass.Key.Value, compiledHash, desc.PipelineOption, passAttachmentHash);
        if (pipelineLibrary.HasPipelineStateObject(pipelineKey))
        {
            return true;
        }

        var compiledCacheResult = shaderLibrary.GetCompiledCache(desc.ShaderId, desc.PassIndex, desc.ActiveKeywords);
        if (compiledCacheResult.IsFailure)
        {
            Logger.Warning($"Failed to load compiled shader cache for graphics pipeline {pipelineKey}. Skipping draw call.");
            pipelineKey = default;
            return false;
        }

        var cache = compiledCacheResult.Value;
        Logger.DebugAssert(cache.compiledHash == compiledHash);
        ShaderLibrary.ParseCacheData(cache.byteCode, out _, out var byteCodeOffsets, out var byteCodes);

        var hasAmplification = (pass.StageMask & ShaderStageMask.Amplification) != 0;
        var hasPixel = (pass.StageMask & ShaderStageMask.Pixel) != 0;
        var expectedByteCodeCount = (hasAmplification ? 1 : 0) + 1 + (hasPixel ? 1 : 0);
        if (byteCodeOffsets.Length != expectedByteCodeCount)
        {
            Logger.Warning($"Shader pass 0x{pass.Key.Value:X16} has {byteCodeOffsets.Length} bytecode entries, expected {expectedByteCodeCount}. Skipping draw call.");
            pipelineKey = default;
            return false;
        }

        var byteCodeIndex = 0;
        var asByteCode = ReadOnlySpan<byte>.Empty;
        if (hasAmplification)
        {
            asByteCode = byteCodes.Slice((int)byteCodeOffsets[0], (int)(byteCodeOffsets[1] - byteCodeOffsets[0]));
            byteCodeIndex++;
        }

        var msStart = (int)byteCodeOffsets[byteCodeIndex];
        var msByteCode = byteCodes.Slice(msStart, hasPixel ? (int)(byteCodeOffsets[byteCodeIndex + 1] - byteCodeOffsets[byteCodeIndex]) : byteCodes.Length - msStart);
        byteCodeIndex++;

        var psByteCode = ReadOnlySpan<byte>.Empty;
        if (hasPixel)
        {
            psByteCode = byteCodes.Slice((int)byteCodeOffsets[byteCodeIndex]);
        }

        var psoDesc = new GraphicsPSODesc
        {
            CompiledHash = compiledHash,
            PassId = pass.Key.Value,
            PipelineOption = desc.PipelineOption,
            RtvFormats = desc.RtvFormats,
            DsvFormat = desc.DsvFormat,
            AsCode = asByteCode,
            MsCode = msByteCode,
            PsCode = psByteCode,
        };

        var createResult = pipelineLibrary.CreateGraphicsPipeline(in psoDesc);
        if (createResult.IsFailure)
        {
            Logger.Warning($"Failed to create graphics pipeline {pipelineKey}: {createResult.Message}");
            pipelineKey = default;
            return false;
        }

        return true;
    }
}