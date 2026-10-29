#ifndef GHOST_LIGHT_EVALUATION_HLSL
#define GHOST_LIGHT_EVALUATION_HLSL

#include "EngineResources/Shaders/Generated/GPULightData.hlsl"

struct PunctualLightVector
{
    float4 distance; // {d, d^2, 1/d, d_proj}
    float3 L;
};

// distances = {d, d^2, 1/d, d_proj}
void ModifyDistancesForFillLighting(inout float4 distances, float lightSqRadius)
{
    // Apply the sphere light hack to soften the core of the punctual light.
    // It is not physically plausible (using max() is more correct, but looks worse).
    // See https://www.desmos.com/calculator/otqhxunqhl
    // We only modify 1/d for performance reasons.
    float sqDist = distances.y;
    distances.z = rsqrt(sqDist + lightSqRadius); // Recompute 1/d
}

PunctualLightVector GetPunctualLightVector(float3 positionRWS, GPUPunctualLight light, float3 lightPositionRWS)
{
    PunctualLightVector vector = (PunctualLightVector)0;

    float3 lightToSurface = positionRWS - lightPositionRWS;
    float3 unL = -lightToSurface;
    float distSq = dot(unL, unL);
    float distRcp = rsqrt(distSq);
    float dist = distSq * distRcp;

    vector.L = unL * distRcp;
    float4 distance = float4(dist, distSq, distRcp, dot(lightToSurface, light.directionWS));

    ModifyDistancesForFillLighting(distance, light.size.x);
    vector.distance = distance;
    
    return vector;
}

float4 EvaluateLight_Punctual(GPUPunctualLight light, float3 L, float4 distance)
{
    float4 color = float4(light.color, 1.0f);

    float factor = distance.y * light.invRangeSq;
    float smoothFactor = saturate(1.0f - factor * factor);
    float attenuation = (smoothFactor * smoothFactor) / max(distance.y, 0.0001f);

    uint lightType = light.lightTypeAndShadowSize & 0xFu;
    if (lightType == GPULIGHT_TYPE_SPOT) // Spot = 1
    {
        // Spot cone attenuation
        float cosAngle = dot(-L, light.directionWS);
        float spotAtten = saturate(cosAngle * light.spotAngleScale + light.spotAngleOffset);
        attenuation *= spotAtten * spotAtten;
    }

    color.a = attenuation;

    // TODO: Height fog.

    return color;
}

#endif // GHOST_LIGHT_EVALUATION_HLSL
