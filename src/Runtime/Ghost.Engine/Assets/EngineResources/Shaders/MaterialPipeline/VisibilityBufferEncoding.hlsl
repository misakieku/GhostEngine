#ifndef GHOST_VISIBILITY_BUFFER_ENCODING_HLSL
#define GHOST_VISIBILITY_BUFFER_ENCODING_HLSL

#define VBUFFER_DEPTH_SHIFT 32u
#define VBUFFER_PRIMITIVE_ID_SHIFT 24u
#define VBUFFER_INSTANCE_INDEX_MASK 0x00FFFFFFu
#define VBUFFER_PRIMITIVE_ID_MASK 0xFFu

uint PackVisibilityPayload(uint visibleMeshletIndex, uint primitiveID)
{
    return (visibleMeshletIndex & VBUFFER_INSTANCE_INDEX_MASK) | ((primitiveID & VBUFFER_PRIMITIVE_ID_MASK) << VBUFFER_PRIMITIVE_ID_SHIFT);
}

uint64_t PackVisibility64(float depth, uint visibleMeshletIndex, uint primitiveID)
{
    uint64_t depthInt = (uint64_t)asuint(depth);
    uint64_t payload = (uint64_t)PackVisibilityPayload(visibleMeshletIndex, primitiveID);
    return (depthInt << VBUFFER_DEPTH_SHIFT) | payload;
}

void UnpackVisibility64(uint64_t val, out float depth, out uint visibleMeshletIndex, out uint primitiveID)
{
    depth = asfloat((uint)(val >> VBUFFER_DEPTH_SHIFT));
    uint payload = (uint)(val & 0xFFFFFFFFu);
    visibleMeshletIndex = payload & VBUFFER_INSTANCE_INDEX_MASK;
    primitiveID = (payload >> VBUFFER_PRIMITIVE_ID_SHIFT) & VBUFFER_PRIMITIVE_ID_MASK;
}

void UnpackVisibility2(uint2 raw, out float depth, out uint visibleMeshletIndex, out uint primitiveID)
{
    depth = asfloat(raw.y);
    uint payload = raw.x;
    visibleMeshletIndex = payload & VBUFFER_INSTANCE_INDEX_MASK;
    primitiveID = (payload >> VBUFFER_PRIMITIVE_ID_SHIFT) & VBUFFER_PRIMITIVE_ID_MASK;
}

#endif // GHOST_VISIBILITY_BUFFER_ENCODING_HLSL
