using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Graphics.Core;

public struct Material : IResourceReleasable
{
    private struct PipelineOverride
    {
        public Key64<ShaderPass> shaderPass;
        public PipelineState options;
    }

    private Handle<Shader> _shader;
    private UnsafeArray<PipelineOverride> _passPipelineOverride;
    private UnsafeArray<byte> _cpuData;
    private uint _poolOffset;
    private uint _propertySize;
    private bool _hasAlphaClip;
    private bool _isDirty;

    public readonly Handle<Shader> Shader => _shader;
    public readonly bool IsDirty => _isDirty;
    public readonly uint PoolOffset => _poolOffset;
    public readonly uint PropertySize => _propertySize;
    public readonly bool HasAlphaClip => _hasAlphaClip;
    public readonly bool IsCreated => _propertySize != 0 && _cpuData.IsCreated;

    /// <summary>
    /// Active pass index for the material. This is used to determine which pass to use when rendering the material.
    /// </summary>
    public int ActivePassIndex { get; set; }

    /// <summary>
    /// Dense runtime variant index in the shader variant registry.
    /// </summary>
    public uint VariantIndex { get; set; }

    internal void ClearDirty() => _isDirty = false;
    internal void SetHasAlphaClip(bool hasAlpha) => _hasAlphaClip = hasAlpha;

    /// <summary>
    /// Sets the shader for the material, records its pool offset, and initializes the CPU property buffer and pipeline overrides.
    /// </summary>
    public Error SetShader(Handle<Shader> shade, uint poolOffset, ResourceManager resourceManager, IResourceDatabase resourceDatabase, IResourceAllocator resourceAllocator)
    {
        if (!shade.IsValid)
        {
            return Error.InvalidArgument;
        }

        if (_cpuData.IsCreated)
        {
            _cpuData.Dispose();
        }

        _shader = shade;
        _poolOffset = poolOffset;

        var r = resourceManager.GetShaderReference(shade);
        if (r.IsFailure)
        {
            return r.Error;
        }

        ref var shader = ref r.Value;
        VariantIndex = shader.VariantIndex;
        _propertySize = shader.PropertyBufferSize;
        _hasAlphaClip = false;
        _isDirty = true;

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

        if (_propertySize != 0)
        {
            _cpuData = new UnsafeArray<byte>((int)_propertySize, AllocationHandle.Persistent);
        }

        return Error.None;
    }

    /// <summary>
    /// Gets the property cache of the material as a struct of type T.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly unsafe Result<T, Error> GetPropertyCache<T>() where T : unmanaged
    {
        if (sizeof(T) != _propertySize || !_cpuData.IsCreated)
        {
            return Error.InvalidArgument;
        }

        return *(T*)_cpuData.GetUnsafePtr();
    }

    /// <summary>
    /// Gets the raw property cache of the material as a ReadOnlySpan of bytes.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlySpan<byte> GetRawPropertyCache()
    {
        if (_propertySize == 0 || !_cpuData.IsCreated)
        {
            return Span<byte>.Empty;
        }

        return _cpuData.AsSpan(0, (int)_propertySize);
    }

    /// <summary>
    /// Sets the property cache of the material with a struct of type T.
    /// </summary>
    public unsafe Error SetPropertyCache<T>(scoped in T data) where T : unmanaged
    {
        if (sizeof(T) != _propertySize)
        {
            return Error.InvalidArgument;
        }

        var dataSpan = MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in data));
        return SetRawPropertyCache(dataSpan);
    }

    /// <summary>
    /// Sets the raw property cache of the material with a ReadOnlySpan of bytes.
    /// </summary>
    public Error SetRawPropertyCache(ReadOnlySpan<byte> data)
    {
        if (data.Length != _propertySize || !_cpuData.IsCreated)
        {
            return Error.InvalidArgument;
        }

        var cacheSpan = _cpuData.AsSpan();
        if (cacheSpan.SequenceEqual(data))
        {
            return Error.None;
        }

        data.CopyTo(cacheSpan);
        _isDirty = true;

        var hasAlpha = data.Length > 16 && data[16] != 0;
        _hasAlphaClip = hasAlpha;

        return Error.None;
    }

    /// <summary>
    /// Gets the pipeline state override for a specific pass index.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly PipelineState GetPassPipelineOverride(int passIndex)
    {
        return _passPipelineOverride[passIndex].options;
    }

    /// <summary>
    /// Sets the pipeline state override for a specific pass index.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPassPipelineOverride(int passIndex, scoped in PipelineState options)
    {
        ref var pipelineOverride = ref _passPipelineOverride[passIndex];
        pipelineOverride.options = options;
        _isDirty = true;
    }

    public void ReleaseResource(IResourceDatabase database)
    {
        if (_cpuData.IsCreated)
        {
            _cpuData.Dispose();
        }

        if (_passPipelineOverride.IsCreated)
        {
            _passPipelineOverride.Dispose();
        }
    }
}

