#ifndef GHOST_SHADOWRASTERIZER_HLSL
#define GHOST_SHADOWRASTERIZER_HLSL

#include "EngineResources/Shaders/Properties.hlsl"
#include "EngineResources/Shaders/Mesh.hlsl"
#include "EngineResources/Shaders/Utilities/CullCommon.hlsl"
#include "EngineResources/Shaders/MaterialPipeline/MaterialEncoding.hlsl"
#include "EngineResources/Shaders/Generated/GPULightData.hlsl"

struct ShadowPixelInput
{
    float4 position : SV_POSITION;
    float4 clipDistances : SV_ClipDistance0;
#if SHADOW_PIXEL_STAGE
    float2 uv : TEXCOORD0;
    nointerpolation uint materialBufferIndex : CBUFFER_ID;
#endif
};

struct ShadowPrimitiveOutput
{
    bool cullPrim : SV_CullPrimitive;
};

#define SHADOW_MS_THREADS 64

struct SharedShadowMeshletState
{
    float4x4 worldViewProj;
    float4 tileOffsetScale;
    uint pageBufferIndex;
    uint pageBase;
    uint vertexOffset;
    uint triangleOffset;
    uint vertexCount;
    uint triangleCount;
    uint materialBufferIndex;
    float doubleSided;
};

groupshared SharedShadowMeshletState s_ShadowMeshlet;
groupshared float4 g_VertexPositions[MAX_VERTICES_PER_MESHLET];

