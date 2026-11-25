using Ghost.Core;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Mathematics.Geometry;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.RenderPipeline;

internal readonly record struct ShadowCacheKey(ulong EntityKey, uint FaceIndex);

internal struct ShadowViewCacheQuery
{
    public ulong entityKey;
    public uint faceIndex;
    public float3 positionWS;
    public float3 directionWS;
    public float range;
    public float fovOrAngle;
    public float nearPlane;
    public uint shadowSize;
    public float lodErrorScale;
    public Frustum worldFrustum;
}

internal sealed unsafe class PunctualShadowCache : IDisposable
{
    private struct ShadowInstanceRecord
    {
        public float4x4 localToWorld;
        public Handle<Mesh> mesh;
        public AABB worldBounds;
        public byte castsShadow;
    }

    private sealed class CachedShadowView
    {
        public ShadowCacheKey key;
        public uint nodeIndex;
        public ShadowAtlasRegion region;
        public ulong lastUsedFrame;
        public bool isDirty;
        public bool isInitialized;
        public Frustum worldFrustum;
        public float3 positionWS;
        public float3 directionWS;
        public float range;
        public float fovOrAngle;
        public float nearPlane;
        public uint shadowSize;
        public float lodErrorScale;
    }

    private readonly IResourceAllocator _resourceAllocator;
    private readonly IResourceDatabase _resourceDatabase;
    private readonly ShadowAtlasBuddyAllocator _allocator;
    private readonly Dictionary<ShadowCacheKey, CachedShadowView> _entries = new();
    private readonly Dictionary<uint, ShadowCacheKey> _nodeToKey = new();
    private readonly List<uint> _evictLeavesScratch = new(16);
    private UnsafeList<ShadowInstanceRecord> _instances;

    private Handle<GPUTexture> _atlasTexture;
    private ulong _currentFrame = 1;
    private bool _disposed;

    public uint AtlasSize => _allocator.AtlasSize;
    public Handle<GPUTexture> AtlasTexture => _atlasTexture;
    public int CachedViewCount => _entries.Count;

    public PunctualShadowCache(IResourceAllocator resourceAllocator, IResourceDatabase resourceDatabase, uint atlasSize)
    {
        _resourceAllocator = resourceAllocator;
        _resourceDatabase = resourceDatabase;
        _allocator = new ShadowAtlasBuddyAllocator(atlasSize);
        _instances = new UnsafeList<ShadowInstanceRecord>(256, AllocationHandle.Persistent);

        var texDesc = new TextureDesc
        {
            Width = _allocator.AtlasSize,
            Height = _allocator.AtlasSize,
            Slice = 1,
            MipLevels = 1,
            Dimension = TextureDimension.Texture2D,
            Format = TextureFormat.R32_Typeless,
            Usage = TextureUsage.ShaderResource | TextureUsage.UnorderedAccess,
            TypelessViewFormat = new TypelessFormatDesc
            {
                Srv = TextureFormat.R32_Float,
                Uav = TextureFormat.R32_UInt
            }
        };

        _atlasTexture = _resourceAllocator.CreateTexture(in texDesc, "PunctualShadowAtlas");
    }

    /// <summary>
    /// Constructor for unit testing allocator/invalidation logic without creating a GPU texture.
    /// </summary>
    internal PunctualShadowCache(uint atlasSize)
    {
        _resourceAllocator = null!;
        _resourceDatabase = null!;
        _allocator = new ShadowAtlasBuddyAllocator(atlasSize);
        _instances = new UnsafeList<ShadowInstanceRecord>(16, AllocationHandle.Persistent);
        _atlasTexture = Handle<GPUTexture>.Invalid;
    }

