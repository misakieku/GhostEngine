using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Engine.RenderPipeline;

/// <summary>
/// Represents an allocated viewport rectangle in the shadow atlas.
/// </summary>
public struct ShadowAtlasRegion
{
    public uint x;
    public uint y;
    public uint width;
    public uint height;
    /// <summary>
    /// Normalized UV transform in atlas space with a 1-texel gutter to prevent PCF filter bleeding.
    /// xy: UV offset [0..1], zw: UV scale [0..1].
    /// </summary>
    public float4 tileOffsetScale;
}

/// <summary>
/// Fast 2D shelf allocator for managing per-view transient shadow atlas regions.
/// Supports single-tile allocations (Spot lights) and 6-tile blocks (Point lights).
/// </summary>
public sealed class ShadowAtlasRegionAllocator : IDisposable
{
    private struct Shelf
    {
        public uint y;
        public uint height;
        public uint currentX;
    }

    private readonly Lock _lock;
    private uint _atlasWidth;
    private uint _atlasHeight;
    private UnsafeList<Shelf> _shelves;
    private uint _nextShelfY;

    public uint AtlasWidth => _atlasWidth;
    public uint AtlasHeight => _atlasHeight;

    public ShadowAtlasRegionAllocator(uint atlasWidth, uint atlasHeight)
        : this(atlasWidth, atlasHeight, AllocationHandle.Persistent)
    {
    }

    public ShadowAtlasRegionAllocator(uint atlasWidth, uint atlasHeight, AllocationHandle allocationHandle)
    {
        _lock = new Lock();
        _atlasWidth = atlasWidth;
        _atlasHeight = atlasHeight;
        _shelves = new UnsafeList<Shelf>(16, allocationHandle);
        _nextShelfY = 0;
    }

    public void Reset(uint atlasWidth, uint atlasHeight)
    {
        _atlasWidth = atlasWidth;
        _atlasHeight = atlasHeight;

        _shelves.Clear();
        _nextShelfY = 0;
    }

    /// <summary>
    /// Allocates a single tile of the given dimensions in the shadow atlas.
    /// Thread-safe via internal Lock.
    /// </summary>
    public bool Allocate(uint width, uint height, out ShadowAtlasRegion region)
    {
        if (width > _atlasWidth || height > _atlasHeight)
        {
            region = default;
            return false;
        }

        lock (_lock)
        {
            return AllocateCore(width, height, out region);
        }
    }

    /// <summary>
    /// Allocates 6 contiguous tiles of the given dimension for a point light's 6 cubemap faces (+X, -X, +Y, -Y, +Z, -Z).
    /// Thread-safe via internal Lock.
    /// </summary>
    public bool AllocatePointLightBlock(uint faceSize, Span<ShadowAtlasRegion> outFaces)
    {
        if (outFaces.Length < 6)
        {
            return false;
        }

        if (faceSize > _atlasWidth || faceSize > _atlasHeight)
        {
            return false;
        }

        lock (_lock)
        {
            // Snapshot current state in case allocation fails midway
            var snapshotNextY = _nextShelfY;
            var snapshotShelfCount = _shelves.Count;
            Span<uint> snapshotCurrentXs = stackalloc uint[snapshotShelfCount];
            for (var s = 0; s < snapshotShelfCount; s++)
            {
                snapshotCurrentXs[s] = _shelves[s].currentX;
            }

            for (var i = 0; i < 6; i++)
            {
                if (!AllocateCore(faceSize, faceSize, out outFaces[i]))
                {
                    // Rollback to snapshot state
                    _nextShelfY = snapshotNextY;
                    if (snapshotShelfCount == 0)
                    {
                        _shelves.Clear();
                    }
                    else if (_shelves.Count > snapshotShelfCount)
                    {
                        _shelves.UnsafeSetCount(snapshotShelfCount);
                    }
                    for (var s = 0; s < snapshotShelfCount; s++)
                    {
                        ref var shelf = ref _shelves[s];
                        shelf.currentX = snapshotCurrentXs[s];
                    }

                    return false;
                }
            }

            return true;
        }
    }

    private bool AllocateCore(uint width, uint height, out ShadowAtlasRegion region)
    {
        region = default;

        // Try to fit into an existing shelf with the same or sufficient height
        for (var i = 0; i < _shelves.Count; i++)
        {
            ref var shelf = ref _shelves[i];
            if (shelf.height >= height && shelf.currentX + width <= _atlasWidth)
            {
                var allocX = shelf.currentX;
                var allocY = shelf.y;
                shelf.currentX += width;

                region = CreateRegion(allocX, allocY, width, height);
                return true;
            }
        }

        // Create a new shelf if space remains vertically
        if (_nextShelfY + height <= _atlasHeight)
        {
            var newShelf = new Shelf
            {
                y = _nextShelfY,
                height = height,
                currentX = width
            };

            var allocX = 0u;
            var allocY = _nextShelfY;
            _nextShelfY += height;
            _shelves.Add(newShelf);

            region = CreateRegion(allocX, allocY, width, height);
            return true;
        }

        // Atlas is full
        return false;
    }

    private ShadowAtlasRegion CreateRegion(uint x, uint y, uint w, uint h)
    {
        const uint gutter = 0;
        var innerW = (w > gutter * 2u) ? (w - gutter * 2u) : w;
        var innerH = (h > gutter * 2u) ? (h - gutter * 2u) : h;

        var offsetX = (float)(x + gutter) / _atlasWidth;
        var offsetY = (float)(y + gutter) / _atlasHeight;
        var scaleX = (float)innerW / _atlasWidth;
        var scaleY = (float)innerH / _atlasHeight;

        return new ShadowAtlasRegion
        {
            x = x,
            y = y,
            width = w,
            height = h,
            tileOffsetScale = new float4(offsetX, offsetY, scaleX, scaleY)
        };
    }

    public void Dispose()
    {
        _shelves.Dispose();
    }
}
