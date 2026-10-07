#ifndef GHOST_PROPERTIES_HLSL
#define GHOST_PROPERTIES_HLSL

#include "Common.hlsl"

#define GLOBAL_BINDLESS_SIG \
    "RootFlags(ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT | " \
    "CBV_SRV_UAV_HEAP_DIRECTLY_INDEXED | " \
    "SAMPLER_HEAP_DIRECTLY_INDEXED), " \
    "RootConstants(num32BitConstants=4, b0, space=0), " \
    "CBV(b1, space=0, flags=DATA_STATIC_WHILE_SET_AT_EXECUTE), " \
    "CBV(b2, space=0, flags=DATA_STATIC_WHILE_SET_AT_EXECUTE)"

// TODO: This should be auto generated to match the c# side.

struct Frustum
{
    float4 planes[6];
    float4 corners[8];
};

struct PushConstantData
{
    uint userData0;
    uint userData1;
    uint userData2;
    uint userData3;
};

struct FrameData
{
    BYTE_ADDRESS_BUFFER sceneBuffer;
    BYTE_ADDRESS_BUFFER paletteOffsetBuffer;          // global PaletteOffsetBuffer
    BYTE_ADDRESS_BUFFER materialIndexBuffer;          // global MaterialIndexBuffer
    BYTE_ADDRESS_BUFFER materialBuffer;               // global MaterialPoolBuffer
    BYTE_ADDRESS_BUFFER punctualLightsBuffer;         // global GPUPunctualLight buffer
    uint punctualLightCount;                          // number of punctual lights
    BYTE_ADDRESS_BUFFER directionalLightBuffer;       // global GPUDirectionalLight buffer
    uint directionalLightCount;                       // number of directional lights
    int primaryDirectionalLightIndex;                 // index of primary shadow-casting directional light (-1 if none)
    uint pad0;
    uint pad1;
    uint pad2;
};

struct PunctualLightData
{
    float3 positionWS;
    float  range;               // Attenuation cutoff radius (meters) — directly used for culling bounding sphere

    float3 color;               // Linear RGB * intensity
    uint   lightTypeAndFlags;   // bits 0..3: type (0=Point, 1=Spot), bits 4..31: flags

    float3 directionWS;         // Spot forward direction (normalized)
    float  spotAngleScale;      // 1.0 / max(0.001, cosInner - cosOuter)

    float  spotAngleOffset;     // -cosOuter * spotAngleScale
    float  invRangeSq;          // 1.0 / (range * range) for fast attenuation
    float  sourceRadius;        // GGX normalization / contact shadow radius
    float normalBias;
    float depthBias;
};

struct DirectionalLightData
{
    float3 directionWS;
    uint   castShadows;
    float3 color;
    float  shadowBiasMultiplier;
    float4 cascadeSplits;       // View-space Z split planes for 4 CSM cascades
    float4x4 shadowMatrices[4]; // 4 CSM view-projection matrices
};

struct ShadowViewData
{
    float4x4 shadowViewProj;
    float4   planes[6];
    float4   tileOffsetScale;   // xy: offset [0..1], zw: scale [0..1]
    float3   lightPositionWS;
    float    lodErrorThreshold;
    float    proj11;
    float    tileSize;
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

cbuffer PushConstants : register(b0)
{
    PushConstantData g_PushConstantData;
};

cbuffer cbViewData : register(b1)
{
    ViewData g_ViewData;
};

cbuffer cbFrameData : register(b2)
{
    FrameData g_FrameData;
};

template<typename T>
T LoadMaterialData(uint byteOffset)
{
    ByteAddressBuffer buf = GET_BUFFER(g_FrameData.materialBuffer);
    return buf.Load<T>(byteOffset);
}

#endif // GHOST_PROPERTIES_HLSL
