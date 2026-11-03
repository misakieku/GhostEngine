#ifndef GHOST_MATERIALCONTEXT_HLSL
#define GHOST_MATERIALCONTEXT_HLSL

struct MaterialContext
{
    uint instanceIndex;
    uint materialIndex;
    float3 positionRWS;
    float3 normalWS;
    float4 tangentWS;
    float2 uv;
    float2 ddx_uv;
    float2 ddy_uv;
    bool isFrontFace;
};

#endif // GHOST_MATERIALCONTEXT_HLSL
