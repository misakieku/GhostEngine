#ifndef GHOST_CLASSIFICATION_COMMON_HLSL
#define GHOST_CLASSIFICATION_COMMON_HLSL

#define CLASSIFICATION_TILE_SIZE 16u
#define CLASSIFICATION_TILE_SIZE_LOG2 4u
#define MAX_CLASSIFICATION_VARIANTS 64u

// Counter structure: 8 bytes per variant
// Offset 0: tileCount (uint)
// Offset 4: pixelCount (uint)
#define VARIANT_COUNTER_STRIDE 8u

// Indirect arguments layout: 16 bytes per variant
// Offset 0: ThreadGroupCountX (tileCount)
// Offset 4: ThreadGroupCountY (1)
// Offset 8: ThreadGroupCountZ (1)
// Offset 12: unused padding (0)
#define INDIRECT_ARGS_STRIDE 16u

static inline bool IsDeferredVariant(uint variantIndex, uint2 deferredMask)
{
    if (variantIndex < 32u)
    {
        return (deferredMask.x & (1u << variantIndex)) != 0u;
    }
    else if (variantIndex < 64u)
    {
        return (deferredMask.y & (1u << (variantIndex - 32u))) != 0u;
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
    uint maxTilesPerVariant;
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
