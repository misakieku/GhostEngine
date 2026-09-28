#ifndef GHOST_LIT_TEMPLATE_COMMON_HLSL
#define GHOST_LIT_TEMPLATE_COMMON_HLSL

// ============================================================
// GhostEngine LitTemplate - Common Definitions
// ============================================================
// TODO: This Lit template is a placeholder for testing and framework validation only.
// As the GPU-driven rendering pipeline (V-Buffer, compute deferred texturing, G-Buffer layout,
// clustered lighting) continues to evolve, this template will be fully expanded.
//
// Injection points:
//   float GetAlphaCoverage(uint materialIndex, float2 uv, inout Payload payload)
//   SurfaceData GetSurfaceData(in MaterialContext ctx, in MaterialProperties props, inout Payload payload)
// ============================================================

#include "EngineResources/Shaders/Properties.hlsl"
#include "EngineResources/Shaders/MaterialPipeline/GBufferPacking.hlsl"
#include "EngineResources/Shaders/Material/Lit/Lit.hlsl"

struct MaterialContext
{
    uint instanceIndex;
    uint materialIndex;
    float3 worldPos;
    float3 normalWS;
    float4 tangentWS;
    float2 uv;
};

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

struct BSDFData
{
    uint materialFeatures;
    float3 diffuseColor;
    float3 fresnel0;
    float ambientOcclusion;
    float specularOcclusion;
    float3 normalWS;
    float perceptualRoughness;
    float3 tangentWS;
    float3 bitangentWS;
    float roughnessT;
    float roughnessB;
    float3 emissive;
};

$GHOST_PROPERTIES_STRUCT$

$GHOST_PAYLOAD_STRUCT$

$GHOST_USER_HLSL$

// ============================================================
// Injection point fallbacks (suppressed when user overrides)
// ============================================================

#ifndef GHOST_OVERRIDE_GET_ALPHA_COVERAGE
static inline float GetAlphaCoverage(in MaterialProperties props, float2 uv, inout Payload payload)
{
    return 1.0f;
}
#endif

#ifndef GHOST_OVERRIDE_GET_SURFACE_DATA
static inline SurfaceData GetSurfaceData(in MaterialContext ctx, in MaterialProperties props, inout Payload payload)
{
    SurfaceData surface = (SurfaceData)0;
    surface.materialFeatures = props.materialFeatureMask;
    surface.albedo = float3(0.73f, 0.73f, 0.73f);
    surface.normalWS = ctx.normalWS;
    surface.metallic = 0.0f;
    surface.roughness = 0.5f;
    surface.occlusion = 1.0f;
    surface.emissive = float3(0.0f, 0.0f, 0.0f);
    
    return surface;
}
#endif

static inline SurfaceData ExtractSurfaceData(in GBufferOutputs gbuffer)
{
    SurfaceData surface = (SurfaceData)0;
    surface.albedo = gbuffer.gbuffer0.rgb;
    surface.materialFeatures = asuint(gbuffer.gbuffer0.a);
    surface.normalWS = OctahedralDecode(gbuffer.gbuffer1.rg);
    surface.roughness = gbuffer.gbuffer1.b;
    surface.metallic = gbuffer.gbuffer1.a;
    surface.occlusion = gbuffer.gbuffer2.b;
    surface.emissive = gbuffer.gbuffer3.rgb;
    return surface;
}

static inline BSDFData GetBSDFData(in MaterialContext ctx, in SurfaceData surface)
{
    BSDFData bsdf = (BSDFData)0;
    bsdf.materialFeatures = surface.materialFeatures;
    bsdf.diffuseColor = surface.albedo;
    bsdf.fresnel0 = lerp(float3(0.04f, 0.04f, 0.04f), surface.albedo, surface.metallic);
    bsdf.ambientOcclusion = surface.occlusion;
    bsdf.specularOcclusion = 1.0f;
    bsdf.normalWS = surface.normalWS;
    bsdf.perceptualRoughness = sqrt(max(0.001f, surface.roughness));
    bsdf.tangentWS = ctx.tangentWS.xyz;
    bsdf.bitangentWS = cross(surface.normalWS, ctx.tangentWS.xyz) * sign(ctx.tangentWS.w);
    bsdf.emissive = surface.emissive;
    
    return bsdf;
}

#ifndef GHOST_OVERRIDE_EVALUATE_BSDF
static inline DirectLighting EvaluateDirectLighting(in BSDFData bsdf, float3 V, float3 L, float3 lightRadiance)
{
    DirectLighting direct = (DirectLighting)0;
    float NdotL = max(0.0f, dot(bsdf.normalWS, L));
    direct.diffuse = bsdf.diffuseColor * NdotL * lightRadiance;
    return direct;
}

static inline LightLoopOutput PostEvaluateBSDF(in AggregateLighting lighting, in BSDFData bsdf, float3 V)
{
    LightLoopOutput output = (LightLoopOutput)0;
    output.diffuse = lighting.direct.diffuse + bsdf.emissive; // + ambient lighting
    output.specular = lighting.direct.specular + lighting.indirect.specularReflected;
    return output;
}
#endif

#endif // GHOST_LIT_TEMPLATE_COMMON_HLSL
