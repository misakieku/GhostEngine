#ifndef GHOST_MESH_HLSL
#define GHOST_MESH_HLSL

#include "EngineResources/Shaders/Common.hlsl"

#define MESHLET_PAGE_SIZE 131072u
#define PACKED_VERTEX_STRIDE 24u
#define PACKED_TRIANGLE_STRIDE 4u

struct MeshData
{
    float3 worldBoundsMin;
    uint globalPageTableOffset;
    float3 worldBoundsMax;
    uint pageCount;

    BYTE_ADDRESS_BUFFER rawBuffer;
    uint meshletBufferOffset;
    uint meshletVerticesBufferOffset;
    uint meshletTrianglesBufferOffset;
    uint meshletGroupBufferOffset;
    uint meshletHierarchyBufferOffset;
    uint meshletCount;
    uint meshletGroupCount;
    uint lodLevelCount;
    uint materialSlotCount;
};

struct Meshlet
{
    float4 boundingSphere;
    float4 parentBoundingSphere;
    float3 boundingBoxMin;
    float3 boundingBoxMax;
    uint vertexOffset;
    uint triangleOffset;
    uint pageIndex;
    float clusterError;
    float parentError;
    uint packedCounts; // byte vertexCount, byte triangleCount, byte localMaterialIndex, byte lodLevel
};

struct MeshletGroup
{
    float4 boundingSphere;
    float3 boundingBoxMin;
    float3 boundingBoxMax;
    float parentError;
    uint meshletStartIndex;
    uint meshletCount;
    uint pageIndex;
    uint childPageRange; // low 16 bits: childPageIndex, high 16 bits: childPageCount
    uint lodLevel;
};

struct MeshletHierarchyNode
{
    float4 bounds;
    float  error;
    int    groupIndex;
    uint   childOffset;
    uint   childCount;
};

// Meshlet loaders
Meshlet LoadMeshlet(uint meshletID, BYTE_ADDRESS_BUFFER rawBufferIndex, uint meshletBufferOffset)
{
    ByteAddressBuffer rawBuffer = GET_BUFFER(rawBufferIndex);
    return rawBuffer.Load<Meshlet>(meshletBufferOffset + meshletID * sizeof(Meshlet));
}

Meshlet LoadMeshlet(uint meshletID, MeshData meshData)
{
    return LoadMeshlet(meshletID, meshData.rawBuffer, meshData.meshletBufferOffset);
}

// MeshletGroup loaders
MeshletGroup LoadMeshletGroup(uint groupID, BYTE_ADDRESS_BUFFER rawBufferIndex, uint meshletGroupBufferOffset)
{
    ByteAddressBuffer rawBuffer = GET_BUFFER(rawBufferIndex);
    return rawBuffer.Load<MeshletGroup>(meshletGroupBufferOffset + groupID * sizeof(MeshletGroup));
}

MeshletGroup LoadMeshletGroup(uint groupID, MeshData meshData)
{
    return LoadMeshletGroup(groupID, meshData.rawBuffer, meshData.meshletGroupBufferOffset);
}

// MeshletHierarchyNode loaders
MeshletHierarchyNode LoadMeshletHierarchyNode(uint nodeID, BYTE_ADDRESS_BUFFER rawBufferIndex, uint meshletHierarchyBufferOffset)
{
    ByteAddressBuffer rawBuffer = GET_BUFFER(rawBufferIndex);
    return rawBuffer.Load<MeshletHierarchyNode>(meshletHierarchyBufferOffset + nodeID * sizeof(MeshletHierarchyNode));
}

MeshletHierarchyNode LoadMeshletHierarchyNode(uint nodeID, MeshData meshData)
{
    return LoadMeshletHierarchyNode(nodeID, meshData.rawBuffer, meshData.meshletHierarchyBufferOffset);
}

