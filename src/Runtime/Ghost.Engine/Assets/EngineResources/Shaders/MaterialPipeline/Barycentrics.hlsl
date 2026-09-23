#ifndef GHOST_BARYCENTRICS_HLSL
#define GHOST_BARYCENTRICS_HLSL

#include "EngineResources/Shaders/Common.hlsl"
#include "EngineResources/Shaders/Properties.hlsl"
#include "EngineResources/Shaders/MeshPipeline/CullCommon.hlsl"
#include "EngineResources/Shaders/MaterialPipeline/MaterialEncoding.hlsl"
#include "EngineResources/Shaders/MaterialPipeline/VisibilityBufferEncoding.hlsl"

struct InterpolatedAttributes
{
    float3 worldPos;
    float3 normalWS;
    float4 tangentWS;
    float4 color;
    float2 uv;
    float2 ddx_uv;
    float2 ddy_uv;
    float2 motionVectors;
    uint cbufferIndex;
    uint variantIndex;
    bool isFrontFacing;
};

static inline float2 PixelToClipSpace(float2 pixelPos, float2 invViewportSize)
{
    return pixelPos * invViewportSize * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f);
}

/// <summary>
/// Reconstructs perspective-correct barycentrics, vertex attributes, and analytical screen derivatives
/// entirely in Homogeneous Clip Space (Nanite / Projective 2D Line Formulation).
/// </summary>
static inline InterpolatedAttributes EvaluateBarycentricsAndDerivatives(uint2 pixelCoord, float depth, uint instanceIndex, uint meshletIndex, uint primitiveID, in MeshData meshData)
{
    Meshlet meshlet = LoadData<Meshlet>(meshData.meshletBuffer, meshletIndex);

    ByteAddressBuffer meshletVerticesBuffer = GET_BUFFER(meshData.meshletVerticesBuffer);
    ByteAddressBuffer meshletTrianglesBuffer = GET_BUFFER(meshData.meshletTrianglesBuffer);
    ByteAddressBuffer vertexBuffer = GET_BUFFER(meshData.vertexBuffer);

    uint packedIndices = meshletTrianglesBuffer.Load((meshlet.triangleOffset + primitiveID) * 4u);
    uint3 localIndices = uint3(packedIndices & 0xFFu, (packedIndices >> 8u) & 0xFFu, (packedIndices >> 16u) & 0xFFu);

    uint v0Idx = meshletVerticesBuffer.Load((meshlet.vertexOffset + localIndices.x) * 4u);
    uint v1Idx = meshletVerticesBuffer.Load((meshlet.vertexOffset + localIndices.y) * 4u);
    uint v2Idx = meshletVerticesBuffer.Load((meshlet.vertexOffset + localIndices.z) * 4u);

    Vertex v0 = vertexBuffer.Load<Vertex>(v0Idx * sizeof(Vertex));
    Vertex v1 = vertexBuffer.Load<Vertex>(v1Idx * sizeof(Vertex));
    Vertex v2 = vertexBuffer.Load<Vertex>(v2Idx * sizeof(Vertex));

    InstanceData instanceData = LoadData<InstanceData>(g_FrameData.sceneBuffer, instanceIndex);
    float4x4 worldViewProj = mul(g_ViewData.viewProjectionMatrix, instanceData.localToWorld);

    // Transform vertices to homogeneous clip space (x, y, z, w)
    float4 p0 = mul(worldViewProj, float4(v0.position, 1.0f));
    float4 p1 = mul(worldViewProj, float4(v1.position, 1.0f));
    float4 p2 = mul(worldViewProj, float4(v2.position, 1.0f));

    // Construct 2D homogeneous line equations in clip space (cross product of p.xyw)
    float3 c0 = cross(p1.xyw, p2.xyw);
    float3 c1 = cross(p2.xyw, p0.xyw);
    float3 c2 = cross(p0.xyw, p1.xyw);

    float det = dot(p0.xyw, c0);

    // Map pixel center to clip space
    float2 pixelPos = float2(pixelCoord) + 0.5f;
    float2 pixelClip = PixelToClipSpace(pixelPos, g_ViewData.screenSize.zw);
    float3 clipP = float3(pixelClip, 1.0f);

    // q = (c0·p, c1·p, c2·p)^T, proportional to (bary / w)
    float3 q = float3(dot(c0, clipP), dot(c1, clipP), dot(c2, clipP));

    float D = q.x + q.y + q.z;
    float invD = (abs(D) > 1e-20f) ? rcp(D) : 0.0f;
    float3 bary = q * invD;

    // Compute dq/dx_pixel and dq/dy_pixel
    // dx_clip / dx_pixel = 2.0 * invScreenSize.x
    // dy_clip / dy_pixel = -2.0 * invScreenSize.y (DirectX viewport Y is flipped)
    float2 dClip_dPixel = g_ViewData.screenSize.zw * float2(2.0f, -2.0f);
    float3 dq_dx = float3(c0.x, c1.x, c2.x) * dClip_dPixel.x;
    float3 dq_dy = float3(c0.y, c1.y, c2.y) * dClip_dPixel.y;

    float dD_dx = dq_dx.x + dq_dx.y + dq_dx.z;
    float dD_dy = dq_dy.x + dq_dy.y + dq_dy.z;

    // UV and analytical screen derivatives via quotient rule
    float2 uv = bary.x * v0.uv + bary.y * v1.uv + bary.z * v2.uv;
    float2 dN_dx = dq_dx.x * v0.uv + dq_dx.y * v1.uv + dq_dx.z * v2.uv;
    float2 dN_dy = dq_dy.x * v0.uv + dq_dy.y * v1.uv + dq_dy.z * v2.uv;

    float2 ddx_uv = (dN_dx - uv * dD_dx) * invD;
    float2 ddy_uv = (dN_dy - uv * dD_dy) * invD;

    // Interpolate vertex attributes
    float3 pw0 = mul(instanceData.localToWorld, float4(v0.position, 1.0f)).xyz;
    float3 pw1 = mul(instanceData.localToWorld, float4(v1.position, 1.0f)).xyz;
    float3 pw2 = mul(instanceData.localToWorld, float4(v2.position, 1.0f)).xyz;
    float3 worldPos = bary.x * pw0 + bary.y * pw1 + bary.z * pw2;

    float3 n0 = mul((float3x3)instanceData.localToWorld, v0.normal);
    float3 n1 = mul((float3x3)instanceData.localToWorld, v1.normal);
    float3 n2 = mul((float3x3)instanceData.localToWorld, v2.normal);
    float3 normalWS = normalize(bary.x * n0 + bary.y * n1 + bary.z * n2);

    float3 t0 = mul((float3x3)instanceData.localToWorld, v0.tangent.xyz);
    float3 t1 = mul((float3x3)instanceData.localToWorld, v1.tangent.xyz);
    float3 t2 = mul((float3x3)instanceData.localToWorld, v2.tangent.xyz);
    float3 tangentWS = normalize(bary.x * t0 + bary.y * t1 + bary.z * t2);

    float4 color = bary.x * v0.color + bary.y * v1.color + bary.z * v2.color;

    // Motion Vectors
    float2 currentScreenUV = pixelPos * g_ViewData.screenSize.zw;
    float4 prevClip = mul(g_ViewData.preVPMatrix, float4(worldPos, 1.0f));
    float prevInvW = (abs(prevClip.w) > 1e-7f) ? rcp(prevClip.w) : 0.0f;
    float2 prevScreenUV = float2(prevClip.x * prevInvW * 0.5f + 0.5f, 1.0f - (prevClip.y * prevInvW * 0.5f + 0.5f));
    float2 motionVectors = currentScreenUV - prevScreenUV;

    uint localMaterialIndex = (meshlet.packedCounts >> 16u) & 0xFFu;
    uint packedMaterial = LoadMaterialBindlessIndex(
        g_FrameData.paletteOffsetBuffer, 
        g_FrameData.materialIndexBuffer, 
        instanceData.materialPaletteIndex, 
        localMaterialIndex
    );

    InterpolatedAttributes attrs;
    attrs.worldPos = worldPos;
    attrs.normalWS = normalWS;
    attrs.tangentWS = float4(tangentWS, v0.tangent.w);
    attrs.color = color;
    attrs.uv = uv;
    attrs.ddx_uv = ddx_uv;
    attrs.ddy_uv = ddy_uv;
    attrs.motionVectors = motionVectors;
    attrs.cbufferIndex = UnpackMaterialMaterialBufferIndex(packedMaterial);
    attrs.variantIndex = UnpackMaterialVariantIndex(packedMaterial);
    // Winding check in clip space (CCW is front-facing)
    attrs.isFrontFacing = (det > 0.0f);

    return attrs;
}

#endif // GHOST_BARYCENTRICS_HLSL
