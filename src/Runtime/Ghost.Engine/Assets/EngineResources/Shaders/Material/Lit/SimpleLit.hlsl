#ifndef GHOST_SIMPLELIT_HLSL
#define GHOST_SIMPLELIT_HLSL

#include "EngineResources/Shaders/Material/Lit/Lit.hlsl"

#ifndef DEFERREDLIGHTING_STRATEGY
#define DEFERREDLIGHTING_STRATEGY SimpleLitDeferredLighting
struct BSDFData
{
    uint materialFeatures;
    float3 diffuseColor;
    float3 fresnel0;
    float ambientOcclusion;
    float specularOcclusion;
    float3 normalWS;
    float perceptualRoughness;
    float3 tangentWS;
    float3 bitangentWS;
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

    BSDFData GetBSDFData(in MaterialContext ctx, in SurfaceData surface)
    {
        BSDFData bsdf = (BSDFData)0;
        bsdf.materialFeatures = surface.materialFeatures;
        bsdf.diffuseColor = surface.albedo;
        bsdf.fresnel0 = lerp(float3(0.04f, 0.04f, 0.04f), surface.albedo, surface.metallic);
        bsdf.ambientOcclusion = surface.occlusion;
        bsdf.specularOcclusion = 1.0f;
        bsdf.normalWS = surface.normalWS;
        bsdf.perceptualRoughness = surface.roughness;
        bsdf.tangentWS = ctx.tangentWS.xyz;
        bsdf.bitangentWS = cross(surface.normalWS, ctx.tangentWS.xyz) * sign(ctx.tangentWS.w);
        bsdf.emissive = surface.emissive;
    
        return bsdf;
    }

    PreLightData GetPreLightData(in ShadingContext ctx, float3 V, inout BSDFData bsdf)
    {
        PreLightData data = (PreLightData)0;
        data.NdotV = max(0.0f, dot(bsdf.normalWS, V));
        return data;
    }

    DirectLighting EvaluateDirectLighting(in BSDFData bsdf, in PreLightData preLightData, float3 V, float3 L, float3 lightRadiance)
    {
        float NdotL = max(0.0f, dot(bsdf.normalWS, L));
        float3 H = normalize(L + V);
        float NdotH = max(0.0f, dot(bsdf.normalWS, H));
        float shininess = exp2(10.0f * (1.0f - bsdf.perceptualRoughness) + 1.0f);
        float spec = pow(NdotH, shininess) * ((shininess + 2.0f) / 8.0f);

        DirectLighting direct;

        direct.diffuse = bsdf.diffuseColor * NdotL * lightRadiance;
        direct.specular = float3(0.04f, 0.04f, 0.04f) * spec * NdotL * lightRadiance;
        return direct;
    }

    LightLoopOutput PostEvaluateBSDF(in AggregateLighting lighting, in BSDFData bsdf, in PreLightData preLightData, float3 V)
    {
        LightLoopOutput output;
        output.diffuse = lighting.direct.diffuse + bsdf.emissive; // + ambient lighting
        output.specular = lighting.direct.specular + lighting.indirect.specularReflected;
        return output;
    }
};
#endif // DEFERREDLIGHTING_STRATEGY

#endif // GHOST_SIMPLELIT_HLSL