// Page Addressing Helper
void GetMeshletPageBufferAndBase(in MeshData meshData, uint pageIndex, out BYTE_ADDRESS_BUFFER pageBuffer, out uint pageBase)
{
    if (IS_VALID_BUFFER(g_FrameData.pagePoolBuffer) && IS_VALID_BUFFER(g_FrameData.pageTableBuffer))
    {
        ByteAddressBuffer pageTable = GET_BUFFER(g_FrameData.pageTableBuffer);
        uint physicalSlot = pageTable.Load((meshData.globalPageTableOffset + pageIndex) * 4u);
        if (physicalSlot != 0xFFFFFFFFu)
        {
            pageBuffer = g_FrameData.pagePoolBuffer;
            pageBase = physicalSlot * MESHLET_PAGE_SIZE;
            return;
        }
    }

    pageBuffer = meshData.rawBuffer;
    pageBase = meshData.meshletVerticesBufferOffset;
}

// Triangle Packed Indices
uint LoadPackedIndices(uint triangleID, MeshData meshData, Meshlet meshlet)
{
    BYTE_ADDRESS_BUFFER pageBufferIndex;
    uint pageBase;
    GetMeshletPageBufferAndBase(meshData, meshlet.pageIndex, pageBufferIndex, pageBase);
    ByteAddressBuffer pageBuffer = GET_BUFFER(pageBufferIndex);
    return pageBuffer.Load<uint>(pageBase + meshlet.triangleOffset + triangleID * PACKED_TRIANGLE_STRIDE);
}

// Packed Vertex and Unpacking Helpers
struct PackedVertex
{
    float3 position;
    float3 normal;
    float2 uv;
    float4 color;
};

float3 UnpackOctahedralNormal(uint packedNormal)
{
    int sx = (int)(packedNormal & 0xFFFFu);
    if (sx >= 32768) sx -= 65536;
    int sy = (int)(packedNormal >> 16u);
    if (sy >= 32768) sy -= 65536;

    float2 oct = float2(sx, sy) * (1.0f / 32767.0f);
    float3 n = float3(oct.x, oct.y, 1.0f - abs(oct.x) - abs(oct.y));
    if (n.z < 0.0f)
    {
        float2 signNotZero = float2(n.x >= 0.0f ? 1.0f : -1.0f, n.y >= 0.0f ? 1.0f : -1.0f);
        n.xy = (1.0f - abs(n.yx)) * signNotZero;
    }
    return normalize(n);
}

float2 UnpackHalf2(uint packedUV)
{
    return f16tof32(uint2(packedUV & 0xFFFFu, packedUV >> 16u));
}

float4 UnpackRGBA8(uint packedColor)
{
    return float4(
        float(packedColor & 0xFFu),
        float((packedColor >> 8u) & 0xFFu),
        float((packedColor >> 16u) & 0xFFu),
        float((packedColor >> 24u) & 0xFFu)
    ) * (1.0f / 255.0f);
}

PackedVertex LoadMeshletPackedVertex(uint localVertexIndex, MeshData meshData, Meshlet meshlet)
{
    BYTE_ADDRESS_BUFFER pageBufferIndex;
    uint pageBase;
    GetMeshletPageBufferAndBase(meshData, meshlet.pageIndex, pageBufferIndex, pageBase);
    ByteAddressBuffer pageBuffer = GET_BUFFER(pageBufferIndex);
    uint vOffset = pageBase + meshlet.vertexOffset + localVertexIndex * PACKED_VERTEX_STRIDE;

    uint4 raw0 = pageBuffer.Load4(vOffset);
    uint2 raw1 = pageBuffer.Load2(vOffset + 16u);

    PackedVertex v;
    v.position = asfloat(raw0.xyz);
    v.normal = UnpackOctahedralNormal(raw0.w);
    v.uv = UnpackHalf2(raw1.x);
    v.color = UnpackRGBA8(raw1.y);
    return v;
}

float3 LoadMeshletPositionOnly(MeshData meshData, Meshlet meshlet, uint localVertexIndex)
{
    BYTE_ADDRESS_BUFFER pageBufferIndex;
    uint pageBase;
    GetMeshletPageBufferAndBase(meshData, meshlet.pageIndex, pageBufferIndex, pageBase);
    ByteAddressBuffer pageBuffer = GET_BUFFER(pageBufferIndex);
    uint vOffset = pageBase + meshlet.vertexOffset + localVertexIndex * PACKED_VERTEX_STRIDE;
    return asfloat(pageBuffer.Load3(vOffset));
}

