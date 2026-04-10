#ifndef GHOST_PROPERTIES_HLSL
#define GHOST_PROPERTIES_HLSL

#include "F:/csharp/GhostEngine/src/Runtime/Ghost.Graphics/Shaders/Includes/Common.hlsl"

#define GLOBAL_BINDLESS_SIG \
    "RootFlags(ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT | " \
    "CBV_SRV_UAV_HEAP_DIRECTLY_INDEXED | " \
    "SAMPLER_HEAP_DIRECTLY_INDEXED), " \
    "RootConstants(num32BitConstants=3, b0, space=0)"

// TODO: This should be auto generated to match the c# side.

struct GraphicsPushConstantData
{
    BYTE_ADDRESS_BUFFER frameBuffer;
    BYTE_ADDRESS_BUFFER viewBuffer;
    uint instanceIndex;
};

struct ComputePushConstantData
{
    BYTE_ADDRESS_BUFFER frameBuffer;
    BYTE_ADDRESS_BUFFER viewBuffer;
    BYTE_ADDRESS_BUFFER propertiesBuffer;
};

struct FrameData
{
    BYTE_ADDRESS_BUFFER instanceBuffer;
    BYTE_ADDRESS_BUFFER userBuffer;
};

struct ViewData
{
    float4x4 viewMatrix;
    float4x4 projectionMatrix;
    float3 cameraPosition;
    float nearClip;
    float3 cameraDirection;
    float farClip;
    float4 screenSize; // xy: size, zw: 1/size
};

struct InstanceData
{
    float4x4 localToWorld;
    BYTE_ADDRESS_BUFFER meshBuffer;
    BYTE_ADDRESS_BUFFER materialBuffer;
};

struct MeshData
{
    float3 worldBoundsMin;
    BYTE_ADDRESS_BUFFER vertexBuffer;
    float3 worldBoundsMax;
    BYTE_ADDRESS_BUFFER indexBuffer;
    
    BYTE_ADDRESS_BUFFER meshletBuffer;
    BYTE_ADDRESS_BUFFER meshletVerticesBuffer;
    BYTE_ADDRESS_BUFFER meshletTrianglesBuffer;
};

#if define(__GRAPHICS__)
GraphicsPushConstantData g_PushConstantData : register(b0);
#elif defined(__COMPUTE__)
ComputePushConstantData g_PushConstantData : register(b0);
#elif defined(__WORK_GRAPH__)
#define WorkGraphPushConstantData ComputePushConstantData
WorkGraphPushConstantData g_PushConstantData : register(b0);
#endif

#endif // GHOST_PROPERTIES_HLSL
