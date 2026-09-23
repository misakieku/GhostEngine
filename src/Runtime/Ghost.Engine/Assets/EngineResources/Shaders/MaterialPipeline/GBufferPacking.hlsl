#ifndef GHOST_GBUFFER_PACKING_HLSL
#define GHOST_GBUFFER_PACKING_HLSL

#include "EngineResources/Shaders/Common.hlsl"
#include "EngineResources/Shaders/Properties.hlsl"

// Octahedral normal encoding into [0, 1]^2
static inline float2 OctahedralEncode(float3 n)
{
    n /= (abs(n.x) + abs(n.y) + abs(n.z));
    float2 oct = (n.z >= 0.0f) ? n.xy : ((1.0f - abs(n.yx)) * select(n.xy >= 0.0f, 1.0f, -1.0f));
    return oct * 0.5f + 0.5f;
}

// Octahedral normal decoding from [0, 1]^2 to normalized float3
static inline float3 OctahedralDecode(float2 enc)
{
    float2 f = enc * 2.0f - 1.0f;
    float3 n = float3(f.x, f.y, 1.0f - abs(f.x) - abs(f.y));
    float t = saturate(-n.z);
    n.xy += select(n.xy >= 0.F, -t, t);
    return normalize(n);
}

struct GBufferOutputs
{
    float4 gbuffer0; // Albedo (rgb) + ShadingModel/Flags (a)
    float4 gbuffer1; // Octahedral Normal (rg) + Roughness (b) + Metallic (a)
    float4 gbuffer2; // Motion Vectors (xy) + Occlusion (z) + FeatureBits (w)
    float4 gbuffer3; // Emissive (rgb) + UnlitFlag (a)
};

static inline void WriteGBuffer(uint2 pixelCoord, in GBufferOutputs outputs, uint gbuffer0Uav, uint gbuffer1Uav, uint gbuffer2Uav, uint gbuffer3Uav)
{
    RWTexture2D<float4> gb0 = ResourceDescriptorHeap[gbuffer0Uav];
    RWTexture2D<float4> gb1 = ResourceDescriptorHeap[gbuffer1Uav];
    RWTexture2D<float4> gb2 = ResourceDescriptorHeap[gbuffer2Uav];
    RWTexture2D<float4> gb3 = ResourceDescriptorHeap[gbuffer3Uav];

    gb0[pixelCoord] = outputs.gbuffer0;
    gb1[pixelCoord] = outputs.gbuffer1;
    gb2[pixelCoord] = outputs.gbuffer2;
    gb3[pixelCoord] = outputs.gbuffer3;
}

#endif // GHOST_GBUFFER_PACKING_HLSL
