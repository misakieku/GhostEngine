#ifndef GHOST_VISIBILITY_BUFFER_ENCODING_HLSL
#define GHOST_VISIBILITY_BUFFER_ENCODING_HLSL

#define VBUFFER_DEPTH_SHIFT 32u
#define VBUFFER_PRIMITIVE_ID_SHIFT 24u
#define VBUFFER_INSTANCE_INDEX_MASK 0x00FFFFFFu
#define VBUFFER_PRIMITIVE_ID_MASK 0xFFu

static inline uint PackVisibilityPayload(uint instanceIndex, uint primitiveID)
{
    return (instanceIndex & VBUFFER_INSTANCE_INDEX_MASK) | ((primitiveID & VBUFFER_PRIMITIVE_ID_MASK) << VBUFFER_PRIMITIVE_ID_SHIFT);
}

static inline uint64_t PackVisibility64(float depth, uint instanceIndex, uint primitiveID)
{
    uint64_t depthInt = (uint64_t)asuint(depth);
    uint64_t payload = (uint64_t)PackVisibilityPayload(instanceIndex, primitiveID);
    return (depthInt << VBUFFER_DEPTH_SHIFT) | payload;
}

static inline void UnpackVisibility64(uint64_t val, out float depth, out uint instanceIndex, out uint primitiveID)
{
    depth = asfloat((uint)(val >> VBUFFER_DEPTH_SHIFT));
    uint payload = (uint)(val & 0xFFFFFFFFu);
    instanceIndex = payload & VBUFFER_INSTANCE_INDEX_MASK;
    primitiveID = (payload >> VBUFFER_PRIMITIVE_ID_SHIFT) & VBUFFER_PRIMITIVE_ID_MASK;
}

#define VBUFFER_TILE_SIZE 8u
#define VBUFFER_TILE_SIZE_LOG2 3u

static inline uint Morton2D_3Bits(uint2 coord)
{
    uint x = coord.x & 7u;
    uint y = coord.y & 7u;

    x = (x | (x << 2u)) & 0x13u;
    x = (x | (x << 1u)) & 0x15u;

    y = (y | (y << 2u)) & 0x13u;
    y = (y | (y << 1u)) & 0x15u;

    return x | (y << 1u);
}

static inline uint ComputePixelByteAddress(uint2 pixelCoord, uint renderWidth)
{
    uint tilesPerRow = (renderWidth + (VBUFFER_TILE_SIZE - 1u)) >> VBUFFER_TILE_SIZE_LOG2;
    uint2 tileCoord = pixelCoord >> VBUFFER_TILE_SIZE_LOG2;
    uint2 localCoord = pixelCoord & (VBUFFER_TILE_SIZE - 1u);

    uint tileIndex = tileCoord.y * tilesPerRow + tileCoord.x;
    uint localMorton = Morton2D_3Bits(localCoord);

    return (tileIndex << 9u) | (localMorton << 3u);
}

static inline uint ComputePixelDepthByteAddress(uint2 pixelCoord, uint renderWidth)
{
    return ComputePixelByteAddress(pixelCoord, renderWidth) + 4u;
}

#endif // GHOST_VISIBILITY_BUFFER_ENCODING_HLSL
