#if false
using Ghost.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.CompilerServices;

namespace Ghost.Graphics.Services;

public enum TextureSizeMode : byte
{
    /// <summary>
    /// Fixed pixel dimensions (width, height).
    /// </summary>
    Absolute,

    /// <summary>
    /// Scale relative to view state (scaleX * viewportWidth, scaleY * viewportHeight).
    /// </summary>
    Relative
}

public struct RelativeTexture;

public struct RelativeTextureDesc
{
    /// <summary>
    /// Size mode of the texture
    /// </summary>
    public TextureSizeMode SizeMode
    {
        get; set;
    }

    /// <summary>
    /// Width of the texture
    /// </summary>
    public uint Width
    {
        get; set;
    }

    /// <summary>
    /// Height of the texture
    /// </summary>
    public uint Height
    {
        get; set;
    }

    /// <summary>
    /// Scale of the texture
    /// </summary>
    public float2 Scale
    {
        get; set;
    }

    /// <summary>
    /// Slice of the texture
    /// </summary>
    public uint Slice
    {
        get; set;
    }

    /// <summary>
    /// Texture Format
    /// </summary>
    public TextureFormat Format
    {
        get; set;
    }

    /// <summary>
    /// Texture dimension
    /// </summary>
    public TextureDimension Dimension
    {
        get;
        set;
    }

    /// <summary>
    /// Number of mip levels. 0 to generate full mip chain
    /// </summary>
    public uint MipLevels
    {
        get; set;
    }

    /// <summary>
    /// Texture usage flags
    /// </summary>
    public TextureUsage Usage
    {
        get; set;
    }
}

public sealed class RelativeTextureSystem : IDisposable
{
    private struct RTRecord
    {
#if GHOST_SAFETY_CHECKS
        public int id;
#endif

        public RelativeTextureDesc desc;
        public CreationOptions options;

        // From AdditionalTextureDesc
        public UnsafeArray<TextureFormat> castableFormat;
        public TextureViewCreationFlags viewCreationFlags;

        public Handle<GPUTexture> realTexture;

        public void Release(IResourceDatabase resourceDatabase)
        {
            if (realTexture.IsValid)
            {
                resourceDatabase.ReleaseResource(realTexture.AsResource());
            }

            castableFormat.Dispose();
        }
    }

    private readonly IResourceAllocator _resourceAllocator;
    private readonly IResourceDatabase _resourceDatabase;

    private UnsafeSlotMap<RTRecord> _records;
    private UnsafeHashSet<Handle<RelativeTexture>> _pendingRTs;
#if GHOST_SAFETY_CHECKS
    private string[] _recordNames;
#endif

    private bool _needResize;
    private uint2 _targetSize;
    private float4 _scalingFactpr;

    public bool NeedResize => _needResize;
    public float4 ScalingFactor => _scalingFactpr;

