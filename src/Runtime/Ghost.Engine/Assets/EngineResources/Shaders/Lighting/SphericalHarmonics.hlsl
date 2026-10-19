#ifndef GHOST_SPHERICAL_HARMONICS_HLSL
#define GHOST_SPHERICAL_HARMONICS_HLSL

#include "EngineResources/Shaders/Common.hlsl"

// ----------------------------------------------------------------------------
// Packed L2 Spherical Harmonics (ShaderX 2 / Sloan polynomial packing)
// Matches GPUPersistentViewState.skyIrradianceSH (7x float4 = 112 bytes)
// ----------------------------------------------------------------------------
struct PackedSH9
{
    float4 shAr;
    float4 shAg;
    float4 shAb;
    float4 shBr;
    float4 shBg;
    float4 shBb;
    float4 shC;
};

// Rotates a direction vector around the world Y-axis.
// sinCos.x = sin(theta), sinCos.y = cos(theta)
float3 RotateDirectionY(float3 dir, float2 sinCos)
{
    return float3(
        dir.x * sinCos.y + dir.z * sinCos.x,
        dir.y,
        -dir.x * sinCos.x + dir.z * sinCos.y
    );
}

// Evaluates diffuse irradiance using fast dot products (ShaderX 2 / Peter-Pike Sloan).
// Expects coefficients pre-convolved with clamped cosine lobe and pre-divided by PI.
float3 EvaluateSH9Irradiance(PackedSH9 sh, float3 N)
{
    float4 vA = float4(N, 1.0f);
    float3 x1;
    x1.r = dot(sh.shAr, vA);
    x1.g = dot(sh.shAg, vA);
    x1.b = dot(sh.shAb, vA);

    float4 vB = N.xyzz * N.yzzx;
    float3 x2;
    x2.r = dot(sh.shBr, vB);
    x2.g = dot(sh.shBg, vB);
    x2.b = dot(sh.shBb, vB);

    float vC = N.x * N.x - N.y * N.y;
    float3 x3 = sh.shC.rgb * vC;

    return max(0.0f, x1 + x2 + x3);
}

// Loads a view's precomputed PackedSH9 from the PersistentViewStateBuffer.
PackedSH9 LoadViewSH9(BYTE_ADDRESS_BUFFER persistentViewStateBuffer, uint viewId)
{
    ByteAddressBuffer buf = GET_BUFFER(persistentViewStateBuffer);
    uint baseOffset = viewId * 256; // 256 bytes per view state
    PackedSH9 sh;
    sh.shAr = asfloat(buf.Load4(baseOffset + 0));
    sh.shAg = asfloat(buf.Load4(baseOffset + 16));
    sh.shAb = asfloat(buf.Load4(baseOffset + 32));
    sh.shBr = asfloat(buf.Load4(baseOffset + 48));
    sh.shBg = asfloat(buf.Load4(baseOffset + 64));
    sh.shBb = asfloat(buf.Load4(baseOffset + 80));
    sh.shC  = asfloat(buf.Load4(baseOffset + 96));
    return sh;
}

// Stores a computed PackedSH9 into the PersistentViewStateBuffer UAV.
void StoreViewSH9(RWByteAddressBuffer buf, uint viewId, PackedSH9 sh)
{
    uint baseOffset = viewId * 256; // 256 bytes per view state
    buf.Store4(baseOffset + 0, asuint(sh.shAr));
    buf.Store4(baseOffset + 16, asuint(sh.shAg));
    buf.Store4(baseOffset + 32, asuint(sh.shAb));
    buf.Store4(baseOffset + 48, asuint(sh.shBr));
    buf.Store4(baseOffset + 64, asuint(sh.shBg));
    buf.Store4(baseOffset + 80, asuint(sh.shBb));
    buf.Store4(baseOffset + 96, asuint(sh.shC));
}

#endif // GHOST_SPHERICAL_HARMONICS_HLSL
