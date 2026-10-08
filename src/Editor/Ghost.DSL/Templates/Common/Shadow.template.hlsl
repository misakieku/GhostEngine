// ============================================================
// GhostEngine Unified Shadow Pass (Lit / Unlit)
// Multi-view hardware mesh shader with linear clip-space viewport
// remapping and alpha clipping support via ALPHA_STRATEGY.
// ============================================================

#ifndef GHOST_TEMPLATE_SHADOW
#define GHOST_TEMPLATE_SHADOW

#if defined(GHOST_TEMPLATE_LIT)
#include "Lit/Lit_Common.template.hlsl"
#elif defined(GHOST_TEMPLATE_UNLIT)
#include "Unlit/Unlit_Common.template.hlsl"
#else
#error "Unsupported template type for shadow."
#endif

#define SHADOW_PIXEL_STAGE 1
#include "EngineResources/Shaders/Lighting/ShadowRaster.hlsl"

#endif // GHOST_TEMPLATE_SHADOW
