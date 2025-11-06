#ifndef COMMON_HLSL
#define COMMON_HLSL

#undef USE_TRADITIONAL_BINDLESS // Just for testing, this should be handled by engine feature level.


// Resource descriptor heap definitions

#if defined(USE_TRADITIONAL_BINDLESS)
#define GLOBAL_TEXTURE2D_HEAP_SIZE 32768
#define GLOBAL_TEXTURE3D_HEAP_SIZE 32
#define GLOBAL_TEXTURECUBE_HEAP_SIZE 32
#define GLOBAL_TEXTURE2D_ARRAY_HEAP_SIZE 256
#define GLOBAL_TEXTURECUBE_ARRAY_HEAP_SIZE 32
#define GLOBAL_SAMPLER_HEAP_SIZE 32

#define GLOBAL_TEXTURE2D_HEAP GlobalTexture2DHeap
#define GLOBAL_TEXTURE3D_HEAP GlobalTexture3DHeap
#define GLOBAL_TEXTURECUBE_HEAP GlobalTextureCubeHeap
#define GLOBAL_TEXTURE2D_ARRAY_HEAP GlobalTexture2DArrayHeap
#define GLOBAL_TEXTURECUBE_ARRAY_HEAP GlobalTextureCubeArrayHeap
#define GLOBAL_SAMPLER_HEAP GlobalSamplerHeap
#else
#define GLOBAL_TEXTURE2D_HEAP ResourceDescriptorHeap
#define GLOBAL_TEXTURE3D_HEAP ResourceDescriptorHeap
#define GLOBAL_TEXTURECUBE_HEAP ResourceDescriptorHeap
#define GLOBAL_TEXTURE2D_ARRAY_HEAP ResourceDescriptorHeap
#define GLOBAL_TEXTURECUBE_ARRAY_HEAP ResourceDescriptorHeap
#define GLOBAL_SAMPLER_HEAP SamplerDescriptorHeap
#endif


// Bindless resource type definitions

#define TEXTURE2D_BINDLESS uint
#define TEXTURE3D_BINDLESS uint
#define TEXTURECUBE_BINDLESS uint
#define TEXTURE2D_ARRAY_BINDLESS uint
#define TEXTURECUBE_ARRAY_BINDLESS uint

#define SAMPLER_BINDLESS uint

#define STRUCT_BUFFER_BINDLESS uint
#define BYTE_ADDRESS_BUFFER_BINDLESS uint

#define TEXTURE2D Texture2D<float4>
#define TEXTURE3D Texture3D<float4>
#define TEXTURECUBE TextureCube<float4>
#define TEXTURE2D_ARRAY Texture2DArray<float4>
#define TEXTURECUBE_ARRAY TextureCubeArray<float4>

#define SAMPLER SamplerState

#define STRUCT_BUFFER(type) StructuredBuffer<type>
#define BYTE_ADDRESS_BUFFER ByteAddressBuffer


// Texture and sampler access macros

#define GET_TEXTURE2D_BINDLESS(id) GLOBAL_TEXTURE2D_HEAP[id]
#define GET_TEXTURE2D_ARRAY_BINDLESS(id) GLOBAL_TEXTURE2D_ARRAY_HEAP[id]
#define GET_TEXTURE3D_BINDLESS(id) GLOBAL_TEXTURE3D_HEAP[id]
#define GET_TEXTURECUBE_BINDLESS(id) GLOBAL_TEXTURECUBE_HEAP[id]
#define GET_TEXTURECUBE_ARRAY_BINDLESS(id) GLOBAL_TEXTURECUBE_ARRAY_HEAP[id]
#define GET_SAMPLER_BINDLESS(id) GLOBAL_SAMPLER_HEAP[id]


// Texture sampling macros

#define SAMPLE_TEXTURE2D(tex, samp, uv) tex.Sample(samp, uv)
#define SAMPLE_TEXTURE2D_LEVEL(tex, samp, uv, level) tex.SampleLevel(samp, uv, level)
#define SAMPLE_TEXTURE2D_BINDLESS(texId, sampId, uv) GET_TEXTURE2D_BINDLESS(texId).Sample(GET_BINDLESS_SAMPLER(sampId), uv)
#define SAMPLE_TEXTURE2D_LEVEL_BINDLESS(texId, sampId, uv, level) GET_TEXTURE2D_BINDLESS(texId).SampleLevel(GET_BINDLESS_SAMPLER(sampId), uv, level)

#define SAMPLE_TEXTURE2D_ARRAY(tex, samp, uv, index) tex.Sample(samp, uv, index)
#define SAMPLE_TEXTURE2D_ARRAY_BINDLESS(texId, sampId, uv, index) GET_TEXTURE2D_ARRAY_BINDLESS(texId).Sample(GET_BINDLESS_SAMPLER(sampId), uv, index)



#define MESH_SHADER_THREADS(x) [NumThreads(x, 1, 1)]

#endif // COMMON_HLSL
