using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Graphics.Core;

internal struct CBufferCache : IResourceReleasable
{
    private UnsafeArray<byte> _cpuData;
    private Handle<GPUBuffer> _gpuResource;
    private uint _size;

    public readonly UnsafeArray<byte> CpuData => _cpuData;
    public readonly Handle<GPUBuffer> GpuResource => _gpuResource;
    public readonly uint Size => _size;

    public readonly bool IsCreated => _size != 0 && _gpuResource.IsValid && _cpuData.IsCreated;

    public CBufferCache(Handle<GPUBuffer> buffer, uint bufferSize)
    {
        _size = bufferSize;
        _cpuData = new UnsafeArray<byte>((int)bufferSize, AllocationHandle.Persistent);
        _gpuResource = buffer;
    }

    public void ReleaseResource(IResourceDatabase database)
    {
        if (!IsCreated)
        {
            return;
        }

        _cpuData.Dispose();
        database.ReleaseResource(_gpuResource.AsResource());

        _gpuResource = Handle<GPUBuffer>.Invalid;
        _size = 0;
    }
}

public struct Material : IResourceReleasable
{
    private struct PipelineOverride
    {
        public Key64<ShaderPass> shaderPass;
        public PipelineState options;
    }

    private Handle<Shader> _shader;
    private UnsafeArray<PipelineOverride> _passPipelineOverride;
    private bool _isDirty;

    // TODO: We should have a global material buffer cache to avoid creating a new buffer for each material. This is a temporary solution.
    internal CBufferCache _cBufferCache;

    public readonly Handle<Shader> Shader => _shader;
    public readonly bool IsDirty => _isDirty;

    /// <summary>
    /// Active pass index for the material. This is used to determine which pass to use when rendering the material.
    /// </summary>
    public int ActivePassIndex
    {
        get; set;
    }

    /// <summary>
    /// Dense runtime variant index in the shader variant registry.
    /// </summary>
    public uint VariantIndex
    {
        get; set;
    }

    /// <summary>
    /// Sets the shader for the material and initializes the property buffer and pipeline overrides based on the shader's passes.
    /// </summary>
    /// <param name="shade">The handle of the shader to set.</param>
    /// <param name="resourceManager">The resource manager to use.</param>
    /// <param name="resourceDatabase">The resource database to use.</param>
    /// <param name="resourceAllocator">The resource allocator to use.</param>
    /// <returns>The error code indicating the result of the operation.</returns>
    public Error SetShader(Handle<Shader> shade, ResourceManager resourceManager, IResourceDatabase resourceDatabase, IResourceAllocator resourceAllocator)
    {
        if (!shade.IsValid)
        {
            return Error.InvalidArgument;
        }

        _cBufferCache.ReleaseResource(resourceDatabase);
        _shader = shade;

        var r = resourceManager.GetShaderReference(shade);
        if (r.IsFailure)
        {
            return r.Error;
        }

        ref var shader = ref r.Value;
        VariantIndex = shader.VariantIndex;
        if (_passPipelineOverride.Count < shader.PassCount)
        {
            if (!_passPipelineOverride.IsCreated)
            {
                _passPipelineOverride = new UnsafeArray<PipelineOverride>(shader.PassCount, AllocationHandle.Persistent);
            }
            else
            {
                _passPipelineOverride.Resize(shader.PassCount);
            }
        }

        for (var i = 0; i < shader.PassCount; i++)
        {
            ref readonly var pass = ref shader.GetPassReference(i);
            _passPipelineOverride[i] = new PipelineOverride
            {
                shaderPass = pass.Key,
                options = pass.DefaultState,
            };
        }

        if (shader.PropertyBufferSize != 0)
        {
            var desc = new BufferDesc
            {
                Size = shader.PropertyBufferSize,
                Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
                HeapType = HeapType.Default,
            };

            var buffer = resourceAllocator.CreateBuffer(desc, "MaterialCBuffer");
            _cBufferCache = new CBufferCache(buffer, shader.PropertyBufferSize);
        }

        return Error.None;
    }

