#ifndef GHOST_UNLIT_TEMPLATE_COMMON_HLSL
#define GHOST_UNLIT_TEMPLATE_COMMON_HLSL

// ============================================================
// GhostEngine UnlitTemplate - Common Definitions
// ============================================================

#include "EngineResources/Shaders/Properties.hlsl"

struct SurfaceData
{
    float3 albedo;
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

$GHOST_PROPERTIES_STRUCT$

$GHOST_USER_HLSL$

// ============================================================
// Injection point fallbacks (suppressed when user overrides)
// ============================================================

#if defined(GHOST_PASS_VISIBILITY) || defined(GHOST_PASS_SHADOW)
#ifndef ALPHA_STRATEGY
#define ALPHA_STRATEGY DefaultAlphaStrategy
struct DefaultAlphaStrategy
{
    static DefaultAlphaStrategy Create()
    {
        return (DefaultAlphaStrategy) 0;
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
        SurfaceData surface = (SurfaceData)0;
        return surface;
    }
};
#endif
#endif

#endif // GHOST_UNLIT_TEMPLATE_COMMON_HLSL