    public RelativeTextureSystem(IResourceAllocator resourceAllocator, IResourceDatabase resourceDatabase)
    {
        _resourceAllocator = resourceAllocator;
        _resourceDatabase = resourceDatabase;

        _records = new UnsafeSlotMap<RTRecord>(1024, AllocationHandle.Persistent);
        _pendingRTs = new UnsafeHashSet<Handle<RelativeTexture>>(64, AllocationHandle.Persistent);

#if GHOST_SAFETY_CHECKS
        _recordNames = new string[1024];
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CreateRealTexture(ref RTRecord record, uint2 screenSize)
    {
        Logger.DebugAssert(record.desc.SizeMode == TextureSizeMode.Absolute || record.realTexture.IsInvalid);

        var width = (uint)(record.desc.Scale.x * screenSize.x);
        var height = (uint)(record.desc.Scale.y * screenSize.y);
        var texDesc = new TextureDesc
        {
            Width = width,
            Height = height,
            Slice = record.desc.Slice,
            Format = record.desc.Format,
            Dimension = record.desc.Dimension,
            MipLevels = record.desc.MipLevels,
            Usage = record.desc.Usage
        };

        var name =
#if GHOST_SAFETY_CHECKS
            _recordNames[record.id];
#else
            string.Empty;
#endif
        var additionalDesc = new AdditionalTextureDesc
        {
            CastableFormat = record.castableFormat,
            ViewCreationFlags = record.viewCreationFlags
        };

        // Update the record's width and height to reflect the actual size of the created texture
        record.desc.Width = width;
        record.desc.Height = height;
        record.realTexture = _resourceAllocator.CreateTexture(texDesc, name, record.options, additionalDesc);
        Logger.DebugAssert(record.realTexture.IsValid, "Failed to create texture for relative texture");
    }

    public void SetTargetSize(uint2 screenSize)
    {
        if (!_targetSize.Equals(screenSize))
        {
            _needResize = math.any(screenSize > _targetSize);

            if (_needResize)
            {
                _targetSize = screenSize;
            }

            var x = (float)screenSize.x / _targetSize.x;
            var y = (float)screenSize.y / _targetSize.y;
            _scalingFactpr = new float4(x, y, 1.0f / x, 1.0f / y);
        }
    }

    public void ResolveRTs()
    {
        if (_needResize)
        {
            foreach (ref var record in _records)
            {
                if (record.desc.SizeMode == TextureSizeMode.Absolute || record.realTexture.IsInvalid)
                {
                    continue;
                }

                _resourceDatabase.ReleaseResource(record.realTexture.AsResource());

                CreateRealTexture(ref record, _targetSize);
            }
        }

        foreach (var handle in _pendingRTs)
        {
            ref var record = ref _records.GetElementReferenceAt(handle.ID, handle.Generation, out var exist);
            Logger.DebugAssert(exist, "Relative texture handle is invalid");

            CreateRealTexture(ref record, _targetSize);
        }

        _pendingRTs.Clear();
        _needResize = false;
    }

    public Handle<RelativeTexture> CreateRelativeTexture(scoped in RelativeTextureDesc desc, string? name = null, CreationOptions options = default, AdditionalTextureDesc additionalDesc = default)
    {
        var castableFormat = default(UnsafeArray<TextureFormat>);
        if (!additionalDesc.CastableFormat.IsEmpty)
        {
            castableFormat = new UnsafeArray<TextureFormat>(additionalDesc.CastableFormat.Length, AllocationHandle.Persistent);
            castableFormat.CopyFrom(additionalDesc.CastableFormat);
        }

        var record = new RTRecord
        {
            desc = desc,
            options = options,
            castableFormat = castableFormat,
            viewCreationFlags = additionalDesc.ViewCreationFlags
        };

        var id = _records.Add(record, out var generation);
        var handle = new Handle<RelativeTexture>(id, generation);

#if GHOST_SAFETY_CHECKS
        if (id >= _recordNames.Length)
        {
            Array.Resize(ref _recordNames, Math.Max(_recordNames.Length * 2, id + 1));
        }

        _recordNames[id] = name ?? $"RelativeTexture_{id}";
        _records.GetElementReferenceAt(id, generation, out _).id = id;
#endif

        _pendingRTs.Add(handle);
        return handle;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReleaseRelativeTexture(Handle<RelativeTexture> handle)
    {
        if (_records.Remove(handle.ID, handle.Generation, out var record))
        {
            record.Release(_resourceDatabase);
        }

        _pendingRTs.Remove(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float4 GetScalingFactor(Handle<RelativeTexture> handle)
    {
        if (!_records.TryGetElementAt(handle.ID, handle.Generation, out var record))
        {
            Logger.DebugAssert(false, "Relative texture handle is invalid");
            return float4.zero;
        }

        if (record.desc.SizeMode == TextureSizeMode.Absolute)
        {
            return float4.one;
        }

        var x = (float)record.desc.Width / _targetSize.x;
        var y = (float)record.desc.Height / _targetSize.y;
        return new float4(x, y, 1.0f / x, 1.0f / y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Handle<GPUTexture> GetRealTexture(Handle<RelativeTexture> handle)
    {
        if (!_records.TryGetElementAt(handle.ID, handle.Generation, out var record))
        {
            return Handle<GPUTexture>.Invalid;
        }

        return record.realTexture;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Result<RelativeTextureDesc, Error> GetResourceDescription(Handle<RelativeTexture> handle)
    {
        if (!_records.TryGetElementAt(handle.ID, handle.Generation, out var record))
        {
            return Error.InvalidArgument;
        }

        return record.desc;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string GetResourceName(Handle<RelativeTexture> handle)
    {
#if GHOST_SAFETY_CHECKS
        if (handle.ID < 0 || handle.ID >= _recordNames.Length)
        {
            return string.Empty;
        }

        return _recordNames[handle.ID];
#else
        return string.Empty;
#endif
    }

    public void Dispose()
    {
        foreach (var record in _records)
        {
            record.Release(_resourceDatabase);
        }

        _records.Dispose();
        _pendingRTs.Dispose();
    }
}
#endif