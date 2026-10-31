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
// Act as a hint to the code maintainer to understand what type of resource is being used.

#define TEXTURE2D uint
#define TEXTURE3D uint
#define TEXTURECUBE uint
#define TEXTURE2D_ARRAY uint
#define TEXTURECUBE_ARRAY uint

#define SAMPLER uint

#define STRUCT_BUFFER uint
#define BYTE_ADDRESS_BUFFER uint


// Texture and sampler access macros
// Always prefer using those macros instead of directly accessing the global descriptor heaps, as we may add other grphics APIs in the future which may use different resource binding strategies.

#define GET_TEXTURE2D(id) GLOBAL_TEXTURE2D_HEAP[id]
#define GET_TEXTURE2D_ARRAY(id) GLOBAL_TEXTURE2D_ARRAY_HEAP[id]
#define GET_TEXTURE3D(id) GLOBAL_TEXTURE3D_HEAP[id]
#define GET_TEXTURECUBE(id) GLOBAL_TEXTURECUBE_HEAP[id]
#define GET_TEXTURECUBE_ARRAY(id) GLOBAL_TEXTURECUBE_ARRAY_HEAP[id]
#define GET_BUFFER(id) GLOBAL_BUFFER_HEAP[id]
#define GET_SAMPLER(id) GLOBAL_SAMPLER_HEAP[id]
#define GET_TLAS(id) GLOBAL_BUFFER_HEAP[id]



// Static sampler

#define PointClampSampler 1
#define PointRepeatSampler 2

#define LinearClampSampler 3
#define LinearRepeatSampler 4

#define TrilinearClampSampler 5
#define TrilinearRepeatSampler 6

#define AnisotropicClampSampler 7
#define AnisotropicRepeatSampler 8

#define LinearClampCompareGESampler 9


#define MAX_VERTICES_PER_MESHLET 64
#define MAX_TRIANGLES_PER_MESHLET 126

#define INVALID_BUFFER_INDEX 0
#define IS_VALID_BUFFER(index) ((index) != INVALID_BUFFER_INDEX)
#define IS_VALID_TEXTURE(index) ((index) != INVALID_BUFFER_INDEX)

template<typename T>
T SampleTexture2D(TEXTURE2D texId, SAMPLER sampId, float2 uv)
{
    Texture2D<T> tex = GET_TEXTURE2D(texId);
    SamplerState samp = GET_SAMPLER(sampId);
    return tex.Sample(samp, uv);
}

template<typename T>
T SampleTexture2DLevel(TEXTURE2D texId, SAMPLER sampId, float2 uv, float level)
{
    Texture2D<T> tex = GET_TEXTURE2D(texId);
    SamplerState samp = GET_SAMPLER(sampId);
    return tex.SampleLevel(samp, uv, level);
}

template<typename T>
T SampleTextureArray(TEXTURE2D_ARRAY texId, SAMPLER sampId, float3 uvw)
{
    Texture2DArray<T> tex = GET_TEXTURE2D_ARRAY(texId);
    SamplerState samp = GET_SAMPLER(sampId);
    return tex.Sample(samp, uvw);
}

template<typename T>
T SampleTexture2DGrad(TEXTURE2D texId, SAMPLER sampId, float2 uv, float2 ddx, float2 ddy)
{
    Texture2D<T> tex = GET_TEXTURE2D(texId);
    SamplerState samp = GET_SAMPLER(sampId);
    return tex.SampleGrad(samp, uv, ddx, ddy);
}

template<typename T>
T SampleTextureCube(TEXTURECUBE texId, SAMPLER sampId, float3 dir)
{
    TextureCube<T> tex = GET_TEXTURECUBE(texId);
    SamplerState samp = GET_SAMPLER(sampId);
    return tex.Sample(samp, dir);
}

template<typename T>
T SampleTextureCubeLevel(TEXTURECUBE texId, SAMPLER sampId, float3 dir, float level)
{
    TextureCube<T> tex = GET_TEXTURECUBE(texId);
    SamplerState samp = GET_SAMPLER(sampId);
    return tex.SampleLevel(samp, dir, level);
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

template<typename T>
T LoadDataAtOffset(BYTE_ADDRESS_BUFFER buffer, uint offset)
{
    ByteAddressBuffer buf = GET_BUFFER(buffer);
    return buf.Load<T>(offset);
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
