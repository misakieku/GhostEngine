using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;

namespace Ghost.Graphics.Core;

#if false
public struct VariantMask
{
    private ulong _mask;
}
#endif

internal struct CBufferCache : IResourceReleasable
{
    private UnsafeArray<byte> _cpuData;
    private Handle<GraphicsBuffer> _gpuResource;
    private uint _size;
    private uint _alignedSize;

    public readonly UnsafeArray<byte> CpuData => _cpuData;
    public readonly Handle<GraphicsBuffer> GpuResource => _gpuResource;
    public readonly uint Size => _size;
    public readonly uint AlignedSize => _alignedSize;

    public readonly bool IsCreated => _size != 0 && _gpuResource.IsValid && _cpuData.IsCreated;

    public CBufferCache(Handle<GraphicsBuffer> buffer, uint bufferSize)
    {
        _size = bufferSize;
        _alignedSize = (bufferSize + 255u) & ~255u;

        _cpuData = new UnsafeArray<byte>((int)AlignedSize, Allocator.Persistent);
        _gpuResource = buffer;
    }

    public void ReleaseResource(IResourceDatabase database)
    {
        if (!IsCreated)
        {
            return;
        }

        _cpuData.Dispose();

        database.ReleaseResource(GpuResource.AsResource());
        _gpuResource = Handle<GraphicsBuffer>.Invalid;

        _size = 0;
        _alignedSize = 0;
    }
}

public struct Material : IResourceReleasable, IHandleType
{
    private struct PipelineOverride
    {
        public ShaderPassKey shaderPass;
        public PipelineState options;
        public MaterialPipelineKey pipelineKey;
    }

    private Identifier<Shader> _shader;
    private CBufferCache _cBufferCache;
    private UnsafeArray<PipelineOverride> _passPipelineOverride;
    private LocalKeywordSet _keywordMask;

    private bool _isDirty;

    internal readonly CBufferCache CBufferCache => _cBufferCache;

    public readonly Identifier<Shader> Shader => _shader;
    public readonly bool IsDirty => _isDirty;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetDirty()
    {
        _isDirty = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal readonly MaterialPipelineKey GetPassPipelineKey(int passIndex)
    {
        return _passPipelineOverride[passIndex].pipelineKey;
    }

    public ErrorStatus SetShader(Identifier<Shader> shaderId, IResourceAllocator allocator, IResourceDatabase database)
    {
        if (!shaderId.IsValid)
        {
            return ErrorStatus.InvalidArgument;
        }

        _cBufferCache.ReleaseResource(database);
        _shader = shaderId;

        var shader = database.GetShaderReference(shaderId);

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
                shaderPass = pass.Identifier,
                options = pass.DeafaultState,
                pipelineKey = new MaterialPipelineKey(pass.Identifier, pass.DeafaultState),
            };
        }

        if (shader.CBufferSize != 0)
        {
            var desc = new BufferDesc
            {
                Size = shader.CBufferSize,
                Usage = BufferUsage.Constant,
                MemoryType = ResourceMemoryType.Default,
            };

            var buffer = allocator.CreateBuffer(ref desc);
            _cBufferCache = new CBufferCache(buffer, shader.CBufferSize);
        }

        return ErrorStatus.None;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly unsafe Result<T, ErrorStatus> GetPropertyCache<T>()
        where T : unmanaged
    {
        if (sizeof(T) != _cBufferCache.Size)
        {
            return ErrorStatus.InvalidArgument;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe ErrorStatus SetPropertyCache<T>(ref readonly T data)
        where T : unmanaged
    {
        if (sizeof(T) != _cBufferCache.Size)
        {
            return ErrorStatus.InvalidArgument;
        }

        Unsafe.WriteUnaligned(_cBufferCache.CpuData.GetUnsafePtr(), data);
        SetDirty();

        return ErrorStatus.None;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe ErrorStatus SetRawPropertyCache(ReadOnlySpan<byte> data)
    {
        if (data.Length != _cBufferCache.Size)
        {
            return ErrorStatus.InvalidArgument;
        }

        Unsafe.WriteUnaligned(_cBufferCache.CpuData.GetUnsafePtr(), data);
        SetDirty();

        return ErrorStatus.None;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly PipelineState GetPassPipelineOverride(int passIndex)
    {
        return _passPipelineOverride[passIndex].options;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPassPipelineOverride(int passIndex, ref readonly PipelineState options)
    {
        ref var pipelineOverride = ref _passPipelineOverride[passIndex];
        pipelineOverride.options = options;
        pipelineOverride.pipelineKey = new MaterialPipelineKey(pipelineOverride.shaderPass, options);
        SetDirty();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ErrorStatus SetKeyword(IResourceDatabase resourceDatabase, int keywordId, bool enabled)
    {
        ref var shader = ref resourceDatabase.GetShaderReference(_shader);
        var localIndex = shader.GetLocalKeywordIndex(keywordId);
        if (localIndex == -1)
        {
            return ErrorStatus.NotFound;
        }

        _keywordMask.SetKeyword(localIndex, enabled);
        SetDirty();

        return ErrorStatus.None;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsKeywordEnabled(int keywordId)
    {
        return _keywordMask.IsKeywordEnabled(keywordId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void UploadData(ICommandBuffer cmb)
    {
        cmb.UploadBuffer(_cBufferCache.GpuResource, _cBufferCache.CpuData.AsSpan());
        cmb.ResourceBarrier(_cBufferCache.GpuResource.AsResource(), ResourceState.VertexAndConstantBuffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void IResourceReleasable.ReleaseResource(IResourceDatabase database)
    {
        _cBufferCache.ReleaseResource(database);
        _passPipelineOverride.Dispose();
    }
}
