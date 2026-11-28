// ============================================================
// GhostEngine Lit Transparent Forward Pass (AVBOIT + Clustered)
// ============================================================

#ifndef GHOST_LIT_TRANSPARENT_FORWARD_TEMPLATE_HLSL
#define GHOST_LIT_TRANSPARENT_FORWARD_TEMPLATE_HLSL

#include "EngineResources/Shaders/Generated/GhostRenderPipeline.hlsl"

cbuffer PassProperties : register(b3, space0)
{
    TransparentForwardShaderProperties g_Properties;
};

#include "Lit/Lit_Common.template.hlsl"
#include "EngineResources/Shaders/Mesh.hlsl"
#include "EngineResources/Shaders/Utilities/CullCommon.hlsl"
#include "EngineResources/Shaders/MaterialPipeline/MaterialEncoding.hlsl"
#include "EngineResources/Shaders/Material/Lit/LightLoop.hlsl"
#include "EngineResources/Shaders/Oit/OitCommon.hlsl"

struct TransparentForwardPixelInput
{
    float4 position : SV_POSITION;
    float3 positionRWS : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
    float2 uv : TEXCOORD2;
    nointerpolation uint instanceIndex : INSTANCE_ID;
    nointerpolation uint materialBufferIndex : CBUFFER_ID;
    nointerpolation uint chromatic : CHROMATIC_FLAG;
};

struct TransparentForwardPrimitiveOutput
{
    bool cullPrim : SV_CullPrimitive;
};

struct TransparentForwardOutput
{
    float4 mrt0 : SV_Target0;
    float4 mrt1 : SV_Target1;
    float4 mrt2 : SV_Target2;
};

#define TRANSPARENT_FORWARD_MS_THREADS 64

struct SharedTransparentForwardState
{
    float4x4 localToRWS;
    float4x4 localToWorld;
    float4x4 worldViewProj;
    float3 worldBoundsMin;
    float positionQuantStep;
    uint pageBufferIndex;
    uint pageBase;
    uint vertexOffset;
    uint triangleOffset;
    uint packedQuantOrigin0;
    uint packedQuantOrigin1;
    uint packedFormat;
    uint vertexCount;
    uint triangleCount;
    uint instanceIndex;
    uint materialBufferIndex;
    uint chromatic;
    float doubleSided;
};

groupshared SharedTransparentForwardState s_Meshlet;
groupshared float4 g_VertexPositions[MAX_VERTICES_PER_MESHLET];

