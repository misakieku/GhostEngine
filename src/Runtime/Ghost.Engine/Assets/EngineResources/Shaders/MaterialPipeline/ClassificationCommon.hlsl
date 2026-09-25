#ifndef GHOST_CLASSIFICATION_COMMON_HLSL
#define GHOST_CLASSIFICATION_COMMON_HLSL

#define CLASSIFICATION_TILE_SIZE 16u
#define CLASSIFICATION_TILE_SIZE_LOG2 4u
#define MAX_CLASSIFICATION_VARIANTS 256u

// Offsets in the classification counter buffer:
// Offset 0: Total unbinned tile entries counter (uint)
#define CLASSIFICATION_OFFSET_TOTAL_ENTRIES 0u
// Offset 4: Start of per-variant tile counts (256 * 4 bytes = 1024 bytes)
#define CLASSIFICATION_OFFSET_VARIANT_COUNTS 4u
// Total counter buffer size: 4 + 256 * 4 = 1028 bytes
#define CLASSIFICATION_COUNTER_BUFFER_SIZE 1028u

// Indirect arguments layout: 16 bytes per variant (D3D12_DISPATCH_ARGUMENTS)
// Offset 0: ThreadGroupCountX (tileCount)
// Offset 4: ThreadGroupCountY (1)
// Offset 8: ThreadGroupCountZ (1)
// Offset 12: unused padding (0)
#define INDIRECT_ARGS_STRIDE 16u

struct UnbinnedTileEntry
{
    uint tileIndex;
    uint variantIndex;
};

static inline bool IsDeferredVariant(uint variantIndex, uint4 mask0, uint4 mask1)
{
    if (variantIndex < 128u)
    {
        uint elem = variantIndex >> 5u;
        uint bit = 1u << (variantIndex & 31u);
        uint val = (elem == 0u) ? mask0.x : ((elem == 1u) ? mask0.y : ((elem == 2u) ? mask0.z : mask0.w));
        return (val & bit) != 0u;
    }
    else if (variantIndex < 256u)
    {
        uint v = variantIndex - 128u;
        uint elem = v >> 5u;
        uint bit = 1u << (v & 31u);
        uint val = (elem == 0u) ? mask1.x : ((elem == 1u) ? mask1.y : ((elem == 2u) ? mask1.z : mask1.w));
        return (val & bit) != 0u;
    }
    return false;
}

static inline uint EncodeTileIndex(uint2 tileCoord, uint tilesPerRow)
{
    return tileCoord.y * tilesPerRow + tileCoord.x;
}

static inline uint2 DecodeTileCoord(uint tileIndex, uint tilesPerRow)
{
    return uint2(tileIndex % tilesPerRow, tileIndex / tilesPerRow);
}

static inline uint2 DecodeTilePixelCoord(uint tileIndex, uint2 inTileCoord, uint tilesPerRow)
{
    uint2 tileCoord = DecodeTileCoord(tileIndex, tilesPerRow);
    return tileCoord * CLASSIFICATION_TILE_SIZE + inTileCoord;
}

struct DeferredTexturingShaderProperties
{
    uint visBufferIndex;
    uint visibleMeshletsPass1;
    uint visibleMeshletsPass2;
    uint variantTileListIndex;
    uint tileOffsetsBufferIndex;
    uint tilesPerRow;
    uint renderWidth;
    uint renderHeight;
    uint gbuffer0Uav;
    uint gbuffer1Uav;
    uint gbuffer2Uav;
    uint gbuffer3Uav;
    uint variantIndex;
};

#endif // GHOST_CLASSIFICATION_COMMON_HLSL