[numthreads(SHADOW_MS_THREADS, 1, 1)]
[outputtopology("triangle")]
void MSMain(
    uint groupID : SV_GroupID,
    uint groupThreadID : SV_GroupThreadID,
    out vertices ShadowPixelInput outVerts[MAX_VERTICES_PER_MESHLET],
    out indices uint3 outTris[MAX_TRIANGLES_PER_MESHLET],
    out primitives ShadowPrimitiveOutput outPrims[MAX_TRIANGLES_PER_MESHLET])
{
    if (groupThreadID == 0)
    {
        STRUCT_BUFFER visibleBufferIndex = g_PushConstantData.userData0;
        uint shadowViewsBufferSrv = g_PushConstantData.userData1;
        BYTE_ADDRESS_BUFFER binOffsetsIndex = g_PushConstantData.userData2;
        uint targetVariantIndex = g_PushConstantData.userData3 >> 1u;

        uint binnedSlot = groupID;
        if (IS_VALID_BUFFER(binOffsetsIndex))
        {
            ByteAddressBuffer binOffsetsBuffer = GET_BUFFER(binOffsetsIndex);
            uint binStartOffset = binOffsetsBuffer.Load(targetVariantIndex * 4u);
            binnedSlot = binStartOffset + groupID;
        }

        StructuredBuffer<UnbinnedMeshletEntry> visibleMeshlets = GET_BUFFER(visibleBufferIndex);
        UnbinnedMeshletEntry visible = visibleMeshlets[binnedSlot];

        uint shadowViewIndex = visible.variantIndex & 0xFFFFu;
        GPUShadowSamplingData shadowCull = LoadDataAtOffset<GPUShadowSamplingData>(shadowViewsBufferSrv, sizeof(GPUShadowViewData) * shadowViewIndex + sizeof(GPUShadowCullingData));

        InstanceData instanceData = LoadData<InstanceData>(g_FrameData.sceneBuffer, visible.instanceIndex);
        MeshData meshData = LoadData<MeshData>(instanceData.meshBuffer, 0);
        Meshlet meshlet = LoadMeshlet(visible.meshletIndex, meshData);

        uint vertexCount = meshlet.packedCounts & 0xFFu;
        uint triangleCount = (meshlet.packedCounts >> 8) & 0xFFu;

#if SHADOW_PIXEL_STAGE
        uint localMaterialIndex = (meshlet.packedCounts >> 16) & 0xFFu;
        uint packedMaterial = LoadMaterialBindlessIndex(g_FrameData.paletteOffsetBuffer, g_FrameData.materialIndexBuffer, instanceData.materialPaletteIndex, localMaterialIndex);
        uint materialBufferIndex = UnpackMaterialByteOffset(packedMaterial);
#else
        uint materialBufferIndex = 0u;
#endif

        ByteAddressBuffer matBuf = GET_BUFFER(g_FrameData.materialBuffer);
        float doubleSided = asfloat(matBuf.Load(materialBufferIndex + 12u));

        float4x4 localToRWS = ApplyCameraTranslationToMatrix(instanceData.localToWorld);
        float4x4 worldViewProj = mul(shadowCull.shadowViewProj, localToRWS);

        BYTE_ADDRESS_BUFFER pageBufferIndex;
        uint pageBase;
        GetMeshletPageBufferAndBase(meshData, meshlet.pageIndex, pageBufferIndex, pageBase);

        s_ShadowMeshlet.worldViewProj = worldViewProj;
        s_ShadowMeshlet.tileOffsetScale = shadowCull.tileOffsetScale;
        s_ShadowMeshlet.pageBufferIndex = pageBufferIndex;
        s_ShadowMeshlet.pageBase = pageBase;
        s_ShadowMeshlet.vertexOffset = meshlet.vertexOffset;
        s_ShadowMeshlet.triangleOffset = meshlet.triangleOffset;
        s_ShadowMeshlet.vertexCount = vertexCount;
        s_ShadowMeshlet.triangleCount = triangleCount;
        s_ShadowMeshlet.materialBufferIndex = materialBufferIndex;
        s_ShadowMeshlet.doubleSided = doubleSided;
    }

    GroupMemoryBarrierWithGroupSync();

    SetMeshOutputCounts(s_ShadowMeshlet.vertexCount, s_ShadowMeshlet.triangleCount);

    if (groupThreadID < s_ShadowMeshlet.vertexCount)
    {
        ByteAddressBuffer pageBuffer = GET_BUFFER(s_ShadowMeshlet.pageBufferIndex);
        uint vOffset = s_ShadowMeshlet.pageBase + s_ShadowMeshlet.vertexOffset + groupThreadID * PACKED_VERTEX_STRIDE;
        float3 pos = asfloat(pageBuffer.Load3(vOffset));

        float4 origClipPos = mul(s_ShadowMeshlet.worldViewProj, float4(pos, 1.0f));

        outVerts[groupThreadID].clipDistances = float4(
            origClipPos.w + origClipPos.x, // Left
            origClipPos.w - origClipPos.x, // Right
            origClipPos.w + origClipPos.y, // Bottom
            origClipPos.w - origClipPos.y  // Top
        );

        float4 clipPos = origClipPos;
        clipPos.x = clipPos.x * s_ShadowMeshlet.tileOffsetScale.z + (2.0f * s_ShadowMeshlet.tileOffsetScale.x + s_ShadowMeshlet.tileOffsetScale.z - 1.0f) * clipPos.w;
        clipPos.y = clipPos.y * s_ShadowMeshlet.tileOffsetScale.w + (1.0f - (2.0f * s_ShadowMeshlet.tileOffsetScale.y + s_ShadowMeshlet.tileOffsetScale.w)) * clipPos.w;

        g_VertexPositions[groupThreadID] = clipPos;
        outVerts[groupThreadID].position = clipPos;

#if SHADOW_PIXEL_STAGE
        outVerts[groupThreadID].uv = UnpackHalf2(pageBuffer.Load(vOffset + 16u));
        outVerts[groupThreadID].materialBufferIndex = s_ShadowMeshlet.materialBufferIndex;
#endif
    }

    GroupMemoryBarrierWithGroupSync();

    [unroll(2)]
    for (uint primId = groupThreadID; primId < s_ShadowMeshlet.triangleCount; primId += SHADOW_MS_THREADS)
    {
        ByteAddressBuffer pageBuffer = GET_BUFFER(s_ShadowMeshlet.pageBufferIndex);
        uint packedIndices = pageBuffer.Load(s_ShadowMeshlet.pageBase + s_ShadowMeshlet.triangleOffset + primId * PACKED_TRIANGLE_STRIDE);
        uint3 indices = uint3(packedIndices & 0xFF, (packedIndices >> 8) & 0xFF, (packedIndices >> 16) & 0xFF);

        float4 v0 = g_VertexPositions[indices.x];
        float4 v1 = g_VertexPositions[indices.y];
        float4 v2 = g_VertexPositions[indices.z];

        outTris[primId] = indices;

        bool isCulled = false;
        if (s_ShadowMeshlet.doubleSided == 0.0f)
        {
            isCulled = !IsTriangleFrontFacing(v0, v1, v2);
            if (!isCulled)
            {
                isCulled = IsTriangleOutsideFrustum(v0, v1, v2);
            }
        }

        outPrims[primId].cullPrim = isCulled;
    }
}

#if SHADOW_PIXEL_STAGE
void PSMain(ShadowPixelInput input)
{
    uint targetVariantIndex = g_PushConstantData.userData3 >> 1u;
    if (targetVariantIndex > 0u)
    {
        ByteAddressBuffer matBuf = GET_BUFFER(g_FrameData.materialBuffer);
        uint alphaClip = matBuf.Load(input.materialBufferIndex + 16u);
        if (alphaClip != 0u)
        {
            float alphaClipThreshold = asfloat(matBuf.Load(input.materialBufferIndex + 20u));
            ALPHA_STRATEGY strategy = ALPHA_STRATEGY::Create();
            float coverage = strategy.GetAlphaCoverage(input.materialBufferIndex, input.uv);
            if (coverage < alphaClipThreshold)
            {
                discard;
            }
        }
    }
}
#endif

#endif // GHOST_SHADOWRASTERIZER_HLSL
