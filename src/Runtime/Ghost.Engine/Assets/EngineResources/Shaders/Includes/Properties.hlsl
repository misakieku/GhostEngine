#ifndef GHOST_PROPERTIES_HLSL
#define GHOST_PROPERTIES_HLSL

#include "Common.hlsl"

#define GLOBAL_BINDLESS_SIG \
    "RootFlags(ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT | " \
    "CBV_SRV_UAV_HEAP_DIRECTLY_INDEXED | " \
    "SAMPLER_HEAP_DIRECTLY_INDEXED), " \
    "RootConstants(num32BitConstants=3, b0, space=0)"

// TODO: This should be auto generated to match the c# side.

struct Frustum
{
    float4 planes[6];
    float3 corners[8];
};

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
    BYTE_ADDRESS_BUFFER paletteOffsetBuffer;   // global PaletteOffsetBuffer
    BYTE_ADDRESS_BUFFER materialIndexBuffer;   // global MaterialIndexBuffer
};

struct ViewData
{
    float4x4 viewMatrix;
    float4x4 projectionMatrix;
    float4x4 viewProjectionMatrix;
    float4x4 preVPMatrix;
    float3 cameraPosition;
    float nearClip;
    float3 cameraDirection;
    float farClip;
    float4 screenSize; // xy: size, zw: 1/size
    Frustum frustum;
};

struct InstanceData
{
    float4x4 localToWorld;
    BYTE_ADDRESS_BUFFER meshBuffer;
    uint materialPaletteIndex;  // index into PaletteOffsetBuffer
    uint pad0;
    uint pad1;
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
    BYTE_ADDRESS_BUFFER meshletGroupBuffer;
    BYTE_ADDRESS_BUFFER meshletHierarchyBuffer;
    uint meshletCount;
    uint meshletGroupCount;
    uint lodLevelCount;
    uint materialSlotCount;
};

#if defined(__WORK_GRAPH__)
#define WorkGraphPushConstantData ComputePushConstantData
#endif

cbuffer PushConstants : register(b0)
{
#if defined(__GRAPHICS__)
    GraphicsPushConstantData g_PushConstantData;
#elif defined(__COMPUTE__)
    ComputePushConstantData g_PushConstantData;
#elif defined(__WORK_GRAPH__)
    WorkGraphPushConstantData g_PushConstantData;
#endif
};


#endif // GHOST_PROPERTIES_HLSL
