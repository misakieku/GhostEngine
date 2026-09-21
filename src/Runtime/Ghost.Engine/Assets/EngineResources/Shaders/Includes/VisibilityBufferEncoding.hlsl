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

static inline uint ComputePixelByteAddress(uint2 pixelCoord, uint renderWidth)
{
    return (pixelCoord.y * renderWidth + pixelCoord.x) * 8u;
}

#endif // GHOST_VISIBILITY_BUFFER_ENCODING_HLSL
