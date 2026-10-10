#ifndef GHOST_GBUFFER_PACKING_HLSL
#define GHOST_GBUFFER_PACKING_HLSL

#include "EngineResources/Shaders/Common.hlsl"
#include "EngineResources/Shaders/Properties.hlsl"

// Octahedral normal encoding into [0, 1]^2
float2 OctahedralEncode(float3 n)
{
    n /= (abs(n.x) + abs(n.y) + abs(n.z));
    float2 oct = (n.z >= 0.0f) ? n.xy : ((1.0f - abs(n.yx)) * select(n.xy >= 0.0f, 1.0f, -1.0f));
    return oct * 0.5f + 0.5f;
}

// Octahedral normal decoding from [0, 1]^2 to normalized float3
float3 OctahedralDecode(float2 enc)
{
    float2 f = enc * 2.0f - 1.0f;
    float3 n = float3(f.x, f.y, 1.0f - abs(f.x) - abs(f.y));
    float t = saturate(-n.z);
    n.xy += select(n.xy >= 0.F, -t, t);
    return normalize(n);
}

// Build an orthonormal basis (b1, b2) given a normal vector n
void BuildOrthonormalBasis(float3 n, out float3 b1, out float3 b2)
{
    float sign = (n.z >= 0.0f) ? 1.0f : -1.0f;
    float a = -1.0f / (sign + n.z);
    float b = n.x * n.y * a;
    b1 = float3(1.0f + sign * n.x * n.x * a, sign * b, -sign * n.x);
    b2 = float3(b, sign + n.y * n.y * a, -n.y);
}

// Pack a tangent vector T into a scalar value in [0, 1] given the normal vector N
float PackTangentToScalar(float3 N, float3 T)
{
    float3 T0, B0;
    BuildOrthonormalBasis(N, T0, B0);

    T = normalize(T - N * dot(N, T));

    float angle = atan2(dot(T, B0), dot(T, T0));
    return (angle / 3.14159265f) * 0.5f + 0.5f;
}

// Unpack a tangent vector T from a scalar value in [0, 1] given the normal vector N
float3 UnpackTangentFromScalar(float3 N, float packedAngle)
{
    float3 T0, B0;
    BuildOrthonormalBasis(N, T0, B0);

    float angle = (packedAngle * 2.0f - 1.0f) * 3.14159265f;
    float sinA, cosA;
    sincos(angle, sinA, cosA);

    return normalize(cosA * T0 + sinA * B0);
}

struct GBufferOutputs
{
    float4 gbuffer0; // BaseColor (rgb) + Metallic (a)
    float4 gbuffer1; // Octahedral Normal (rg) + Roughness (b) + unused (a)
    float4 gbuffer2; // Occlusion (r) + ShadingModel (g) + FeatureBitmask (b, currently unused) + Tagent scaler (a)
    float3 gbuffer3; // Emissive (rgb)
    
    float3 GetBaseColor()
    {
        return gbuffer0.rgb;
    }
    
    void SetBaseColor(float3 baseColor)
    {
        gbuffer0.rgb = baseColor;
    }
    
    float GetMetallic()
    {
        return gbuffer0.a;
    }
    
    void SetMetallic(float metallic)
    {
        gbuffer0.a = metallic;
    }
    
    float3 GetNormal()
    {
        return OctahedralDecode(gbuffer1.rg);
    }
    
    void SetNormal(float3 normal)
    {
        gbuffer1.rg = OctahedralEncode(normal);
    }
    
    float GetRoughness()
    {
        return gbuffer1.b;
    }
    
    void SetRoughness(float roughness)
    {
        gbuffer1.b = roughness;
    }
    
    float GetOcclusion()
    {
        return gbuffer2.r;
    }
    
    void SetOcclusion(float occlusion)
    {
        gbuffer2.r = occlusion;
    }
    
    uint GetShadingModel()
    {
        return (uint)round(gbuffer2.g * 255.0f) & 0xFFu;
    }
    
    void SetShadingModel(uint shadingModel)
    {
        gbuffer2.g = (float) (shadingModel & 0xFFu) / 255.0f;
    }
    
    float3 GetTangent(float3 N)
    {
        return UnpackTangentFromScalar(N, gbuffer2.a);
    }
    
    void SetTangent(float3 N, float3 T)
    {
        gbuffer2.a = PackTangentToScalar(N, T);
    }
    
    float3 GetEmissive()
    {
        return gbuffer3;
    }
    
    void SetEmissive(float3 emissive)
    {
        gbuffer3 = emissive;
    }
};

void WriteGBuffer(uint2 pixelCoord, in GBufferOutputs outputs, uint gbuffer0Uav, uint gbuffer1Uav, uint gbuffer2Uav, uint gbuffer3Uav)
{
    RWTexture2D<float4> gb0 = ResourceDescriptorHeap[gbuffer0Uav];
    RWTexture2D<float4> gb1 = ResourceDescriptorHeap[gbuffer1Uav];
    RWTexture2D<float4> gb2 = ResourceDescriptorHeap[gbuffer2Uav];
    RWTexture2D<float3> gb3 = ResourceDescriptorHeap[gbuffer3Uav];

    gb0[pixelCoord] = outputs.gbuffer0;
    gb1[pixelCoord] = outputs.gbuffer1;
    gb2[pixelCoord] = outputs.gbuffer2;
    gb3[pixelCoord] = outputs.gbuffer3;
}

GBufferOutputs ReadGBuffer(uint2 pixelCoord, uint gbuffer0Srv, uint gbuffer1Srv, uint gbuffer2Srv, uint gbuffer3Srv)
{
    Texture2D<float4> gb0 = ResourceDescriptorHeap[gbuffer0Srv];
    Texture2D<float4> gb1 = ResourceDescriptorHeap[gbuffer1Srv];
    Texture2D<float4> gb2 = ResourceDescriptorHeap[gbuffer2Srv];
    Texture2D<float3> gb3 = ResourceDescriptorHeap[gbuffer3Srv];
    
    GBufferOutputs outputs;
    outputs.gbuffer0 = gb0[pixelCoord];
    outputs.gbuffer1 = gb1[pixelCoord];
    outputs.gbuffer2 = gb2[pixelCoord];
    outputs.gbuffer3 = gb3[pixelCoord];
    
    return outputs;
}

#endif // GHOST_GBUFFER_PACKING_HLSL
