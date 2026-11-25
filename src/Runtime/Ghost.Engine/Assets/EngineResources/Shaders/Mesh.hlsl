#ifndef GHOST_MESH_HLSL
#define GHOST_MESH_HLSL

#include "EngineResources/Shaders/Common.hlsl"

#define MESHLET_PAGE_SIZE 131072u

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
    float positionQuantStep;
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
    float maxEdgeLength;
    uint packedQuantOrigin0;
    uint packedQuantOrigin1;
    uint packedFormat;
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

// Bitstream & Cluster Quantization Helpers
uint ExtractBitStream(ByteAddressBuffer buf, uint byteBase, uint bitOffset, uint bitCount)
{
    if (bitCount == 0u)
    {
        return 0u;
    }

    uint wordIndex = bitOffset >> 5u;
    uint bitInWord = bitOffset & 31u;
    uint wordAddr = byteBase + wordIndex * 4u;
    uint val = buf.Load(wordAddr) >> bitInWord;
    if (bitInWord + bitCount > 32u)
    {
        val |= buf.Load(wordAddr + 4u) << (32u - bitInWord);
    }
    return val & ((1u << bitCount) - 1u);
}

uint3 UnpackMeshletQuantOrigin(uint packed0, uint packed1)
{
    uint minX = packed0 & 0x1FFFFFu;
    uint minY = (packed0 >> 21u) | ((packed1 & 0x3FFu) << 11u);
    uint minZ = (packed1 >> 10u) & 0x1FFFFFu;
    return uint3(minX, minY, minZ);
}

uint GetMeshletAttributeBase(uint pageBase, uint packedFormat)
{
    return pageBase + (((packedFormat >> 16u) & 0x7FFFu) << 2u);
}

bool HasMeshletVertexColor(uint packedFormat)
{
    return (packedFormat & (1u << 15u)) != 0u;
}

float3 UnpackMeshletPosition(ByteAddressBuffer pageBuffer, uint pageBase, uint vertexOffset, uint packedQuantOrigin0, uint packedQuantOrigin1, uint packedFormat, float3 worldBoundsMin, float positionQuantStep, uint localVertexIndex)
{
    uint bitsX = packedFormat & 0x1Fu;
    uint bitsY = (packedFormat >> 5u) & 0x1Fu;
    uint bitsZ = (packedFormat >> 10u) & 0x1Fu;
    uint totalBits = bitsX + bitsY + bitsZ;

    uint3 qOrigin = UnpackMeshletQuantOrigin(packedQuantOrigin0, packedQuantOrigin1);
    uint3 qDelta = uint3(0u, 0u, 0u);
    if (totalBits > 0u)
    {
        uint posByteBase = pageBase + vertexOffset;
        uint bitOffset = localVertexIndex * totalBits;
        qDelta.x = ExtractBitStream(pageBuffer, posByteBase, bitOffset, bitsX);
        qDelta.y = ExtractBitStream(pageBuffer, posByteBase, bitOffset + bitsX, bitsY);
        qDelta.z = ExtractBitStream(pageBuffer, posByteBase, bitOffset + bitsX + bitsY, bitsZ);
    }

    uint3 qPos = qOrigin + qDelta;
    return worldBoundsMin + (float3)qPos * positionQuantStep;
}

uint3 UnpackMeshletTriangleIndices(ByteAddressBuffer pageBuffer, uint pageBase, uint triangleOffset, uint localTriangleIndex)
{
    uint triByteBase = pageBase + triangleOffset;
    uint packed18 = ExtractBitStream(pageBuffer, triByteBase, localTriangleIndex * 18u, 18u);
    return uint3(packed18 & 0x3Fu, (packed18 >> 6u) & 0x3Fu, (packed18 >> 12u) & 0x3Fu);
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

    bool hasColor = HasMeshletVertexColor(meshlet.packedFormat);
    uint attrStride = hasColor ? 12u : 8u;
    uint attrOffset = GetMeshletAttributeBase(pageBase, meshlet.packedFormat) + localVertexIndex * attrStride;
    uint2 rawAttr = pageBuffer.Load2(attrOffset);

    PackedVertex v;
    v.position = UnpackMeshletPosition(pageBuffer, pageBase, meshlet.vertexOffset, meshlet.packedQuantOrigin0, meshlet.packedQuantOrigin1, meshlet.packedFormat, meshData.worldBoundsMin, meshData.positionQuantStep, localVertexIndex);
    v.normal = UnpackOctahedralNormal(rawAttr.x);
    v.uv = UnpackHalf2(rawAttr.y);
    v.color = hasColor ? UnpackRGBA8(pageBuffer.Load(attrOffset + 8u)) : float4(1.0f, 1.0f, 1.0f, 1.0f);
    return v;
}

float3 LoadMeshletPositionOnly(MeshData meshData, Meshlet meshlet, uint localVertexIndex)
{
    BYTE_ADDRESS_BUFFER pageBufferIndex;
    uint pageBase;
    GetMeshletPageBufferAndBase(meshData, meshlet.pageIndex, pageBufferIndex, pageBase);
    ByteAddressBuffer pageBuffer = GET_BUFFER(pageBufferIndex);
    return UnpackMeshletPosition(pageBuffer, pageBase, meshlet.vertexOffset, meshlet.packedQuantOrigin0, meshlet.packedQuantOrigin1, meshlet.packedFormat, meshData.worldBoundsMin, meshData.positionQuantStep, localVertexIndex);
}

float2 LoadMeshletUVOnly(ByteAddressBuffer pageBuffer, uint pageBase, uint packedFormat, uint localVertexIndex)
{
    bool hasColor = HasMeshletVertexColor(packedFormat);
    uint attrStride = hasColor ? 12u : 8u;
    uint attrBase = GetMeshletAttributeBase(pageBase, packedFormat);
    uint packedUV = pageBuffer.Load(attrBase + localVertexIndex * attrStride + 4u);
    return UnpackHalf2(packedUV);
}

float2 LoadMeshletUVOnly(MeshData meshData, Meshlet meshlet, uint localVertexIndex)
{
    BYTE_ADDRESS_BUFFER pageBufferIndex;
    uint pageBase;
    GetMeshletPageBufferAndBase(meshData, meshlet.pageIndex, pageBufferIndex, pageBase);
    ByteAddressBuffer pageBuffer = GET_BUFFER(pageBufferIndex);
    return LoadMeshletUVOnly(pageBuffer, pageBase, meshlet.packedFormat, localVertexIndex);
}

uint LoadMeshletTriangle(MeshData meshData, Meshlet meshlet, uint localTriangleIndex)
{
    BYTE_ADDRESS_BUFFER pageBufferIndex;
    uint pageBase;
    GetMeshletPageBufferAndBase(meshData, meshlet.pageIndex, pageBufferIndex, pageBase);
    ByteAddressBuffer pageBuffer = GET_BUFFER(pageBufferIndex);
    uint3 idx = UnpackMeshletTriangleIndices(pageBuffer, pageBase, meshlet.triangleOffset, localTriangleIndex);
    return idx.x | (idx.y << 8u) | (idx.z << 16u);
}

uint LoadPackedIndices(uint triangleID, MeshData meshData, Meshlet meshlet)
{
    return LoadMeshletTriangle(meshData, meshlet, triangleID);
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
