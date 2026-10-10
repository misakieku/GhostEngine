#ifndef GHOST_LIT_HLSL
#define GHOST_LIT_HLSL

#include "EngineResources/Shaders/MaterialPipeline/GBufferPacking.hlsl"

struct SurfaceData
{
    uint shadingModel;
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
    float2 ddx_uv;
    float2 ddy_uv;
};

struct ShadingContext
{
    float3 positionWS;
    float3 normalWS;
    float2 positionNDC;
    uint2 positionSS;
    uint2 tileCoord;
    float depth;
    float linearDepth;
    uint shadowAtlasIndex;
    uint shadowViewsBufferIndex;
    uint shadowIndicesBufferIndex;
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
    surface.albedo = gbuffer.GetBaseColor();
    surface.shadingModel = gbuffer.GetMetallic();
    surface.normalWS = gbuffer.GetNormal();
    surface.roughness = gbuffer.GetRoughness();
    surface.metallic = gbuffer.GetMetallic();
    surface.occlusion = gbuffer.GetOcclusion();
    surface.emissive = gbuffer.GetEmissive();
    return surface;
}

#endif // GHOST_LIT_HLSL
