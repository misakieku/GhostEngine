#ifndef GHOST_UNLIT_TEMPLATE_COMMON_HLSL
#define GHOST_UNLIT_TEMPLATE_COMMON_HLSL

// ============================================================
// GhostEngine UnlitTemplate - Common Definitions
// ============================================================
// Stitched into every pass of an Unlit template shader. Defines
// the flat properties struct, the Payload struct, and default
// injection-point fallbacks.
//
// Material properties are resolved on the GPU: the amplification
// shader resolves the bindless cbuffer index via the palette
// indirection (FrameData -> InstanceData -> Meshlet) and forwards
// it through the mesh pipeline to the pixel stage.
//
// Injection points (user overrides in their hlsl block):
//   float  GetAlphaCoverage(uint materialIndex, float2 uv, inout Payload payload)
//   float4 GetColor(uint materialIndex, float2 uv, inout Payload payload)
// ============================================================

#include "EngineResources/Shaders/Properties.hlsl"

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

#ifndef GHOST_OVERRIDE_GET_COLOR
static inline float4 GetColor(in MaterialProperties props, float2 uv, inout Payload payload)
{
    return 1.0f;
}
#endif

#endif // GHOST_UNLIT_TEMPLATE_COMMON_HLSL
