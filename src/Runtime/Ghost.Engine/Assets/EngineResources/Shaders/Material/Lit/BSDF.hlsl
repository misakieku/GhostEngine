#ifndef GHOST_BSDF_HLSL
#define GHOST_BSDF_HLSL

#include "EngineResources/Shaders/Common.hlsl"
#include "EngineResources/Shaders/Utilities/Math.hlsl"

// Safely clamp NdotV to avoid division by zero or negative angles
float ClampNdotV(float NdotV)
{
    return max(NdotV, 0.0001f);
}

// Schlick approximation for Fresnel
// F(u) = f0 + (1 - f0) * (1 - u)^5
float3 F_Schlick(float3 f0, float u)
{
    float x = saturate(1.0f - u);
    float x2 = x * x;
    return f0 + (1.0f - f0) * (x2 * x2 * x);
}

float F_Schlick(float f0, float u)
{
    float x = saturate(1.0f - u);
    float x2 = x * x;
    return f0 + (1.0f - f0) * (x2 * x2 * x);
}

// Precomputes part of lambdaV for height-correlated Smith-Joint GGX
// lambdaV_part = sqrt((-NdotV * a2 + NdotV) * NdotV + a2)
float GetSmithJointGGXPartLambdaV(float NdotV, float roughness)
{
    float a2 = roughness * roughness;
    return sqrt((-NdotV * a2 + NdotV) * NdotV + a2);
}

// Combined D * V for height-correlated Smith-Joint GGX
// V = G / (4 * NdotL * NdotV)
// Ref: Heitz 2014, "Understanding the Masking-Shadowing Function in Microfacet-Based BRDFs"
float DV_SmithJointGGX(float NdotH, float NdotL, float NdotV, float roughness, float partLambdaV)
{
    float a2 = roughness * roughness;
    float s = (NdotH * a2 - NdotH) * NdotH + 1.0f;

    float lambdaV = NdotL * partLambdaV;
    float lambdaL = NdotV * sqrt((-NdotL * a2 + NdotL) * NdotL + a2);

    float2 D = float2(a2, s * s);
    float2 G = float2(1.0f, lambdaV + lambdaL);

    return INV_PI * 0.5f * (D.x * G.x) / max(D.y * G.y, 1e-7f);
}

// Disney Burley diffuse (normalized for energy conservation)
// Ref: Burley 2012, "Physically-Based Shading at Disney"
// Uses identity: 2 * (LdotH)^2 = 1 + LdotV to eliminate LdotH evaluation
float DisneyDiffuseNoPI(float NdotV, float NdotL, float LdotV, float perceptualRoughness)
{
    float fd90 = 0.5f + (perceptualRoughness + perceptualRoughness * LdotV);
    float xL = 1.0f - NdotL;
    float xL2 = xL * xL;
    float lightScatter = 1.0f + (fd90 - 1.0f) * (xL2 * xL2 * xL);

    float xV = 1.0f - NdotV;
    float xV2 = xV * xV;
    float viewScatter = 1.0f + (fd90 - 1.0f) * (xV2 * xV2 * xV);

    // Normalization factor rcp(1.03571) = 0.96552f to prevent energy gain at grazing angles
    return 0.96552f * (lightScatter * viewScatter);
}

float DisneyDiffuse(float NdotV, float NdotL, float LdotV, float perceptualRoughness)
{
    return INV_PI * DisneyDiffuseNoPI(NdotV, NdotL, LdotV, perceptualRoughness);
}

#endif // GHOST_BSDF_HLSL
