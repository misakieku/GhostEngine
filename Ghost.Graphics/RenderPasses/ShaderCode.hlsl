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

// SM 6.6 approach - direct access to global descriptor heap
SamplerState _MainSampler : register(s0);

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

    // Write vertex output
    outVerts[vertexId].position = v.position;
    outVerts[vertexId].color = v.color;
    outVerts[vertexId].uv = v.uv;

    // Thread 0 defines topology
    if (vertexId == 0)
    {
        SetMeshOutputCounts(3, 1);
        outTris[0] = uint3(0, 1, 2);
    }
}

float4 PSMain(PixelInput input) : SV_TARGET
{
    // SM 6.6 Modern Bindless Approach:
    // ResourceDescriptorHeap[index] directly accesses any texture in the heap
    Texture2D tex1 = ResourceDescriptorHeap[_TextureIndex1];
    Texture2D tex2 = ResourceDescriptorHeap[_TextureIndex2];
    Texture2D tex3 = ResourceDescriptorHeap[_TextureIndex3];
    Texture2D tex4 = ResourceDescriptorHeap[_TextureIndex4];
    
    // Sample the textures
    float4 color1 = tex1.Sample(_MainSampler, input.uv.xy);
    float4 color2 = tex2.Sample(_MainSampler, input.uv.xy);
    float4 color3 = tex3.Sample(_MainSampler, input.uv.xy);
    float4 color4 = tex4.Sample(_MainSampler, input.uv.xy);
    
    // Blend all textures together (simple average)
    float4 blendedColor = (color1 + color2 + color3 + color4) * 0.25f;
    
    return blendedColor * _Color;
}