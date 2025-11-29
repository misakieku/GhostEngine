using Ghost.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;

namespace Ghost.Graphics.Core;

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
    private Identifier<Shader> _shader;
    private CBufferCache _cBufferCache;

    internal readonly CBufferCache CBufferCache => _cBufferCache;

    public readonly Identifier<Shader> Shader => _shader;

    public Result SetShader(Identifier<Shader> shaderId, IResourceAllocator allocator, IResourceDatabase database)
    {
        if (!shaderId.IsValid)
        {
            return Result.Failure("Shader ID is invalid.");
        }

        _cBufferCache.ReleaseResource(database);
        _shader = shaderId;

        var shader = database.GetShaderReference(shaderId);
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

        return Result.Success();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly unsafe Result<T, ResultStatus> GetPropertyCache<T>()
        where T : unmanaged
    {
        if (sizeof(T) != _cBufferCache.Size)
        {
            return Result.Create(default(T), ResultStatus.InvalidArgument);
        }

        return Result.Create(*(T*)_cBufferCache.CpuData.GetUnsafePtr(), ResultStatus.Success);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<byte> GetRawPropertyCache()
    {
        if (_cBufferCache.Size == 0)
        {
            return Span<byte>.Empty;
        }

        return _cBufferCache.CpuData.AsSpan(0, (int)_cBufferCache.Size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly unsafe ResultStatus SetPropertyCache<T>(ref readonly T data)
        where T : unmanaged
    {
        if (sizeof(T) != _cBufferCache.Size)
        {
            return ResultStatus.InvalidArgument;
        }

        Unsafe.WriteUnaligned(_cBufferCache.CpuData.GetUnsafePtr(), data);
        return ResultStatus.Success;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly unsafe ResultStatus SetRawPropertyCache(ReadOnlySpan<byte> data)
    {
        if (data.Length != _cBufferCache.Size)
        {
            return ResultStatus.InvalidArgument;
        }

        Unsafe.WriteUnaligned(_cBufferCache.CpuData.GetUnsafePtr(), data);
        return ResultStatus.Success;
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
    }
}
