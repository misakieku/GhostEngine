using System.Runtime.InteropServices;

namespace Ghost.Zeux.MeshOptimizer;

public static unsafe partial class Api
{
    private const string _DLL_NAME = "meshoptimizer";

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint meshopt_generateVertexRemap([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const void *")] void* vertices, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_size);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint meshopt_generateVertexRemapMulti([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("const struct meshopt_Stream *")] meshopt_Stream* streams, [NativeTypeName("size_t")] nuint stream_count);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint meshopt_generateVertexRemapCustom([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride, [NativeTypeName("int (*)(void *, unsigned int, unsigned int)")] delegate* unmanaged[Cdecl]<void*, uint, uint, int> callback, void* context);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void meshopt_remapVertexBuffer(void* destination, [NativeTypeName("const void *")] void* vertices, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_size, [NativeTypeName("const unsigned int *")] uint* remap);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void meshopt_remapIndexBuffer([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const unsigned int *")] uint* remap);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void meshopt_generateShadowIndexBuffer([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const void *")] void* vertices, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_size, [NativeTypeName("size_t")] nuint vertex_stride);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void meshopt_generateShadowIndexBufferMulti([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("const struct meshopt_Stream *")] meshopt_Stream* streams, [NativeTypeName("size_t")] nuint stream_count);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void meshopt_generatePositionRemap([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void meshopt_generateAdjacencyIndexBuffer([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void meshopt_generateTessellationIndexBuffer([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint meshopt_generateProvokingIndexBuffer([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("unsigned int *")] uint* reorder, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("size_t")] nuint vertex_count);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void meshopt_optimizeVertexCache([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("size_t")] nuint vertex_count);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void meshopt_optimizeVertexCacheStrip([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("size_t")] nuint vertex_count);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void meshopt_optimizeVertexCacheFifo([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("unsigned int")] uint cache_size);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void meshopt_optimizeOverdraw([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride, float threshold);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint meshopt_optimizeVertexFetch(void* destination, [NativeTypeName("unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const void *")] void* vertices, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_size);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint meshopt_optimizeVertexFetchRemap([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("size_t")] nuint vertex_count);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint meshopt_encodeIndexBuffer([NativeTypeName("unsigned char *")] byte* buffer, [NativeTypeName("size_t")] nuint buffer_size, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint meshopt_encodeIndexBufferBound([NativeTypeName("size_t")] nuint index_count, [NativeTypeName("size_t")] nuint vertex_count);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void meshopt_encodeIndexVersion(int version);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int meshopt_decodeIndexBuffer(void* destination, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("size_t")] nuint index_size, [NativeTypeName("const unsigned char *")] byte* buffer, [NativeTypeName("size_t")] nuint buffer_size);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int meshopt_decodeIndexVersion([NativeTypeName("const unsigned char *")] byte* buffer, [NativeTypeName("size_t")] nuint buffer_size);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint meshopt_encodeIndexSequence([NativeTypeName("unsigned char *")] byte* buffer, [NativeTypeName("size_t")] nuint buffer_size, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint meshopt_encodeIndexSequenceBound([NativeTypeName("size_t")] nuint index_count, [NativeTypeName("size_t")] nuint vertex_count);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int meshopt_decodeIndexSequence(void* destination, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("size_t")] nuint index_size, [NativeTypeName("const unsigned char *")] byte* buffer, [NativeTypeName("size_t")] nuint buffer_size);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint meshopt_encodeVertexBuffer([NativeTypeName("unsigned char *")] byte* buffer, [NativeTypeName("size_t")] nuint buffer_size, [NativeTypeName("const void *")] void* vertices, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_size);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint meshopt_encodeVertexBufferBound([NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_size);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint meshopt_encodeVertexBufferLevel([NativeTypeName("unsigned char *")] byte* buffer, [NativeTypeName("size_t")] nuint buffer_size, [NativeTypeName("const void *")] void* vertices, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_size, int level, int version);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void meshopt_encodeVertexVersion(int version);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int meshopt_decodeVertexBuffer(void* destination, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_size, [NativeTypeName("const unsigned char *")] byte* buffer, [NativeTypeName("size_t")] nuint buffer_size);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int meshopt_decodeVertexVersion([NativeTypeName("const unsigned char *")] byte* buffer, [NativeTypeName("size_t")] nuint buffer_size);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void meshopt_decodeFilterOct(void* buffer, [NativeTypeName("size_t")] nuint count, [NativeTypeName("size_t")] nuint stride);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void meshopt_decodeFilterQuat(void* buffer, [NativeTypeName("size_t")] nuint count, [NativeTypeName("size_t")] nuint stride);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void meshopt_decodeFilterExp(void* buffer, [NativeTypeName("size_t")] nuint count, [NativeTypeName("size_t")] nuint stride);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void meshopt_decodeFilterColor(void* buffer, [NativeTypeName("size_t")] nuint count, [NativeTypeName("size_t")] nuint stride);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void meshopt_encodeFilterOct(void* destination, [NativeTypeName("size_t")] nuint count, [NativeTypeName("size_t")] nuint stride, int bits, [NativeTypeName("const float *")] float* data);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void meshopt_encodeFilterQuat(void* destination, [NativeTypeName("size_t")] nuint count, [NativeTypeName("size_t")] nuint stride, int bits, [NativeTypeName("const float *")] float* data);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void meshopt_encodeFilterExp(void* destination, [NativeTypeName("size_t")] nuint count, [NativeTypeName("size_t")] nuint stride, int bits, [NativeTypeName("const float *")] float* data, [NativeTypeName("enum meshopt_EncodeExpMode")] meshopt_EncodeExpMode mode);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void meshopt_encodeFilterColor(void* destination, [NativeTypeName("size_t")] nuint count, [NativeTypeName("size_t")] nuint stride, int bits, [NativeTypeName("const float *")] float* data);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint meshopt_simplify([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride, [NativeTypeName("size_t")] nuint target_index_count, float target_error, [NativeTypeName("unsigned int")] uint options, float* result_error);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint meshopt_simplifyWithAttributes([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride, [NativeTypeName("const float *")] float* vertex_attributes, [NativeTypeName("size_t")] nuint vertex_attributes_stride, [NativeTypeName("const float *")] float* attribute_weights, [NativeTypeName("size_t")] nuint attribute_count, [NativeTypeName("const unsigned char *")] byte* vertex_lock, [NativeTypeName("size_t")] nuint target_index_count, float target_error, [NativeTypeName("unsigned int")] uint options, float* result_error);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint meshopt_simplifyWithUpdate([NativeTypeName("unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride, float* vertex_attributes, [NativeTypeName("size_t")] nuint vertex_attributes_stride, [NativeTypeName("const float *")] float* attribute_weights, [NativeTypeName("size_t")] nuint attribute_count, [NativeTypeName("const unsigned char *")] byte* vertex_lock, [NativeTypeName("size_t")] nuint target_index_count, float target_error, [NativeTypeName("unsigned int")] uint options, float* result_error);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint meshopt_simplifySloppy([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride, [NativeTypeName("const unsigned char *")] byte* vertex_lock, [NativeTypeName("size_t")] nuint target_index_count, float target_error, float* result_error);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint meshopt_simplifyPrune([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride, float target_error);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint meshopt_simplifyPoints([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride, [NativeTypeName("const float *")] float* vertex_colors, [NativeTypeName("size_t")] nuint vertex_colors_stride, float color_weight, [NativeTypeName("size_t")] nuint target_vertex_count);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern float meshopt_simplifyScale([NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint meshopt_stripify([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("unsigned int")] uint restart_index);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint meshopt_stripifyBound([NativeTypeName("size_t")] nuint index_count);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint meshopt_unstripify([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("unsigned int")] uint restart_index);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint meshopt_unstripifyBound([NativeTypeName("size_t")] nuint index_count);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("struct meshopt_VertexCacheStatistics")]
    public static extern meshopt_VertexCacheStatistics meshopt_analyzeVertexCache([NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("unsigned int")] uint cache_size, [NativeTypeName("unsigned int")] uint warp_size, [NativeTypeName("unsigned int")] uint primgroup_size);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("struct meshopt_VertexFetchStatistics")]
    public static extern meshopt_VertexFetchStatistics meshopt_analyzeVertexFetch([NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_size);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("struct meshopt_OverdrawStatistics")]
    public static extern meshopt_OverdrawStatistics meshopt_analyzeOverdraw([NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("struct meshopt_CoverageStatistics")]
    public static extern meshopt_CoverageStatistics meshopt_analyzeCoverage([NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint meshopt_buildMeshlets([NativeTypeName("struct meshopt_Meshlet *")] meshopt_Meshlet* meshlets, [NativeTypeName("unsigned int *")] uint* meshlet_vertices, [NativeTypeName("unsigned char *")] byte* meshlet_triangles, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride, [NativeTypeName("size_t")] nuint max_vertices, [NativeTypeName("size_t")] nuint max_triangles, float cone_weight);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint meshopt_buildMeshletsScan([NativeTypeName("struct meshopt_Meshlet *")] meshopt_Meshlet* meshlets, [NativeTypeName("unsigned int *")] uint* meshlet_vertices, [NativeTypeName("unsigned char *")] byte* meshlet_triangles, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint max_vertices, [NativeTypeName("size_t")] nuint max_triangles);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint meshopt_buildMeshletsBound([NativeTypeName("size_t")] nuint index_count, [NativeTypeName("size_t")] nuint max_vertices, [NativeTypeName("size_t")] nuint max_triangles);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint meshopt_buildMeshletsFlex([NativeTypeName("struct meshopt_Meshlet *")] meshopt_Meshlet* meshlets, [NativeTypeName("unsigned int *")] uint* meshlet_vertices, [NativeTypeName("unsigned char *")] byte* meshlet_triangles, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride, [NativeTypeName("size_t")] nuint max_vertices, [NativeTypeName("size_t")] nuint min_triangles, [NativeTypeName("size_t")] nuint max_triangles, float cone_weight, float split_factor);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint meshopt_buildMeshletsSpatial([NativeTypeName("struct meshopt_Meshlet *")] meshopt_Meshlet* meshlets, [NativeTypeName("unsigned int *")] uint* meshlet_vertices, [NativeTypeName("unsigned char *")] byte* meshlet_triangles, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride, [NativeTypeName("size_t")] nuint max_vertices, [NativeTypeName("size_t")] nuint min_triangles, [NativeTypeName("size_t")] nuint max_triangles, float fill_weight);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void meshopt_optimizeMeshlet([NativeTypeName("unsigned int *")] uint* meshlet_vertices, [NativeTypeName("unsigned char *")] byte* meshlet_triangles, [NativeTypeName("size_t")] nuint triangle_count, [NativeTypeName("size_t")] nuint vertex_count);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("struct meshopt_Bounds")]
    public static extern meshopt_Bounds meshopt_computeClusterBounds([NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("struct meshopt_Bounds")]
    public static extern meshopt_Bounds meshopt_computeMeshletBounds([NativeTypeName("const unsigned int *")] uint* meshlet_vertices, [NativeTypeName("const unsigned char *")] byte* meshlet_triangles, [NativeTypeName("size_t")] nuint triangle_count, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("struct meshopt_Bounds")]
    public static extern meshopt_Bounds meshopt_computeSphereBounds([NativeTypeName("const float *")] float* positions, [NativeTypeName("size_t")] nuint count, [NativeTypeName("size_t")] nuint positions_stride, [NativeTypeName("const float *")] float* radii, [NativeTypeName("size_t")] nuint radii_stride);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("size_t")]
    public static extern nuint meshopt_partitionClusters([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* cluster_indices, [NativeTypeName("size_t")] nuint total_index_count, [NativeTypeName("const unsigned int *")] uint* cluster_index_counts, [NativeTypeName("size_t")] nuint cluster_count, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride, [NativeTypeName("size_t")] nuint target_partition_size);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void meshopt_spatialSortRemap([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void meshopt_spatialSortTriangles([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void meshopt_spatialClusterPoints([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride, [NativeTypeName("size_t")] nuint cluster_size);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("unsigned short")]
    public static extern ushort meshopt_quantizeHalf(float v);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern float meshopt_quantizeFloat(float v, int N);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern float meshopt_dequantizeHalf([NativeTypeName("unsigned short")] ushort h);

    [DllImport(_DLL_NAME, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void meshopt_setAllocator([NativeTypeName("void *(*)(size_t) __attribute__((cdecl))")] delegate* unmanaged[Cdecl]<nuint, void*> allocate, [NativeTypeName("void (*)(void *) __attribute__((cdecl))")] delegate* unmanaged[Cdecl]<void*, void> deallocate);

    public static int meshopt_quantizeUnorm(float v, int N)
    {
        var scale = (float)((1 << N) - 1);

        v = (v >= 0) ? v : 0;
        v = (v <= 1) ? v : 1;
        return (int)(v * scale + 0.5f);
    }

    public static int meshopt_quantizeSnorm(float v, int N)
    {
        var scale = (float)((1 << (N - 1)) - 1);
        var round = (v >= 0 ? 0.5f : -0.5f);

        v = (v >= -1) ? v : -1;
        v = (v <= +1) ? v : +1;
        return (int)(v * scale + round);
    }

    [return: NativeTypeName("size_t")]
    public static nuint meshopt_encodeVertexBufferLevel([NativeTypeName("unsigned char *")] byte* buffer, [NativeTypeName("size_t")] nuint buffer_size, [NativeTypeName("const void *")] void* vertices, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_size, int level)
    {
        return meshopt_encodeVertexBufferLevel(buffer, buffer_size, vertices, vertex_count, vertex_size, level, unchecked(-1));
    }
}
