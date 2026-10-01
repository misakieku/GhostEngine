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
};

$GHOST_PROPERTIES_STRUCT$

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
        SurfaceData surface = (SurfaceData)0;
        return surface;
    }
};
#endif
#endif

#endif // GHOST_UNLIT_TEMPLATE_COMMON_HLSL