[numthreads(TRANSPARENT_FORWARD_MS_THREADS, 1, 1)]
[outputtopology("triangle")]
void MSMain(
    uint groupID : SV_GroupID,
    uint groupThreadID : SV_GroupThreadID,
    out vertices TransparentForwardPixelInput outVerts[MAX_VERTICES_PER_MESHLET],
    out indices uint3 outTris[MAX_TRIANGLES_PER_MESHLET],
    out primitives TransparentForwardPrimitiveOutput outPrims[MAX_TRIANGLES_PER_MESHLET])
{
    if (groupThreadID == 0)
    {
        ByteAddressBuffer matBuf = GET_BUFFER(g_FrameData.materialBuffer);
        ByteAddressBuffer binOffsetsBuffer = GET_BUFFER(g_Properties.binOffsetsBufferIndex);
        uint binStartOffset = binOffsetsBuffer.Load(g_Properties.binIndex * 4u);
        uint binnedSlot = binStartOffset + groupID;

        StructuredBuffer<VisibleMeshletEntry> visibleMeshlets = GET_BUFFER(g_Properties.visibleMeshletsBufferIndex);
        VisibleMeshletEntry visible = visibleMeshlets[binnedSlot];

        InstanceData instanceData = LoadData<InstanceData>(g_FrameData.sceneBuffer, visible.instanceIndex);
        MeshData meshData = LoadData<MeshData>(instanceData.meshBuffer, 0);
        Meshlet meshlet = LoadMeshlet(visible.meshletIndex, meshData);

        uint vertexCount = meshlet.packedCounts & 0xFFu;
        uint triangleCount = (meshlet.packedCounts >> 8u) & 0xFFu;
        uint localMaterialIndex = (meshlet.packedCounts >> 16u) & 0xFFu;

        uint packedMaterial = LoadMaterialBindlessIndex(g_FrameData.paletteOffsetBuffer, g_FrameData.materialIndexBuffer, instanceData.materialPaletteIndex, localMaterialIndex);
        uint materialBufferIndex = UnpackMaterialByteOffset(packedMaterial);
        uint chromatic = UnpackMaterialChromaticTransmittance(packedMaterial);
        float doubleSided = asfloat(matBuf.Load(materialBufferIndex + 12u));

        float4x4 localToRWS = ApplyCameraTranslationToMatrix(instanceData.localToWorld);
        float4x4 worldViewProj = mul(g_ViewData.viewProjectionMatrix, localToRWS);

        BYTE_ADDRESS_BUFFER pageBufferIndex;
        uint pageBase;
        GetMeshletPageBufferAndBase(meshData, meshlet.pageIndex, pageBufferIndex, pageBase);

        s_Meshlet.localToRWS = localToRWS;
        s_Meshlet.localToWorld = instanceData.localToWorld;
        s_Meshlet.worldViewProj = worldViewProj;
        s_Meshlet.worldBoundsMin = meshData.worldBoundsMin;
        s_Meshlet.positionQuantStep = meshData.positionQuantStep;
        s_Meshlet.pageBufferIndex = pageBufferIndex;
        s_Meshlet.pageBase = pageBase;
        s_Meshlet.vertexOffset = meshlet.vertexOffset;
        s_Meshlet.triangleOffset = meshlet.triangleOffset;
        s_Meshlet.packedQuantOrigin0 = meshlet.packedQuantOrigin0;
        s_Meshlet.packedQuantOrigin1 = meshlet.packedQuantOrigin1;
        s_Meshlet.packedFormat = meshlet.packedFormat;
        s_Meshlet.vertexCount = vertexCount;
        s_Meshlet.triangleCount = triangleCount;
        s_Meshlet.instanceIndex = visible.instanceIndex;
        s_Meshlet.materialBufferIndex = materialBufferIndex;
        s_Meshlet.chromatic = chromatic;
        s_Meshlet.doubleSided = doubleSided;
    }

    GroupMemoryBarrierWithGroupSync();

    SetMeshOutputCounts(s_Meshlet.vertexCount, s_Meshlet.triangleCount);

    if (groupThreadID < s_Meshlet.vertexCount)
    {
        ByteAddressBuffer pageBuffer = GET_BUFFER(s_Meshlet.pageBufferIndex);

        float3 pos = UnpackMeshletPosition(pageBuffer, s_Meshlet.pageBase, s_Meshlet.vertexOffset, s_Meshlet.packedQuantOrigin0, s_Meshlet.packedQuantOrigin1, s_Meshlet.packedFormat, s_Meshlet.worldBoundsMin, s_Meshlet.positionQuantStep, groupThreadID);

        bool hasColor = HasMeshletVertexColor(s_Meshlet.packedFormat);
        uint attrStride = hasColor ? 12u : 8u;
        uint attrOffset = GetMeshletAttributeBase(s_Meshlet.pageBase, s_Meshlet.packedFormat) + groupThreadID * attrStride;
        uint2 rawAttr = pageBuffer.Load2(attrOffset);

        float3 localNormal = UnpackOctahedralNormal(rawAttr.x);
        float2 uv = UnpackHalf2(rawAttr.y);

        float3 posRWS = mul(s_Meshlet.localToRWS, float4(pos, 1.0f)).xyz;
        float3 normalWS = normalize(mul((float3x3)s_Meshlet.localToWorld, localNormal));
        float4 clipPos = mul(s_Meshlet.worldViewProj, float4(pos, 1.0f));

        g_VertexPositions[groupThreadID] = clipPos;

        outVerts[groupThreadID].position = clipPos;
        outVerts[groupThreadID].positionRWS = posRWS;
        outVerts[groupThreadID].normalWS = normalWS;
        outVerts[groupThreadID].uv = uv;
        outVerts[groupThreadID].instanceIndex = s_Meshlet.instanceIndex;
        outVerts[groupThreadID].materialBufferIndex = s_Meshlet.materialBufferIndex;
        outVerts[groupThreadID].chromatic = s_Meshlet.chromatic;
    }

    GroupMemoryBarrierWithGroupSync();

    [unroll(2)]
    for (uint primId = groupThreadID; primId < s_Meshlet.triangleCount; primId += TRANSPARENT_FORWARD_MS_THREADS)
    {
        ByteAddressBuffer pageBuffer = GET_BUFFER(s_Meshlet.pageBufferIndex);
        uint3 indices = UnpackMeshletTriangleIndices(pageBuffer, s_Meshlet.pageBase, s_Meshlet.triangleOffset, primId);

        float4 v0 = g_VertexPositions[indices.x];
        float4 v1 = g_VertexPositions[indices.y];
        float4 v2 = g_VertexPositions[indices.z];

        outTris[primId] = indices;

        bool isCulled = false;
        if (s_Meshlet.doubleSided == 0.0f)
        {
            isCulled = !IsTriangleFrontFacing(v0, v1, v2);
        }

        if (!isCulled)
        {
            isCulled = IsTriangleOutsideFrustum(v0, v1, v2);
        }

        outPrims[primId].cullPrim = isCulled;
    }
}

