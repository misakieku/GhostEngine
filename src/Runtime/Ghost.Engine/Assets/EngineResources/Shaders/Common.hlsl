#ifndef GHOST_COMMON_HLSL
#define GHOST_COMMON_HLSL

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


#define ZERO(T) (T)0
#define ZERO_INIT(T, V) (V) = ZERO(T)
#define ZERO_CREATE(T, V) T V = ZERO(T)

#define MAX_VERTICES_PER_MESHLET 64
#define MAX_TRIANGLES_PER_MESHLET 126

#define INVALID_BUFFER_INDEX 0xFFFFFFFF

float4 SampleTexture2D(uint texId, uint sampId, float2 uv)
{
    Texture2D tex = GET_TEXTURE2D(texId);
    SamplerState samp = GET_SAMPLER(sampId);
    return tex.Sample(samp, uv);
}

float4 SampleTexture2DLevel(uint texId, uint sampId, float2 uv, float level)
{
    Texture2D tex = GET_TEXTURE2D(texId);
    SamplerState samp = GET_SAMPLER(sampId);
    return tex.SampleLevel(samp, uv, level);
}

float4 SampleTextureArray(uint texId, uint sampId, float3 uvw)
{
    Texture2DArray tex = GET_TEXTURE2D_ARRAY(texId);
    SamplerState samp = GET_SAMPLER(sampId);
    return tex.Sample(samp, uvw);
}

template<typename T>
T LoadData(BYTE_ADDRESS_BUFFER buffer, uint index)
{
    ByteAddressBuffer buf = GET_BUFFER(buffer);
    return buf.Load<T>(index * sizeof(T));
}

template<typename T>
T LoadDataWithOffset(BYTE_ADDRESS_BUFFER buffer, uint index, uint offset)
{
    ByteAddressBuffer buf = GET_BUFFER(buffer);
    return buf.Load<T>(index * sizeof(T) + offset);
}

/// Resolves a meshlet's local material index to a global bindless CBuffer descriptor index.
/// Uses the two-buffer indirection: PaletteOffsetBuffer → MaterialIndexBuffer → CBuffer.
///   paletteOffsetBuffer  : from FrameData — one uint per palette, base offset into materialIndexBuffer
///   materialIndexBuffer  : from FrameData — packed bindless CBuffer indices for all palettes
///   paletteIndex         : per-instance value from InstanceData.materialPaletteIndex
///   localMaterialIndex   : per-meshlet value from Meshlet.packedCounts byte 2
uint LoadMaterialBindlessIndex(BYTE_ADDRESS_BUFFER paletteOffsetBuffer, BYTE_ADDRESS_BUFFER materialIndexBuffer, uint paletteIndex, uint localMaterialIndex)
{
    ByteAddressBuffer offsets = GET_BUFFER(paletteOffsetBuffer);
    ByteAddressBuffer indices = GET_BUFFER(materialIndexBuffer);
    uint base = offsets.Load(paletteIndex * 4);
    return indices.Load((base + localMaterialIndex) * 4);
}

#endif // GHOST_COMMON_HLSL
