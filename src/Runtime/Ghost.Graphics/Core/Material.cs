using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Graphics.Core;

internal struct CBufferCache : IResourceReleasable
{
    private UnsafeArray<byte> _cpuData;
    private Handle<GraphicsBuffer> _gpuResource;
    private uint _size;

    public readonly UnsafeArray<byte> CpuData => _cpuData;
    public readonly Handle<GraphicsBuffer> GpuResource => _gpuResource;
    public readonly uint Size => _size;

    public readonly bool IsCreated => _size != 0 && _gpuResource.IsValid && _cpuData.IsCreated;

    public CBufferCache(Handle<GraphicsBuffer> buffer, uint bufferSize)
    {
        _size = bufferSize;
        _cpuData = new UnsafeArray<byte>((int)bufferSize, Allocator.Persistent);
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

        _gpuResource = Handle<GraphicsBuffer>.Invalid;
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

    private Identifier<Shader> _shader;
    private UnsafeArray<PipelineOverride> _passPipelineOverride;
    private bool _isDirty;

    internal CBufferCache _cBufferCache;
    internal LocalKeywordSet _keywordMask;

    public readonly Identifier<Shader> Shader => _shader;
    public readonly bool IsDirty => _isDirty;

    public int ActivePassIndex
    {
        get; set;
    }

    // For now, 0 means opaque, 1 means transparent, may be we need 2 means ui, etc. but higher values are reserved for user-defined render types.
    public uint MaterialRenderType
    {
        get; set;
    }

    public Error SetShader(Identifier<Shader> shaderId, ResourceManager resourceManager, IResourceDatabase resourceDatabase, IResourceAllocator resourceAllocator)
    {
        if (!shaderId.IsValid)
        {
            return Error.InvalidArgument;
        }

        _cBufferCache.ReleaseResource(resourceDatabase);
        _shader = shaderId;

        var r = resourceManager.GetShaderReference(shaderId);
        if (r.IsFailure)
        {
            return r.Error;
        }

        ref readonly var shader = ref r.Value;
        if (_passPipelineOverride.Count < shader.PassCount)
        {
            if (!_passPipelineOverride.IsCreated)
            {
                _passPipelineOverride = new UnsafeArray<PipelineOverride>(shader.PassCount, Allocator.Persistent);
            }
            else
            {
                _passPipelineOverride.Resize(shader.PassCount);
            }
        }

        _keywordMask.Clear();
        for (var i = 0; i < shader.PassCount; i++)
        {
            ref var pass = ref shader.GetPassReference(i);
            _passPipelineOverride[i] = new PipelineOverride
            {
                shaderPass = pass.Key,
                options = pass.DefaultState,
            };
        }

        if (shader.CBufferSize != 0)
        {
            var desc = new BufferDesc
            {
                Size = shader.CBufferSize,
                Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
                MemoryType = ResourceMemoryType.Default,
            };

            var buffer = resourceAllocator.CreateBuffer(ref desc, "MaterialCBuffer");
            _cBufferCache = new CBufferCache(buffer, shader.CBufferSize);
        }

        return Error.None;
    }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlySpan<byte> GetRawPropertyCache()
    {
        if (_cBufferCache.Size == 0)
        {
            return [];
        }

        return _cBufferCache.CpuData.AsSpan(0, (int)_cBufferCache.Size);
    }

    public unsafe Error SetPropertyCache<T>(scoped ref readonly T data)
        where T : unmanaged
    {
        if (sizeof(T) != _cBufferCache.Size)
        {
            return Error.InvalidArgument;
        }

        var dataSpan = MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in data));
        var cacheSpan = _cBufferCache.CpuData.AsSpan();
        if (cacheSpan.SequenceEqual(dataSpan))
        {
            return Error.None;
        }

        dataSpan.CopyTo(cacheSpan);
        _isDirty = true;

        return Error.None;
    }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly PipelineState GetPassPipelineOverride(int passIndex)
    {
        return _passPipelineOverride[passIndex].options;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPassPipelineOverride(int passIndex, scoped ref readonly PipelineState options)
    {
        ref var pipelineOverride = ref _passPipelineOverride[passIndex];
        pipelineOverride.options = options;
        _isDirty = true;
    }

    public Error SetKeyword(ResourceManager manager, int keywordId, bool enabled)
    {
        var r = manager.GetShaderReference(_shader);
        if (r.IsFailure)
        {
            return r.Error;
        }

        ref readonly var shader = ref r.Value;
        var localIndex = shader.GetLocalKeywordIndex(keywordId);
        if (localIndex == -1)
        {
            return Error.NotFound;
        }

        _keywordMask.SetKeyword(localIndex, enabled);
        _isDirty = true;

        return Error.None;
    }

    public readonly bool IsKeywordEnabled(ResourceManager manager, int keywordId)
    {
        var r = manager.GetShaderReference(_shader);
        if (r.IsFailure)
        {
            return false;
        }

        ref readonly var shader = ref r.Value;
        var localIndex = shader.GetLocalKeywordIndex(keywordId);
        if (localIndex == -1)
        {
            return false;
        }

        return _keywordMask.IsKeywordEnabled(localIndex);
    }

    public readonly void UploadData(ICommandBuffer cmd, IResourceDatabase resourceDatabase)
    {
        if (!_isDirty)
        {
            return;
        }

        var cbufferResource = _cBufferCache.GpuResource.AsResource();
        var r = resourceDatabase.GetResourceBarrierData(cbufferResource);
        if (r.IsFailure)
        {
            return;
        }

        var barrierData = r.Value;
        var desc = BarrierDesc.Buffer(
            cbufferResource,
            barrierData.sync,
            BarrierSync.Copy,
            barrierData.access,
            BarrierAccess.CopyDest);

        cmd.ResourceBarrier(desc);
        cmd.UploadBuffer(_cBufferCache.GpuResource, _cBufferCache.CpuData.AsSpan());

        desc = BarrierDesc.Buffer(
            cbufferResource,
            BarrierSync.Copy,
            BarrierSync.AllShading,
            BarrierAccess.CopyDest,
            BarrierAccess.ShaderResource);

        cmd.ResourceBarrier(desc);
    }

    public void ReleaseResource(IResourceDatabase database)
    {
        _cBufferCache.ReleaseResource(database);
        _passPipelineOverride.Dispose();
    }
}
