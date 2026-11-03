#ifndef GHOST_MATERIALUTILITY_HLSL
#define GHOST_MATERIALUTILITY_HLSL

// Flips ctx.normalWS (tangentToWorld[2]) towards the viewer for double-sided materials
void ApplyDoubleSidedFlipOrMirror(inout MaterialContext ctx, float4 doubleSidedConstants)
{
    if (doubleSidedConstants.w > 0.0f && !ctx.isFrontFace)
    {
        ctx.normalWS *= doubleSidedConstants.z;
    }
}

// Transforms a Tangent-Space normal map to World Space with double-sided Flip/Mirror support
float3 TransformTangentToWorldDoubleSided(float3 normalTS, MaterialContext ctx, float4 doubleSidedConstants)
{
    if (doubleSidedConstants.w > 0.0f && !ctx.isFrontFace)
    {
        normalTS.xy *= doubleSidedConstants.x;
    }

    float3 N = ctx.normalWS; // Already flipped by ApplyDoubleSidedFlipOrMirror
    float3 T = ctx.tangentWS.xyz;
    float3 B = cross(N, T) * ctx.tangentWS.w;
    return normalize(normalTS.x * T + normalTS.y * B + normalTS.z * N);
}

#endif // GHOST_MATERIALUTILITY_HLSL
