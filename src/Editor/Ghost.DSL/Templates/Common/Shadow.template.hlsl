// ============================================================
// GhostEngine Unified Shadow Pass (Lit / Unlit)
// Multi-view hardware mesh shader with linear clip-space viewport
// remapping and alpha clipping support via ALPHA_STRATEGY.
// ============================================================

#ifndef GHOST_TEMPLATE_SHADOW
#define GHOST_TEMPLATE_SHADOW

#if defined(GHOST_TEMPLATE_LIT)
#include "Lit/Lit_Common.template.hlsl"
#elif defined(GHOST_TEMPLATE_UNLIT)
#include "Unlit/Unlit_Common.template.hlsl"
#else
#error "Unsupported template type for shadow."
#endif

#include "EngineResources/Shaders/Mesh.hlsl"
#include "EngineResources/Shaders/Properties.hlsl"
#include "EngineResources/Shaders/Utilities/CullCommon.hlsl"
#include "EngineResources/Shaders/MaterialPipeline/MaterialEncoding.hlsl"

struct ShadowPixelInput
{
    float4 position : SV_POSITION;
    float2 uv : TEXCOORD0;
    nointerpolation uint materialBufferIndex : CBUFFER_ID;
    nointerpolation float4 tileMinMaxPixels : TILE_BOUNDS; // xy = min pixel, zw = max pixel
};

struct ShadowPrimitiveOutput
{
    bool cullPrim : SV_CullPrimitive;
};

bool IsFrontFacingAndVisible(float4 h0, float4 h1, float4 h2, float subpixelThreshold = 0.0f)
{
    if (min(h0.w, min(h1.w, h2.w)) <= 0.0f)
    {
        return true;
    }

    float2 e0 = h1.xy * h0.w - h0.xy * h1.w;
    float2 e1 = h2.xy * h0.w - h0.xy * h2.w;

    float crossProduct = e1.x * e0.y - e1.y * e0.x;

    return crossProduct > subpixelThreshold;
}

#define SHADOW_MS_THREADS 64

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
    uint visibleBufferIndex = g_PushConstantData.userData0;
    uint shadowViewsBufferSrv = g_PushConstantData.userData1;
    uint binOffsetsIndex = g_PushConstantData.userData2;
    uint targetVariantIndex = g_PushConstantData.userData3 >> 1u;

    uint binnedSlot = groupID;
    if (IS_VALID_BUFFER(binOffsetsIndex))
    {
        ByteAddressBuffer binOffsetsBuffer = ResourceDescriptorHeap[binOffsetsIndex];
        uint binStartOffset = binOffsetsBuffer.Load(targetVariantIndex * 4u);
        binnedSlot = binStartOffset + groupID;
    }

    StructuredBuffer<UnbinnedMeshletEntry> visibleMeshlets = ResourceDescriptorHeap[visibleBufferIndex];
    UnbinnedMeshletEntry visible = visibleMeshlets[binnedSlot];

    uint shadowViewIndex = visible.variantIndex & 0xFFFFu;
    ShadowViewData shadowView = LoadData<ShadowViewData>(shadowViewsBufferSrv, shadowViewIndex);

    InstanceData instanceData = LoadData<InstanceData>(g_FrameData.sceneBuffer, visible.instanceIndex);
    MeshData meshData = LoadData<MeshData>(instanceData.meshBuffer, 0);
    Meshlet meshlet = LoadMeshlet(visible.meshletIndex, meshData);

    uint vertexCount = meshlet.packedCounts & 0xFFu;
    uint triangleCount = (meshlet.packedCounts >> 8) & 0xFFu;
    uint localMaterialIndex = (meshlet.packedCounts >> 16) & 0xFFu;

    uint packedMaterial = LoadMaterialBindlessIndex(g_FrameData.paletteOffsetBuffer, g_FrameData.materialIndexBuffer, instanceData.materialPaletteIndex, localMaterialIndex);
    uint materialBufferIndex = UnpackMaterialByteOffset(packedMaterial);

    ByteAddressBuffer matBuf = GET_BUFFER(g_FrameData.materialBuffer);
    float doubleSided = asfloat(matBuf.Load(materialBufferIndex + 12u));

    SetMeshOutputCounts(vertexCount, triangleCount);

    float atlasDim = (shadowView.tileOffsetScale.z > 0.0f) ? (shadowView.tileSize / shadowView.tileOffsetScale.z) : 2048.0f;
    float4 tileMinMax = float4(
        shadowView.tileOffsetScale.xy * atlasDim,
        (shadowView.tileOffsetScale.xy + shadowView.tileOffsetScale.zw) * atlasDim
    );

    float4x4 worldViewProj = mul(shadowView.shadowViewProj, instanceData.localToWorld);

    if (groupThreadID < vertexCount)
    {
        Vertex v = LoadMeshletVertex(groupThreadID, meshData, meshlet);

        float4 clipPos = mul(worldViewProj, float4(v.position, 1.0f));

        // Linear Clip-Space Viewport Remapping to shadow atlas tile
        clipPos.x = clipPos.x * shadowView.tileOffsetScale.z + (2.0f * shadowView.tileOffsetScale.x + shadowView.tileOffsetScale.z - 1.0f) * clipPos.w;
        clipPos.y = clipPos.y * shadowView.tileOffsetScale.w + (1.0f - (2.0f * shadowView.tileOffsetScale.y + shadowView.tileOffsetScale.w)) * clipPos.w;

        g_VertexPositions[groupThreadID] = clipPos;

        outVerts[groupThreadID].position = clipPos;
        outVerts[groupThreadID].uv = v.uv;
        outVerts[groupThreadID].materialBufferIndex = materialBufferIndex;
        outVerts[groupThreadID].tileMinMaxPixels = tileMinMax;
    }

    GroupMemoryBarrierWithGroupSync();

    [unroll(2)]
    for (uint primId = groupThreadID; primId < triangleCount; primId += SHADOW_MS_THREADS)
    {
        uint packedIndices = LoadPackedIndices(primId, meshData, meshlet);
        uint3 indices = uint3(packedIndices & 0xFF, (packedIndices >> 8) & 0xFF, (packedIndices >> 16) & 0xFF);

        float4 v0 = g_VertexPositions[indices.x];
        float4 v1 = g_VertexPositions[indices.y];
        float4 v2 = g_VertexPositions[indices.z];

        outTris[primId] = indices;

        bool isCulled = false;
        if (doubleSided == 0.0f)
        {
            isCulled = !IsFrontFacingAndVisible(v0, v1, v2);
            if (!isCulled)
            {
                isCulled = IsTriangleOutsideFrustum(v0, v1, v2);
            }
        }

        outPrims[primId].cullPrim = isCulled;
    }
}

void PSMain(ShadowPixelInput input)
{
    if (input.position.x < input.tileMinMaxPixels.x || input.position.x >= input.tileMinMaxPixels.z ||
        input.position.y < input.tileMinMaxPixels.y || input.position.y >= input.tileMinMaxPixels.w)
    {
        discard;
    }
    
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

#endif // GHOST_TEMPLATE_SHADOW
