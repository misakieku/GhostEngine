#ifndef BUILTIN_PROPERTIES_HLSL
#define BUILTIN_PROPERTIES_HLSL

#include "F:/csharp/GhostEngine/src/Runtime/Ghost.Graphics/Shaders/Includes/Common.hlsl"

struct PushConstantData
{
    uint globalIndex;
    uint viewIndex;
    uint objectIndex;
    uint instanceIndex;
    uint materialIndex;
};

struct GlobalFrameData
{
    uint viewBufferIndex;
    uint instanceBufferIndex;
    uint viewBufferCount;
    uint instanceBufferCount;
    uint userBufferIndex;
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

struct PerInstanceData
{
    float4x4 localToWorld;
};

struct PerObjectData
{
    float3 worldBoundsMin;
    BYTE_ADDRESS_BUFFER vertexBuffer;
    float3 worldBoundsMax;
    BYTE_ADDRESS_BUFFER indexBuffer;
    BYTE_ADDRESS_BUFFER meshletBuffer;
    BYTE_ADDRESS_BUFFER meshletVerticesBuffer;
    BYTE_ADDRESS_BUFFER meshletTrianglesBuffer;
};

PushConstantData g_PushConstantData : register(b0);

#endif // BUILTIN_PROPERTIES_HLSL
