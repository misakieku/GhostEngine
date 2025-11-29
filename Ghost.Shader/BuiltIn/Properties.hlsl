#ifndef BUILTIN_PROPERTIES_HLSL
#define BUILTIN_PROPERTIES_HLSL

#include "F:/csharp/GhostEngine/Ghost.Shader/BuiltIn/Common.hlsl"

struct PerViewData
{
    float4x4 cameraMatrix;
    float4x4 cameraInverseMatrix;
    float4 screenSize; // xy = size, zw = 1/size
};

struct PerObjectData
{
    float4x4 localToWorld;
    float3 worldBoundsMin;
    BYTE_ADDRESS_BUFFER vertexBuffer;
    float3 worldBoundsMax;
    BYTE_ADDRESS_BUFFER indexBuffer;
};

cbuffer GlobalConstants : register(b0)
{
    GlobalData g_GlobalData;
};

cbuffer PerViewConstants : register(b1)
{
    PerViewData g_PerViewData;
};

cbuffer PerObjectConstants : register(b2)
{
    PerObjectData g_PerObjectData;
};

cbuffer PerMaterialConstants : register(b3)
{
    PerMaterialData g_PerMaterialData;
};

#if defined(USE_TRADITIONAL_BINDLESS)
Texture2D GlobalTexture2DHeap[GLOBAL_TEXTURE2D_HEAP_SIZE] : register(t0);
Texture3D GlobalTexture3DHeap[GLOBAL_TEXTURE3D_HEAP_SIZE] : register(t0);
TextureCube GlobalTextureCubeHeap[GLOBAL_TEXTURECUBE_HEAP_SIZE] : register(t0);
Texture2DArray GlobalTexture2DArrayHeap[GLOBAL_TEXTURE2D_ARRAY_HEAP_SIZE] : register(t0);
TextureCubeArray GlobalTextureCubeArrayHeap[GLOBAL_TEXTURECUBE_ARRAY_HEAP_SIZE] : register(t0);
SamplerState GlobalSamplerHeap[GLOBAL_SAMPLER_HEAP_SIZE] : register(s0);
#endif

#endif // BUILTIN_PROPERTIES_HLSL