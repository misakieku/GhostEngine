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

$GHOST_PROPERTIES_STRUCT$

$GHOST_PAYLOAD_STRUCT$

$GHOST_USER_HLSL$

// ============================================================
// Injection point fallbacks (suppressed when user overrides)
// ============================================================

#ifndef GHOST_OVERRIDE_GET_ALPHA_COVERAGE
float GetAlphaCoverage(in MaterialProperties props, float2 uv, inout Payload payload)
{
    return 1.0f;
}
#endif

#ifndef GHOST_OVERRIDE_GET_SURFACE_DATA
SurfaceData GetSurfaceData(in MaterialContext ctx, in MaterialProperties props, inout Payload payload)
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

#ifndef GHOST_OVERRIDE_EVALUATE_BSDF

BSDFData GetBSDFData(in MaterialContext ctx, in SurfaceData surface)
{
    return (BSDFData)0;
}

PreLightData GetPreLightData(in ShadingContext ctx, float3 V, inout BSDFData bsdf)
{
    return (PreLightData)0;
}

DirectLighting EvaluateDirectLighting(in BSDFData bsdf, in PreLightData preLightData, float3 V, float3 L, float3 lightRadiance)
{
    DirectLighting direct = (DirectLighting)0;
    float NdotL = max(0.0f, dot(bsdf.normalWS, L));
    direct.diffuse = bsdf.diffuseColor * NdotL * lightRadiance;
    return direct;
}

LightLoopOutput PostEvaluateBSDF(in AggregateLighting lighting, in BSDFData bsdf, in PreLightData preLightData, float3 V)
{
    LightLoopOutput output = (LightLoopOutput)0;
    output.diffuse = lighting.direct.diffuse + bsdf.emissive; // + ambient lighting
    output.specular = lighting.direct.specular + lighting.indirect.specularReflected;
    return output;
}
#endif

#endif // GHOST_LIT_TEMPLATE_COMMON_HLSL
