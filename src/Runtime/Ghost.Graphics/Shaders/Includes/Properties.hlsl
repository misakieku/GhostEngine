#ifndef BUILTIN_PROPERTIES_HLSL
#define BUILTIN_PROPERTIES_HLSL

#include "F:/csharp/GhostEngine/src/Runtime//Ghost.Graphics/Shaders/Includes/Common.hlsl"

struct PushConstantData
{
    BYTE_ADDRESS_BUFFER globalBuffer;
    BYTE_ADDRESS_BUFFER perViewBuffer;
    BYTE_ADDRESS_BUFFER perObjectBuffer;
    BYTE_ADDRESS_BUFFER perMaterialBuffer;
};

struct PerViewData
{
    float4x4 viewMatrix;
    float4x4 projectionMatrix;
    float3 cameraPosition;
    float nearClip;
    float3 cameraDirection;
    float farClip;
    float4 screenSize; // xy: size, zw: 1/size
};

struct PerObjectData
{
    float4x4 localToWorld;
    float3 worldBoundsMin;
    BYTE_ADDRESS_BUFFER vertexBuffer;
    float3 worldBoundsMax;
    BYTE_ADDRESS_BUFFER indexBuffer;
};

PushConstantData g_PushConstantData : register(b0);

#endif // BUILTIN_PROPERTIES_HLSL