TransparentForwardOutput PSMain(TransparentForwardPixelInput input, bool isFrontFace : SV_IsFrontFace)
{
    uint2 pixelCoord = (uint2)input.position.xy;
    float3 normalWS = normalize(input.normalWS);
    float2 ddx_uv = ddx(input.uv);
    float2 ddy_uv = ddy(input.uv);
    float3 ddx_pos = ddx(input.positionRWS);
    float3 ddy_pos = ddy(input.positionRWS);
    float4 tangentWS = DeriveImplicitTangent(input.positionRWS, input.positionRWS + ddx_pos, input.positionRWS + ddy_pos, input.uv, input.uv + ddx_uv, input.uv + ddy_uv, normalWS);

    MaterialContext ctx = (MaterialContext)0;
    ctx.instanceIndex = input.instanceIndex;
    uint materialIdx = WaveActiveAllEqual(input.materialBufferIndex) ? WaveReadLaneFirst(input.materialBufferIndex) : input.materialBufferIndex;
    ctx.materialIndex = materialIdx;
    ctx.positionRWS = input.positionRWS;
    ctx.normalWS = normalWS;
    ctx.tangentWS = tangentWS;
    ctx.uv = input.uv;
    ctx.ddx_uv = ddx_uv;
    ctx.ddy_uv = ddy_uv;
    ctx.isFrontFace = isFrontFace;

    MaterialProperties matProps = LoadMaterialData<MaterialProperties>(materialIdx);
    TRANSPARENCY_STRATEGY transStrategy = TRANSPARENCY_STRATEGY::Create();
    TransparencyData tData = transStrategy.GetTransparencyData(ctx, matProps);

    if (tData.opacity <= 0.001f)
    {
        discard;
    }

    DEFERREDTEXTURING_STRATEGY texStrategy = DEFERREDTEXTURING_STRATEGY::Create();
    SurfaceData surface = texStrategy.GetSurfaceData(ctx, matProps);

    DEFERREDLIGHTING_STRATEGY lightStrategy = DEFERREDLIGHTING_STRATEGY::Create();
    BSDFData bsdf = lightStrategy.GetBSDFData(input.positionRWS, tangentWS.xyz, surface);

    float viewZ = max(dot(input.positionRWS, g_ViewData.cameraDirection), g_ViewData.nearClip);
    float2 screenUV = input.position.xy * g_ViewData.screenSize.zw;
    float2 ndc = float2(screenUV.x * 2.0f - 1.0f, 1.0f - screenUV.y * 2.0f);
    float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);

    ShadingContext shadingCtx = (ShadingContext)0;
    shadingCtx.positionRWS = input.positionRWS;
    shadingCtx.normalWS = surface.normalWS;
    shadingCtx.positionNDC = ndc;
    shadingCtx.positionSS = pixelCoord;
    shadingCtx.tileCoord = pixelCoord / CLUSTER_TILE_SIZE;
    shadingCtx.depth = input.position.z;
    shadingCtx.linearDepth = viewZ;

    ByteAddressBuffer clusterGrid = GET_BUFFER(g_Properties.clusterLightGridBufferIndex);
    ByteAddressBuffer clusterIndices = GET_BUFFER(g_Properties.clusterLightIndexBufferIndex);
    ClusteredLightGridPolicy lightGrid = ClusteredLightGridPolicy::Create(clusterGrid, clusterIndices, g_Properties.clusterTilesX, g_Properties.clusterSliceCount);
    LightLoopOutput light = ExecuteLightLoop<DEFERREDLIGHTING_STRATEGY, ClusteredLightGridPolicy>(shadingCtx, bsdf, lightStrategy, lightGrid, V);

    float3 litColor = light.diffuse + light.specular;
    float3 weight = OitSampleTransmittanceWeight(g_Properties.oitIntegralLutSrv, g_Properties.oitDepthWarpLutSrv, screenUV, viewZ, g_Properties.oitSampleBias);

    float3 alphaRGB = (input.chromatic == 0u)
        ? float3(tData.opacity, tData.opacity, tData.opacity)
        : saturate(tData.opacity * (1.0f - saturate(tData.transmittanceColor)));

    TransparentForwardOutput output;
    output.mrt0 = float4(litColor * tData.opacity * weight, 1.0f);
    output.mrt1 = float4(alphaRGB * weight, 1.0f);
    output.mrt2 = float4(OitAlphaToRawExtinction(alphaRGB), 1.0f);
    return output;
}

#endif // GHOST_LIT_TRANSPARENT_FORWARD_TEMPLATE_HLSL
