#ifndef GHOST_LIGHT_GRID_COMMON_HLSL
#define GHOST_LIGHT_GRID_COMMON_HLSL

#include "EngineResources/Shaders/Common.hlsl"

#define FPTL_TILE_SIZE 16
#define CLUSTER_TILE_SIZE 16u

struct TileLightFetchResult
{
    uint lightIndex;
    bool valid;
};

struct ClusterLightGridEntry
{
    uint offset;
    uint count;
};

uint DepthToClusterSlice(float viewZ, float nearClip, float farClip, uint sliceCount)
{
    float safeNear = max(nearClip, 1e-4f);
    float safeFar = max(farClip, safeNear + 1e-3f);
    float clampedZ = clamp(viewZ, safeNear, safeFar);
    float t = log2(clampedZ / safeNear) / max(log2(safeFar / safeNear), 1e-5f);
    return min((uint)floor(t * float(sliceCount)), sliceCount - 1u);
}

uint GetClusterIndex(uint2 tileCoord, uint sliceZ, uint tilesX, uint sliceCount)
{
    return (tileCoord.y * tilesX + tileCoord.x) * sliceCount + sliceZ;
}

ClusterLightGridEntry GetClusterLightGridEntry(ByteAddressBuffer clusterLightGrid, uint clusterIndex)
{
    uint2 raw = clusterLightGrid.Load2(clusterIndex * 8u);
    ClusterLightGridEntry entry;
    entry.offset = raw.x;
    entry.count = raw.y;
    return entry;
}

uint FetchClusterLightIndex(ByteAddressBuffer clusterLightIndexList, uint offset, uint lightOffset)
{
    return clusterLightIndexList.Load((offset + lightOffset) * 4u);
}

/// <summary>
/// Returns the number of punctual lights affecting the given 16x16 tile.
/// DWORDS can be 16 (up to 31 lights) or 32 (up to 63 lights).
/// </summary>
template<uint DWORDS>
uint GetTileLightCount(ByteAddressBuffer tileLightList, uint tileIndex)
{
    uint tileByteOffset = tileIndex * (DWORDS * 4u);
    uint dword0 = tileLightList.Load(tileByteOffset);
    return dword0 & 0xFFFFu;
}

/// <summary>
/// Fetches a punctual light index from the tile light list at the given light offset (0 <= lightOffset < count).
/// DWORDS can be 16 or 32.
/// </summary>
template<uint DWORDS>
TileLightFetchResult FetchTileLight(ByteAddressBuffer tileLightList, uint tileIndex, uint lightOffset)
{
    TileLightFetchResult result;
    result.lightIndex = 0u;
    result.valid = false;

    uint tileByteOffset = tileIndex * (DWORDS * 4u);

    if (lightOffset == 0u)
    {
        uint dword0 = tileLightList.Load(tileByteOffset);
        uint count = dword0 & 0xFFFFu;
        if (count > 0u)
        {
            result.lightIndex = dword0 >> 16u;
            result.valid = true;
        }
    }
    else
    {
        uint dwordIndex = (lightOffset + 1u) / 2u;
        if (dwordIndex < DWORDS)
        {
            uint raw = tileLightList.Load(tileByteOffset + dwordIndex * 4u);
            result.lightIndex = ((lightOffset + 1u) & 1u) ? (raw >> 16u) : (raw & 0xFFFFu);
            result.valid = true;
        }
    }

    return result;
}

#endif // GHOST_LIGHT_GRID_COMMON_HLSL
