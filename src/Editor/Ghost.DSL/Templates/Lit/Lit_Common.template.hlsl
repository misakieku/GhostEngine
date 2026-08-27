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
//   void  GetSurfaceData(in MaterialContext ctx, inout Payload payload, out SurfaceData surface)
//   float GetAlphaCoverage(uint materialIndex, float2 uv, inout Payload payload)
// ============================================================

#include "EngineResources/Shaders/Includes/Common.hlsl"
#include "EngineResources/Shaders/Includes/Properties.hlsl"

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
static inline void GetSurfaceData(in MaterialContext ctx, inout Payload payload, out SurfaceData surface)
{
    LitShaderProperties props = LoadLitProperties(ctx.materialIndex);

    surface = (SurfaceData)0;
    surface.albedo = props.baseColor.rgb;

    if (props.baseMap != 0)
    {
        surface.albedo *= SAMPLE_TEXTURE2D(props.baseMap, props.sampler_baseMap, ctx.uv).rgb;
    }

    surface.normalWS = ctx.normalWS;
    surface.metallic = props.metallic;
    surface.roughness = props.roughness;
    surface.occlusion = props.occlusion;
}
#endif

#ifndef GHOST_OVERRIDE_GET_ALPHA_COVERAGE
static inline float GetAlphaCoverage(uint materialIndex, float2 uv, inout Payload payload)
{
    return 1.0f;
}
#endif

#endif // GHOST_LIT_TEMPLATE_COMMON_HLSL
