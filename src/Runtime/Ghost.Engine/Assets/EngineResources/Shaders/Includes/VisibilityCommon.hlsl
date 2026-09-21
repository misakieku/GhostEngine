#ifndef GHOST_VISIBILITY_COMMON_HLSL
#define GHOST_VISIBILITY_COMMON_HLSL

#include "Properties.hlsl"
#include "CullCommon.hlsl"
#include "MaterialEncoding.hlsl"
#include "VisibilityBufferEncoding.hlsl"

#define VISIBILITY_MS_THREADS 64

struct VisibilityPixelInput
{
    float4 position : SV_POSITION;
    float2 uv : TEXCOORD0;
    nointerpolation uint instanceIndex : INSTANCE_ID;
    nointerpolation uint localMaterialIndex : MATERIAL_ID;
    nointerpolation uint cbufferIndex : CBUFFER_ID;
    nointerpolation uint variantIndex : VARIANT_ID;
};

struct VisibilityPrimitiveOutput
{
    uint primitiveID : SV_PrimitiveID;
};

// Speculative early-Z test via non-atomic 64-bit load
static inline bool VisibilitySpeculativeEarlyZ(
    uint2 pixelCoord,
    float depth,
    uint visBufferIndex,
    out uint byteAddress,
    out uint64_t currentPacked)
{
    uint renderWidth = (uint)g_ViewData.screenSize.x;
    byteAddress = ComputePixelByteAddress(pixelCoord, renderWidth);

    RWByteAddressBuffer visBuffer = ResourceDescriptorHeap[visBufferIndex];
    currentPacked = visBuffer.Load<uint64_t>(byteAddress);

    uint currentDepthInt = (uint)(currentPacked >> VBUFFER_DEPTH_SHIFT);
    uint depthInt = asuint(depth);

    // In Reversed-Z, a closer fragment has a strictly greater depth integer
    return (depthInt > currentDepthInt);
}

// Writes visibility buffer entry via 64-bit atomic max
static inline void VisibilityWritePixelAtomic(
    uint visBufferIndex,
    uint byteAddress,
    float depth,
    uint instanceIndex,
    uint primitiveID)
{
    RWByteAddressBuffer visBuffer = ResourceDescriptorHeap[visBufferIndex];
    uint64_t newPacked = PackVisibility64(depth, instanceIndex, primitiveID);
    visBuffer.InterlockedMax64(byteAddress, newPacked);
}

#endif // GHOST_VISIBILITY_COMMON_HLSL
