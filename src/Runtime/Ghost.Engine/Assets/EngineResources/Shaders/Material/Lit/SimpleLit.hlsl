#ifndef GHOST_SIMPLELIT_HLSL
#define GHOST_SIMPLELIT_HLSL

#include "EngineResources/Shaders/Material/Lit/Lit.hlsl"

struct BSDFData
{
    uint shadingModel;
    float3 diffuseColor;
    float3 fresnel0;
    float ambientOcclusion;
    float specularOcclusion;
    float3 normalWS;
    float perceptualRoughness;
    float3 tangentWS;
    float roughnessT;
    float roughnessB;
    float3 emissive;
};

struct PreLightData
{
    float NdotV;
};

struct SimpleLitDeferredLighting
{
    static const uint ShadingModelID = 1u;

    static SimpleLitDeferredLighting Create()
    {
        return (SimpleLitDeferredLighting)0;
    }

    BSDFData GetBSDFData(float3 positionRWS, float3 T, SurfaceData surface)
    {
        BSDFData bsdf = (BSDFData)0;
        bsdf.shadingModel = surface.shadingModel;
        bsdf.diffuseColor = surface.albedo;
        bsdf.fresnel0 = lerp(float3(0.04f, 0.04f, 0.04f), surface.albedo, surface.metallic);
        bsdf.ambientOcclusion = surface.occlusion;
        bsdf.specularOcclusion = 1.0f;
        bsdf.normalWS = surface.normalWS;
        bsdf.perceptualRoughness = surface.roughness;
        bsdf.tangentWS = T;
        bsdf.emissive = surface.emissive;

        return bsdf;
    }

    PreLightData GetPreLightData(ShadingContext ctx, float3 V, inout BSDFData bsdf)
    {
        PreLightData data = (PreLightData)0;
        data.NdotV = max(0.0f, dot(bsdf.normalWS, V));
        return data;
    }

    bool IsNonZeroBSDF(float3 V, float3 L, BSDFData bsdf)
    {
        float NdotL = dot(bsdf.normalWS, L);
        return NdotL > 0.0;
    }

    void ClampRoughness(inout BSDFData bsdf, float minRoughness)
    {
        bsdf.roughnessT = max(minRoughness, bsdf.roughnessT);
        bsdf.roughnessB = max(minRoughness, bsdf.roughnessB);
    }

    DirectLighting EvaluateDirectLighting(BSDFData bsdf, PreLightData preLightData, float3 V, float3 L, float3 lightColor)
    {
        float NdotL = max(0.0f, dot(bsdf.normalWS, L));
        float3 H = normalize(L + V);
        float NdotH = max(0.0f, dot(bsdf.normalWS, H));
        float shininess = exp2(10.0f * (1.0f - bsdf.perceptualRoughness) + 1.0f);
        float spec = pow(NdotH, shininess) * ((shininess + 2.0f) / 8.0f);

        DirectLighting direct;

        direct.diffuse = bsdf.diffuseColor * NdotL * lightColor;
        direct.specular = float3(0.04f, 0.04f, 0.04f) * spec * NdotL * lightColor;
        return direct;
    }

    LightLoopOutput PostEvaluateBSDF(AggregateLighting lighting, BSDFData bsdf, PreLightData preLightData, float3 V)
    {
        LightLoopOutput output;
        output.diffuse = lighting.direct.diffuse + bsdf.emissive; // + ambient lighting
        output.specular = lighting.direct.specular + lighting.indirect.specularReflected;
        return output;
    }
};

#endif // GHOST_SIMPLELIT_HLSL

