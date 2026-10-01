#ifndef GHOST_LIT_TEMPLATE_COMMON_HLSL
#define GHOST_LIT_TEMPLATE_COMMON_HLSL

// ============================================================
// GhostEngine LitTemplate - Common Definitions
// ============================================================

#include "EngineResources/Shaders/Properties.hlsl"
#include "EngineResources/Shaders/MaterialPipeline/GBufferPacking.hlsl"
#include "EngineResources/Shaders/Material/Lit/Lit.hlsl"

#if !defined(GHOST_PASS_DEFERREDLIGHTING)
$GHOST_PROPERTIES_STRUCT$
#endif

$GHOST_USER_HLSL$

// ============================================================
// Injection point fallbacks (suppressed when user overrides)
// ============================================================

#if defined(GHOST_PASS_VISIBILITY)
#ifndef VBUFFER_STRATEGY
#define VBUFFER_STRATEGY DefaultVbufferStrategy
struct DefaultVbufferStrategy
{
    static DefaultVbufferStrategy Create()
    {
        return (DefaultVbufferStrategy) 0;
    }
    
    float GetAlphaCoverage(uint materialBufferIndex, float2 uv)
    {
        return 1.0f;
    }
};
#endif
#endif

#if defined(GHOST_PASS_DEFERREDTEXTURING)
#ifndef DEFERREDTEXTURING_STRATEGY
#define DEFERREDTEXTURING_STRATEGY DefaultDeferredTexturingStrategy
struct DefaultDeferredTexturingStrategy
{
    static DefaultDeferredTexturingStrategy Create()
    {
        return (DefaultDeferredTexturingStrategy)0;
    }
    
    SurfaceData GetSurfaceData(in MaterialContext ctx, in MaterialProperties props)
    {
        SurfaceData surface = (SurfaceData) 0;
        surface.materialFeatures = SHADING_MODEL_ID;
        surface.albedo = float3(0.73f, 0.73f, 0.73f);
        surface.normalWS = ctx.normalWS;
        surface.metallic = 0.0f;
        surface.roughness = 0.5f;
        surface.occlusion = 1.0f;
        surface.emissive = float3(0.0f, 0.0f, 0.0f);
        
        return surface;
    }
};
#endif
#endif

#if defined(GHOST_PASS_DEFERREDLIGHTING)
#ifndef DEFERREDLIGHTING_STRATEGY
#define DEFERREDLIGHTING_STRATEGY DefaultDeferredLightingStrategy
struct BSDFData
{
};

struct PreLightData
{
};

struct DefaultDeferredLightingStrategy
{
    static const uint ShadingModelID = 0u;

    static DefaultDeferredLightingStrategy Create()
    {
        return (DefaultDeferredLightingStrategy)0;
    }
    
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
        return (DirectLighting)0;
    }

    LightLoopOutput PostEvaluateBSDF(in AggregateLighting lighting, in BSDFData bsdf, in PreLightData preLightData, float3 V)
    {
        return (LightLoopOutput)0;
    }
};
#endif
#endif

#endif // GHOST_LIT_TEMPLATE_COMMON_HLSL
