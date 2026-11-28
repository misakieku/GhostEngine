#ifndef GHOST_UNLIT_TEMPLATE_COMMON_HLSL
#define GHOST_UNLIT_TEMPLATE_COMMON_HLSL

// ============================================================
// GhostEngine UnlitTemplate - Common Definitions
// ============================================================

#include "EngineResources/Shaders/Properties.hlsl"
#include "EngineResources/Shaders/MaterialContext.hlsl"

struct SurfaceData
{
    float3 albedo;
    float3 emissive;
};

#if defined(GHOST_PASS_TRANSPARENTPREPASS) || defined(GHOST_PASS_TRANSPARENTFORWARD)
struct TransparencyData
{
    float opacity;
    float3 transmittanceColor;
};
#endif

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

#if defined(GHOST_PASS_TRANSPARENTPREPASS) || defined(GHOST_PASS_TRANSPARENTFORWARD)
#ifndef TRANSPARENCY_STRATEGY
#define TRANSPARENCY_STRATEGY DefaultTransparencyStrategy
struct DefaultTransparencyStrategy
{
    static DefaultTransparencyStrategy Create()
    {
        return (DefaultTransparencyStrategy)0;
    }

    TransparencyData GetTransparencyData(MaterialContext ctx, MaterialProperties props)
    {
        TransparencyData data;
        data.opacity = 1.0f;
        data.transmittanceColor = float3(1.0f, 1.0f, 1.0f);
        return data;
    }
};
#endif
#endif

#if defined(GHOST_PASS_DEFERREDTEXTURING) || defined(GHOST_PASS_TRANSPARENTFORWARD)
#ifndef DEFERREDTEXTURING_STRATEGY
#define DEFERREDTEXTURING_STRATEGY DefaultDeferredTexturingStrategy
struct DefaultDeferredTexturingStrategy
{
    static DefaultDeferredTexturingStrategy Create()
    {
        return (DefaultDeferredTexturingStrategy)0;
    }

    SurfaceData GetSurfaceData(MaterialContext ctx, MaterialProperties props)
    {
        return (SurfaceData)0;
    }
};
#endif
#endif

#endif // GHOST_UNLIT_TEMPLATE_COMMON_HLSL
