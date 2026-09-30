#ifndef GHOST_LIT_HLSL
#define GHOST_LIT_HLSL

#include "EngineResources/Shaders/MaterialPipeline/GBufferPacking.hlsl"

struct SurfaceData
{
    uint materialFeatures;
    float3 albedo;
    float3 normalWS;
    float metallic;
    float roughness;
    float occlusion;
    float3 emissive;
};

struct MaterialContext
{
    uint instanceIndex;
    uint materialIndex;
    float3 positionWS;
    float3 normalWS;
    float4 tangentWS;
    float2 uv;
};

struct ShadingContext
{
    float3 positionWS;
    float2 positionNDC;
    uint2 positionSS;
    uint2 tileCoord;
    float depth;
    float linearDepth;
};

struct DirectLighting
{
    float3 diffuse;
    float3 specular;
};

struct IndirectLighting
{
    float3 specularReflected;
    float3 specularTransmitted;
};

struct AggregateLighting
{
    DirectLighting direct;
    IndirectLighting indirect;
};

struct LightLoopOutput
{
    float3 diffuse;
    float3 specular;
};

void AccumulateDirectLighting(inout AggregateLighting total, in DirectLighting direct)
{
    total.direct.diffuse += direct.diffuse;
    total.direct.specular += direct.specular;
}

void AccumulateIndirectLighting(inout AggregateLighting total, in IndirectLighting indirect)
{
    total.indirect.specularReflected += indirect.specularReflected;
    total.indirect.specularTransmitted += indirect.specularTransmitted;
}

SurfaceData ExtractSurfaceData(in GBufferOutputs gbuffer)
{
    SurfaceData surface = (SurfaceData)0;
    surface.albedo = gbuffer.gbuffer0.rgb;
    surface.materialFeatures = (uint)(gbuffer.gbuffer0.a * 255.0f);
    surface.normalWS = OctahedralDecode(gbuffer.gbuffer1.rg);
    surface.roughness = gbuffer.gbuffer1.b;
    surface.metallic = gbuffer.gbuffer1.a;
    surface.occlusion = gbuffer.gbuffer2.b;
    surface.emissive = gbuffer.gbuffer3.rgb;
    return surface;
}

#endif // GHOST_LIT_HLSL
