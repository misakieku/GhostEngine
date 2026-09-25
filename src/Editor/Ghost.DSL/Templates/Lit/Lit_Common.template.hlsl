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
//   void  GetSurfaceData(in MaterialContext ctx, in MaterialProperties props, inout Payload payload, out SurfaceData surface)
//   float GetAlphaCoverage(uint materialIndex, float2 uv, inout Payload payload)
// ============================================================

#include "EngineResources/Shaders/Properties.hlsl"

struct SurfaceData
{
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
    float3 worldPos;
    float3 normalWS;
    float2 uv;
};

$GHOST_PROPERTIES_STRUCT$

$GHOST_PAYLOAD_STRUCT$

static inline LitShaderProperties LoadLitProperties(uint materialBindlessIndex)
{
    return LoadData<LitShaderProperties>(materialBindlessIndex, 0);
}

$GHOST_USER_HLSL$

// ============================================================
// Injection point fallbacks (suppressed when user overrides)
// ============================================================

#ifndef GHOST_OVERRIDE_GET_SURFACE_DATA
static inline void GetSurfaceData(in MaterialContext ctx, in MaterialProperties props, inout Payload payload, out SurfaceData surface)
{
    surface = (SurfaceData)0;
    surface.albedo = float3(0.73f, 0.73f, 0.73f);
    surface.normalWS = ctx.normalWS;
    surface.metallic = 0.0f;
    surface.roughness = 0.5f;
    surface.occlusion = 1.0f;
    surface.emissive = float3(0.0f, 0.0f, 0.0f);
}
#endif

#ifndef GHOST_OVERRIDE_GET_ALPHA_COVERAGE
static inline float GetAlphaCoverage(in MaterialProperties props, float2 uv, inout Payload payload)
{
    return 1.0f;
}
#endif

#endif // GHOST_LIT_TEMPLATE_COMMON_HLSL
