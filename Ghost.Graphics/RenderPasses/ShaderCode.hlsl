cbuffer ConstantBuffer : register(b0)
{
    float4 _Color;
    uint _TextureIndex1;
    uint _TextureIndex2;
    uint _TextureIndex3;
    uint _TextureIndex4;
    uint _VertexBufferIndex;
    uint _IndexBufferIndex;
};

struct Vertex
{
    float4 position;
    float4 normal;
    float4 tangent;
    float4 color;
    float4 uv;
};

struct PixelInput
{
    float4 position : SV_POSITION;
    float4 color : COLOR;
    float4 uv : TEXCOORD0;
};

[NumThreads(3, 1, 1)] // 3 threads per triangle
[OutputTopology("triangle")]
void MSMain(
    uint3 groupThreadID : SV_GroupThreadID,
    uint groupID : SV_GroupID,
    out vertices PixelInput outVerts[3],
    out indices uint3 outTris[1])
{
    // Fetch bindless buffers
    ByteAddressBuffer vertexBuffer = ResourceDescriptorHeap[_VertexBufferIndex];
    ByteAddressBuffer indexBuffer = ResourceDescriptorHeap[_IndexBufferIndex];

    // Compute the triangle’s vertex indices
    uint vertexId = groupThreadID.x;
    uint indexOffset = (groupID.x * 3 + vertexId) * 4; // uint32 index
    uint vertexIndex = indexBuffer.Load(indexOffset);

    // Load vertex attributes
    uint vertexOffset = vertexIndex * 80; // 80 bytes per vertex
    Vertex v;
    v.position = asfloat(vertexBuffer.Load4(vertexOffset + 0));
    v.normal = asfloat(vertexBuffer.Load4(vertexOffset + 16));
    v.tangent = asfloat(vertexBuffer.Load4(vertexOffset + 32));
    v.color = asfloat(vertexBuffer.Load4(vertexOffset + 48));
    v.uv = asfloat(vertexBuffer.Load4(vertexOffset + 64));

    SetMeshOutputCounts(3, 1);
    
    // Write vertex output
    outVerts[vertexId].position = v.position;
    outVerts[vertexId].color = v.color;
    outVerts[vertexId].uv = v.uv;

    // Thread 0 defines topology
    if (vertexId == 0)
    {
        outTris[0] = uint3(0, 1, 2);
    }
}

float4 PSMain(PixelInput input) : SV_TARGET
{
    float4 color1 = SAMPLE_TEXTURE2D_BINDLESS(_TextureIndex1, 0, input.uv.xy);
    float4 color2 = SAMPLE_TEXTURE2D_BINDLESS(_TextureIndex2, 0, input.uv.xy);
    float4 color3 = SAMPLE_TEXTURE2D_BINDLESS(_TextureIndex3, 0, input.uv.xy);
    float4 color4 = SAMPLE_TEXTURE2D_BINDLESS(_TextureIndex4, 0, input.uv.xy);
    
    float4 blendedColor = (color1 + color2 + color3 + color4) * 0.25f;
    return blendedColor * _Color;
}
