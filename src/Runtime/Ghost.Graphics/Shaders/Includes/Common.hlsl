#ifndef BUILTIN_COMMON_HLSL
#define BUILTIN_COMMON_HLSL

struct Vertex
{
    float4 position;
    float4 normal;
    float4 tangent;
    float4 uv;
    float4 color;
};

// Resource descriptor heap definitions

#define GLOBAL_TEXTURE2D_HEAP ResourceDescriptorHeap
#define GLOBAL_TEXTURE3D_HEAP ResourceDescriptorHeap
#define GLOBAL_TEXTURECUBE_HEAP ResourceDescriptorHeap
#define GLOBAL_TEXTURE2D_ARRAY_HEAP ResourceDescriptorHeap
#define GLOBAL_TEXTURECUBE_ARRAY_HEAP ResourceDescriptorHeap
#define GLOBAL_BUFFER_HEAP ResourceDescriptorHeap
#define GLOBAL_SAMPLER_HEAP SamplerDescriptorHeap

// Bindless resource type definitions

#define TEXTURE2D uint
#define TEXTURE3D uint
#define TEXTURECUBE uint
#define TEXTURE2D_ARRAY uint
#define TEXTURECUBE_ARRAY uint

#define SAMPLER uint

#define STRUCT_BUFFER uint
#define BYTE_ADDRESS_BUFFER uint


// Texture and sampler access macros

#define GET_TEXTURE2D(id) GLOBAL_TEXTURE2D_HEAP[id]
#define GET_TEXTURE2D_ARRAY(id) GLOBAL_TEXTURE2D_ARRAY_HEAP[id]
#define GET_TEXTURE3D(id) GLOBAL_TEXTURE3D_HEAP[id]
#define GET_TEXTURECUBE(id) GLOBAL_TEXTURECUBE_HEAP[id]
#define GET_TEXTURECUBE_ARRAY(id) GLOBAL_TEXTURECUBE_ARRAY_HEAP[id]
#define GET_BUFFER(id) GLOBAL_BUFFER_HEAP[id]
#define GET_SAMPLER(id) GLOBAL_SAMPLER_HEAP[id]

#define SAMPLE_TEXTURE2D(texId, sampId, uv) SampleTexture2D(texId, sampId, uv)
#define SAMPLE_TEXTURE2D_LEVEL(texId, sampId, uv, level) SampleTexture2DLevel(texId, sampId, uv, level)
#define SAMPLE_TEXTURE2D_ARRAY(texId, sampId, uvw) SampleTextureArray(texId, sampId, uvw)


#define ZERO_INIT(T) (T)0


static inline float4 SampleTexture2D(uint texId, uint sampId, float2 uv)
{
    Texture2D tex = GET_TEXTURE2D(texId);
    SamplerState samp = GET_SAMPLER(sampId);
    return tex.Sample(samp, uv);
}

static inline float4 SampleTexture2DLevel(uint texId, uint sampId, float2 uv, float level)
{
    Texture2D tex = GET_TEXTURE2D(texId);
    SamplerState samp = GET_SAMPLER(sampId);
    return tex.SampleLevel(samp, uv, level);
}

static inline float4 SampleTextureArray(uint texId, uint sampId, float3 uvw)
{
    Texture2DArray tex = GET_TEXTURE2D_ARRAY(texId);
    SamplerState samp = GET_SAMPLER(sampId);
    return tex.Sample(samp, uvw);
}

static inline Vertex LoadVertexData(uint vertexID, uint groupID, BYTE_ADDRESS_BUFFER vertexBuffer, BYTE_ADDRESS_BUFFER indexBuffer)
{
    ByteAddressBuffer vertices = GET_BUFFER(vertexBuffer);
    ByteAddressBuffer indices = GET_BUFFER(indexBuffer);

    // Compute the triangle’s vertex indices
    uint indexOffset = (groupID * 3 + vertexID) * 4; // uint32 index
    uint vertexIndex = indices.Load(indexOffset);

    return vertices.Load<Vertex>(vertexIndex * sizeof(Vertex));
}

template<typename T>
static inline T LoadData(BYTE_ADDRESS_BUFFER buffer, uint index)
{
    ByteAddressBuffer buf = GET_BUFFER(buffer);
    return buf.Load<T>(index * sizeof(T));
}

#endif // BUILTIN_COMMON_HLSL