    public void OnInstanceUpdated(uint instanceId, in float4x4 localToWorld, Handle<Mesh> mesh, in AABB localBounds, ShadowCastingMode shadowCastingMode)
    {
        var idx = (int)instanceId;
        if (idx >= _instances.Capacity)
        {
            var newCap = Math.Max(idx + 1, _instances.Capacity * 2);
            _instances.Resize(newCap);
        }

        if (idx >= _instances.Count)
        {
            var oldCount = _instances.Count;
            _instances.UnsafeSetCount(idx + 1);
            _instances.AsSpan().Slice(oldCount, idx + 1 - oldCount).Clear();
        }

        ref var record = ref _instances[idx];
        if (record.castsShadow != 0)
        {
            InvalidateBounds(in record.worldBounds);
        }

        var castsShadow = shadowCastingMode != ShadowCastingMode.Off;
        var worldBounds = Utilities.MathUtility.TransformAABB(in localBounds, in localToWorld);

        record.localToWorld = localToWorld;
        record.mesh = mesh;
        record.worldBounds = worldBounds;
        record.castsShadow = (byte)(castsShadow ? 1 : 0);

        if (castsShadow)
        {
            InvalidateBounds(in worldBounds);
        }
    }

    public void OnInstanceRemoved(uint instanceId, uint swapWithInstanceId)
    {
        var idx = (int)instanceId;
        var swapIdx = (int)swapWithInstanceId;
        if (idx < 0 || idx >= _instances.Count)
        {
            return;
        }

        if (_instances[idx].castsShadow != 0)
        {
            InvalidateBounds(in _instances[idx].worldBounds);
        }

        if (swapIdx >= 0 && swapIdx < _instances.Count)
        {
            _instances[idx] = _instances[swapIdx];
            if (swapIdx < _instances.Count)
            {
                _instances.UnsafeSetCount(swapIdx);
            }
        }
    }

    public void OnMeshesStreamed(ResourceManager resourceManager, ReadOnlySpan<Handle<Mesh>> streamedMeshes)
    {
        if (streamedMeshes.IsEmpty || _instances.Count == 0 || _entries.Count == 0)
        {
            return;
        }

        for (var i = 0; i < _instances.Count; i++)
        {
            ref var record = ref _instances[i];
            if (record.castsShadow == 0 || !record.mesh.IsValid)
            {
                continue;
            }

            var matched = false;
            for (var m = 0; m < streamedMeshes.Length; m++)
            {
                if (streamedMeshes[m] == record.mesh)
                {
                    matched = true;
                    break;
                }
            }

            if (!matched)
            {
                continue;
            }

            var (meshRef, err) = resourceManager.GetMeshReference(record.mesh);
            if (err.IsSuccess)
            {
                var localBounds = meshRef.Get().BoundingBox;
                record.worldBounds = Utilities.MathUtility.TransformAABB(in localBounds, in record.localToWorld);
            }

            InvalidateBounds(in record.worldBounds);
        }
    }

    public void BeginFrame(ulong frameNumber)
    {
        _currentFrame = Math.Max(1ul, frameNumber);
    }

    public void InvalidateBounds(in AABB worldBounds)
    {
        foreach (var entry in _entries.Values)
        {
            if (entry.isDirty)
            {
                continue;
            }

            if (!Utilities.MathUtility.SphereIntersectsAABB(entry.positionWS, entry.range, in worldBounds))
            {
                continue;
            }

            if (Utilities.MathUtility.AABBIntersectFrustum(in worldBounds, entry.worldFrustum.planes))
            {
                entry.isDirty = true;
            }
        }
    }

    public void InvalidateAll()
    {
        foreach (var entry in _entries.Values)
        {
            entry.isDirty = true;
        }
    }