float2 LoadMeshletUVOnly(MeshData meshData, Meshlet meshlet, uint localVertexIndex)
{
    BYTE_ADDRESS_BUFFER pageBufferIndex;
    uint pageBase;
    GetMeshletPageBufferAndBase(meshData, meshlet.pageIndex, pageBufferIndex, pageBase);
    ByteAddressBuffer pageBuffer = GET_BUFFER(pageBufferIndex);
    uint vOffset = pageBase + meshlet.vertexOffset + localVertexIndex * PACKED_VERTEX_STRIDE;
    uint packedUV = pageBuffer.Load(vOffset + 16u);
    return UnpackHalf2(packedUV);
}

uint LoadMeshletTriangle(MeshData meshData, Meshlet meshlet, uint localTriangleIndex)
{
    BYTE_ADDRESS_BUFFER pageBufferIndex;
    uint pageBase;
    GetMeshletPageBufferAndBase(meshData, meshlet.pageIndex, pageBufferIndex, pageBase);
    ByteAddressBuffer pageBuffer = GET_BUFFER(pageBufferIndex);
    uint tOffset = pageBase + meshlet.triangleOffset + localTriangleIndex * PACKED_TRIANGLE_STRIDE;
    return pageBuffer.Load(tOffset);
}

float3 LoadMeshletPositionOnly(ByteAddressBuffer rawBuffer, uint meshletVerticesBufferOffset, uint pageIndex, uint vertexOffset, uint localVertexIndex)
{
    uint pageBase = meshletVerticesBufferOffset + pageIndex * MESHLET_PAGE_SIZE;
    uint vOffset = pageBase + vertexOffset + localVertexIndex * PACKED_VERTEX_STRIDE;
    return asfloat(rawBuffer.Load3(vOffset));
}

float2 LoadMeshletUVOnly(ByteAddressBuffer rawBuffer, uint meshletVerticesBufferOffset, uint pageIndex, uint vertexOffset, uint localVertexIndex)
{
    uint pageBase = meshletVerticesBufferOffset + pageIndex * MESHLET_PAGE_SIZE;
    uint vOffset = pageBase + vertexOffset + localVertexIndex * PACKED_VERTEX_STRIDE;
    uint packedUV = rawBuffer.Load(vOffset + 16u);
    return UnpackHalf2(packedUV);
}

uint LoadMeshletTriangle(ByteAddressBuffer rawBuffer, uint meshletVerticesBufferOffset, uint pageIndex, uint triangleOffset, uint localTriangleIndex)
{
    uint pageBase = meshletVerticesBufferOffset + pageIndex * MESHLET_PAGE_SIZE;
    uint tOffset = pageBase + triangleOffset + localTriangleIndex * PACKED_TRIANGLE_STRIDE;
    return rawBuffer.Load(tOffset);
}

// Christian Schüler's implicit tangent derivation
float4 DeriveImplicitTangent(float3 p0, float3 p1, float3 p2, float2 uv0, float2 uv1, float2 uv2, float3 normalWS)
{
    float2 uv10 = uv1 - uv0;
    float2 uv20 = uv2 - uv0;

    float3 dp10 = p1 - p0;
    float3 dp20 = p2 - p0;

    float3 perp2 = cross(normalWS, dp20);
    float3 perp1 = cross(dp10, normalWS);

    float3 tangent = perp2 * uv10.x + perp1 * uv20.x;
    float lenSq = dot(tangent, tangent);
    if (lenSq > 1e-12f)
    {
        tangent *= rsqrt(lenSq);
    }
    else
    {
        tangent = abs(normalWS.z) < 0.999f ? cross(normalWS, float3(0, 0, 1)) : cross(normalWS, float3(1, 0, 0));
        tangent = normalize(tangent);
    }

    float3 geoNormal = cross(dp10, dp20);
    float uvSign = uv10.x * uv20.y - uv20.x * uv10.y;
    float sign = (uvSign * dot(normalWS, geoNormal) < 0.0f) ? -1.0f : 1.0f;

    return float4(tangent, sign);
}

#endif // GHOST_MESH_HLSL
