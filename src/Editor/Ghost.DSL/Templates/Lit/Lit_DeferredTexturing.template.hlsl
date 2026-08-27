// ============================================================
// GhostEngine LitTemplate - Deferred Texturing Pass (Compute)
// ============================================================

#include "Lit_Common.template.hlsl"

[numthreads(8, 8, 1)]
void CSMain(uint3 dispatchThreadID : SV_DispatchThreadID)
{
    // Minimal placeholder compute kernel for deferred material surface evaluation
    Payload payload = (Payload)0;
    MaterialContext ctx = (MaterialContext)0;
    ctx.materialIndex = g_PushConstantData.propertiesBuffer;

    SurfaceData surface;
    GetSurfaceData(ctx, payload, surface);
}