    public bool TryAcquireView(in ShadowViewCacheQuery query, out ShadowAtlasRegion region, out bool isDirty)
    {
        var key = new ShadowCacheKey(query.entityKey, query.faceIndex);
        if (_entries.TryGetValue(key, out var existing))
        {
            if (existing.shadowSize == query.shadowSize)
            {
                existing.lastUsedFrame = _currentFrame;
                _allocator.Touch(existing.nodeIndex, _currentFrame);

                if (!MatchesLightState(existing, query.positionWS, query.directionWS, query.range, query.fovOrAngle, query.nearPlane, query.lodErrorScale))
                {
                    existing.isDirty = true;
                    existing.positionWS = query.positionWS;
                    existing.directionWS = query.directionWS;
                    existing.range = query.range;
                    existing.fovOrAngle = query.fovOrAngle;
                    existing.nearPlane = query.nearPlane;
                    existing.lodErrorScale = query.lodErrorScale;
                    existing.worldFrustum = query.worldFrustum;
                }

                region = existing.region;
                isDirty = existing.isDirty || !existing.isInitialized;
                return true;
            }

            // Shadow resolution changed: free old tile and re-allocate
            FreeEntry(key, existing);
        }

        if (!AllocateWithEviction(query.shadowSize, out var nodeIndex, out region))
        {
            isDirty = false;
            return false;
        }

        var entry = new CachedShadowView
        {
            key = key,
            nodeIndex = nodeIndex,
            region = region,
            lastUsedFrame = _currentFrame,
            isDirty = true,
            isInitialized = false,
            worldFrustum = query.worldFrustum,
            positionWS = query.positionWS,
            directionWS = query.directionWS,
            range = query.range,
            fovOrAngle = query.fovOrAngle,
            nearPlane = query.nearPlane,
            shadowSize = query.shadowSize,
            lodErrorScale = query.lodErrorScale,
        };

        _entries[key] = entry;
        _nodeToKey[nodeIndex] = key;
        isDirty = true;
        return true;
    }

    public void ReleaseLightFaces(ulong entityKey, uint faceCount)
    {
        for (uint f = 0; f < faceCount; f++)
        {
            var key = new ShadowCacheKey(entityKey, f);
            if (_entries.TryGetValue(key, out var entry))
            {
                FreeEntry(key, entry);
            }
        }
    }

    public void MarkRendered(ulong entityKey, uint faceIndex)
    {
        var key = new ShadowCacheKey(entityKey, faceIndex);
        if (_entries.TryGetValue(key, out var entry))
        {
            entry.isDirty = false;
            entry.isInitialized = true;
        }
    }

    private bool AllocateWithEviction(uint shadowSize, out uint nodeIndex, out ShadowAtlasRegion region)
    {
        if (_allocator.TryAllocate(shadowSize, _currentFrame, out nodeIndex, out region))
        {
            return true;
        }

        while (_allocator.TryFindEvictionBlock(shadowSize, _currentFrame, out _, _evictLeavesScratch))
        {
            for (var i = 0; i < _evictLeavesScratch.Count; i++)
            {
                var leafNode = _evictLeavesScratch[i];
                if (_nodeToKey.TryGetValue(leafNode, out var victimKey) && _entries.TryGetValue(victimKey, out var victimEntry))
                {
                    FreeEntry(victimKey, victimEntry);
                }
                else
                {
                    _allocator.Free(leafNode);
                }
            }

            if (_allocator.TryAllocate(shadowSize, _currentFrame, out nodeIndex, out region))
            {
                return true;
            }
        }

        nodeIndex = uint.MaxValue;
        region = default;
        return false;
    }

    private void FreeEntry(ShadowCacheKey key, CachedShadowView entry)
    {
        _entries.Remove(key);
        _nodeToKey.Remove(entry.nodeIndex);
        _allocator.Free(entry.nodeIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool MatchesLightState(CachedShadowView entry, float3 positionWS, float3 directionWS, float range, float fovOrAngle, float nearPlane, float lodErrorScale)
    {
        const float epsSq = 1e-10f;
        const float eps = 1e-5f;
        return math.lengthsq(entry.positionWS - positionWS) <= epsSq
            && math.lengthsq(entry.directionWS - directionWS) <= epsSq
            && MathF.Abs(entry.range - range) <= eps
            && MathF.Abs(entry.fovOrAngle - fovOrAngle) <= eps
            && MathF.Abs(entry.nearPlane - nearPlane) <= eps
            && MathF.Abs(entry.lodErrorScale - lodErrorScale) <= eps;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _entries.Clear();
        _nodeToKey.Clear();
        if (_instances.IsCreated)
        {
            _instances.Dispose();
        }

        if (_atlasTexture.IsValid && _resourceDatabase != null)
        {
            _resourceDatabase.ReleaseResource(_atlasTexture.AsResource());
            _atlasTexture = Handle<GPUTexture>.Invalid;
        }
    }
}