    /// <summary>
    /// Gets the property cache of the material as a struct of type T.
    /// </summary>
    /// <typeparam name="T">The type of the property cache.</typeparam>
    /// <returns>The property cache as a struct of type T. <see cref="Error.InvalidArgument"/> if the size of T does not match the size of the property cache.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly unsafe Result<T, Error> GetPropertyCache<T>()
        where T : unmanaged
    {
        if (sizeof(T) != _cBufferCache.Size)
        {
            return Error.InvalidArgument;
        }

        return *(T*)_cBufferCache.CpuData.GetUnsafePtr();
    }

    /// <summary>
    /// Gets the raw property cache of the material as a ReadOnlySpan of bytes.
    /// </summary>
    /// <returns>The raw property cache as a ReadOnlySpan of bytes.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlySpan<byte> GetRawPropertyCache()
    {
        if (_cBufferCache.Size == 0)
        {
            return Span<byte>.Empty;
        }

        return _cBufferCache.CpuData.AsSpan(0, (int)_cBufferCache.Size);
    }

    /// <summary>
    /// Sets the property cache of the material with a struct of type T. The size of T must match the size of the property cache.
    /// </summary>
    /// <typeparam name="T">The type of the property cache.</typeparam>
    /// <param name="data">The data to set.</param>
    /// <returns>The error code indicating the result of the operation.</returns>
    public unsafe Error SetPropertyCache<T>(scoped in T data)
        where T : unmanaged
    {
        if (sizeof(T) != _cBufferCache.Size)
        {
            return Error.InvalidArgument;
        }

        var dataSpan = MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in data));
        return SetRawPropertyCache(dataSpan);
    }

    /// <summary>
    /// Sets the raw property cache of the material with a ReadOnlySpan of bytes. The length of the span must match the size of the property cache.
    /// </summary>
    /// <param name="data">The data to set.</param>
    /// <returns>The error code indicating the result of the operation.</returns>
    public Error SetRawPropertyCache(ReadOnlySpan<byte> data)
    {
        if (data.Length != _cBufferCache.Size)
        {
            return Error.InvalidArgument;
        }

        var cacheSpan = _cBufferCache.CpuData.AsSpan();
        if (cacheSpan.SequenceEqual(data))
        {
            return Error.None;
        }

        data.CopyTo(cacheSpan);
        _isDirty = true;

        return Error.None;
    }

    /// <summary>
    /// Gets the pipeline state override for a specific pass index. If no override is set, it returns the default pipeline state for that pass.
    /// </summary>
    /// <param name="passIndex">The index of the pass for which to get the pipeline state override.</param>
    /// <returns>The pipeline state override for the specified pass, or the default pipeline state if no override is set.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly PipelineState GetPassPipelineOverride(int passIndex)
    {
        return _passPipelineOverride[passIndex].options;
    }

    /// <summary>
    /// Sets the pipeline state override for a specific pass index. This allows customization of the rendering behavior for that pass.
    /// </summary>
    /// <param name="passIndex">The index of the pass for which to set the pipeline state override.</param>
    /// <param name="options">The pipeline state options to set.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPassPipelineOverride(int passIndex, scoped in PipelineState options)
    {
        ref var pipelineOverride = ref _passPipelineOverride[passIndex];
        pipelineOverride.options = options;
        _isDirty = true;
    }

    internal readonly void UploadData(RenderContext ctx)
    {
        var cbufferResource = _cBufferCache.GpuResource;
        var desc = BarrierDesc.Buffer(
            cbufferResource,
            BarrierSync.AllShading,
            BarrierSync.Copy,
            BarrierAccess.ShaderResource,
            BarrierAccess.CopyDest);
        ctx.CommandBuffer.Barrier(desc);
        ctx.UploadBuffer(_cBufferCache.GpuResource, _cBufferCache.CpuData.AsSpan());

        desc = BarrierDesc.Buffer(
            cbufferResource,
            BarrierSync.Copy,
            BarrierSync.AllShading,
            BarrierAccess.CopyDest,
            BarrierAccess.ShaderResource);
        ctx.CommandBuffer.Barrier(desc);
    }

    public void ReleaseResource(IResourceDatabase database)
    {
        _cBufferCache.ReleaseResource(database);
        _passPipelineOverride.Dispose();
    }
}
