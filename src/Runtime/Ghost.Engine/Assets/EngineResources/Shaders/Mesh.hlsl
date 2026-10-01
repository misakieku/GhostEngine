#ifndef GHOST_MESH_HLSL
#define GHOST_MESH_HLSL

struct MeshData
{
    float3 worldBoundsMin;
    uint vertexBufferOffset;
    float3 worldBoundsMax;
    uint indexBufferOffset;

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

struct Vertex
{
    float4 color;
    float4 tangent;
    float3 position;
    float3 normal;
    float2 uv;
};

struct Meshlet
{
    float4 boundingSphere;
    float4 parentBoundingSphere;
    float3 boundingBoxMin;
    float3 boundingBoxMax;
    uint vertexOffset;
    uint triangleOffset;
    uint groupIndex;
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

Meshlet LoadMeshlet(uint meshletID, in MeshData meshData)
{
    return LoadMeshlet(meshletID, meshData.rawBuffer, meshData.meshletBufferOffset);
}

// MeshletGroup loaders
MeshletGroup LoadMeshletGroup(uint groupID, BYTE_ADDRESS_BUFFER rawBufferIndex, uint meshletGroupBufferOffset)
{
    ByteAddressBuffer rawBuffer = GET_BUFFER(rawBufferIndex);
    return rawBuffer.Load<MeshletGroup>(meshletGroupBufferOffset + groupID * sizeof(MeshletGroup));
}

MeshletGroup LoadMeshletGroup(uint groupID, in MeshData meshData)
{
    return LoadMeshletGroup(groupID, meshData.rawBuffer, meshData.meshletGroupBufferOffset);
}

// MeshletHierarchyNode loaders
MeshletHierarchyNode LoadMeshletHierarchyNode(uint nodeID, BYTE_ADDRESS_BUFFER rawBufferIndex, uint meshletHierarchyBufferOffset)
{
    ByteAddressBuffer rawBuffer = GET_BUFFER(rawBufferIndex);
    return rawBuffer.Load<MeshletHierarchyNode>(meshletHierarchyBufferOffset + nodeID * sizeof(MeshletHierarchyNode));
}

MeshletHierarchyNode LoadMeshletHierarchyNode(uint nodeID, in MeshData meshData)
{
    return LoadMeshletHierarchyNode(nodeID, meshData.rawBuffer, meshData.meshletHierarchyBufferOffset);
}

// Triangle Packed Indices
uint LoadPackedIndices(uint triangleID, in MeshData meshData, in Meshlet meshlet)
{
    ByteAddressBuffer rawBuffer = GET_BUFFER(meshData.rawBuffer);
    uint triangleOffset = meshlet.triangleOffset + triangleID;
    return rawBuffer.Load<uint>(meshData.meshletTrianglesBufferOffset + triangleOffset * 4u);
}

// Vertex Loaders
Vertex LoadVertex(uint vertexIndex, in MeshData meshData)
{
    ByteAddressBuffer rawBuffer = GET_BUFFER(meshData.rawBuffer);
    return rawBuffer.Load<Vertex>(meshData.vertexBufferOffset + vertexIndex * sizeof(Vertex));
}

uint LoadMeshletVertexIndex(uint localVertexIndex, in MeshData meshData, in Meshlet meshlet)
{
    ByteAddressBuffer rawBuffer = GET_BUFFER(meshData.rawBuffer);
    return rawBuffer.Load(meshData.meshletVerticesBufferOffset + (meshlet.vertexOffset + localVertexIndex) * 4u);
}

Vertex LoadMeshletVertex(uint localVertexIndex, in MeshData meshData, in Meshlet meshlet)
{
    uint vertexIndex = LoadMeshletVertexIndex(localVertexIndex, meshData, meshlet);
    return LoadVertex(vertexIndex, meshData);
}

#endif // GHOST_MESH_HLSL

