using System.Runtime.InteropServices;

namespace Ghost.MeshOptimizer
{
    public static unsafe partial class Api
    {
        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_generateVertexRemap"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint meshopt_generateVertexRemap([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const void *")] void* vertices, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_size);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_generateVertexRemapMulti"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint meshopt_generateVertexRemapMulti([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("const struct meshopt_Stream *")] meshopt_Stream* streams, [NativeTypeName("size_t")] nuint stream_count);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_generateVertexRemapCustom"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint meshopt_generateVertexRemapCustom([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride, [NativeTypeName("int (*)(void *, unsigned int, unsigned int)")] delegate* unmanaged[Cdecl]<void*, uint, uint, int> callback, void* context);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_remapVertexBuffer"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void meshopt_remapVertexBuffer(void* destination, [NativeTypeName("const void *")] void* vertices, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_size, [NativeTypeName("const unsigned int *")] uint* remap);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_remapIndexBuffer"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void meshopt_remapIndexBuffer([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const unsigned int *")] uint* remap);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_filterIndexBuffer"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint meshopt_filterIndexBuffer([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const void *")] void* vertices, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_size, [NativeTypeName("size_t")] nuint vertex_stride);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_filterIndexBufferMulti"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint meshopt_filterIndexBufferMulti([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("const struct meshopt_Stream *")] meshopt_Stream* streams, [NativeTypeName("size_t")] nuint stream_count);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_generateShadowIndexBuffer"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void meshopt_generateShadowIndexBuffer([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const void *")] void* vertices, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_size, [NativeTypeName("size_t")] nuint vertex_stride);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_generateShadowIndexBufferMulti"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void meshopt_generateShadowIndexBufferMulti([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("const struct meshopt_Stream *")] meshopt_Stream* streams, [NativeTypeName("size_t")] nuint stream_count);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_generatePositionRemap"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void meshopt_generatePositionRemap([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_generateAdjacencyIndexBuffer"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void meshopt_generateAdjacencyIndexBuffer([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_generateTessellationIndexBuffer"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void meshopt_generateTessellationIndexBuffer([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_generateProvokingIndexBuffer"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint meshopt_generateProvokingIndexBuffer([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("unsigned int *")] uint* reorder, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("size_t")] nuint vertex_count);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_optimizeVertexCache"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void meshopt_optimizeVertexCache([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("size_t")] nuint vertex_count);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_optimizeVertexCacheStrip"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void meshopt_optimizeVertexCacheStrip([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("size_t")] nuint vertex_count);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_optimizeVertexCacheFifo"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void meshopt_optimizeVertexCacheFifo([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("unsigned int")] uint cache_size);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_optimizeOverdraw"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void meshopt_optimizeOverdraw([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride, float threshold);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_optimizeVertexFetch"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint meshopt_optimizeVertexFetch(void* destination, [NativeTypeName("unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const void *")] void* vertices, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_size);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_optimizeVertexFetchRemap"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint meshopt_optimizeVertexFetchRemap([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("size_t")] nuint vertex_count);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_encodeIndexBuffer"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint meshopt_encodeIndexBuffer([NativeTypeName("unsigned char *")] byte* buffer, [NativeTypeName("size_t")] nuint buffer_size, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_encodeIndexBufferBound"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint meshopt_encodeIndexBufferBound([NativeTypeName("size_t")] nuint index_count, [NativeTypeName("size_t")] nuint vertex_count);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_encodeIndexVersion"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void meshopt_encodeIndexVersion(int version);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_decodeIndexBuffer"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int meshopt_decodeIndexBuffer(void* destination, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("size_t")] nuint index_size, [NativeTypeName("const unsigned char *")] byte* buffer, [NativeTypeName("size_t")] nuint buffer_size);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_decodeIndexVersion"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int meshopt_decodeIndexVersion([NativeTypeName("const unsigned char *")] byte* buffer, [NativeTypeName("size_t")] nuint buffer_size);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_encodeIndexSequence"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint meshopt_encodeIndexSequence([NativeTypeName("unsigned char *")] byte* buffer, [NativeTypeName("size_t")] nuint buffer_size, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_encodeIndexSequenceBound"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint meshopt_encodeIndexSequenceBound([NativeTypeName("size_t")] nuint index_count, [NativeTypeName("size_t")] nuint vertex_count);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_decodeIndexSequence"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int meshopt_decodeIndexSequence(void* destination, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("size_t")] nuint index_size, [NativeTypeName("const unsigned char *")] byte* buffer, [NativeTypeName("size_t")] nuint buffer_size);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_encodeMeshlet"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint meshopt_encodeMeshlet([NativeTypeName("unsigned char *")] byte* buffer, [NativeTypeName("size_t")] nuint buffer_size, [NativeTypeName("const unsigned int *")] uint* vertices, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("const unsigned char *")] byte* triangles, [NativeTypeName("size_t")] nuint triangle_count);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_encodeMeshletBound"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint meshopt_encodeMeshletBound([NativeTypeName("size_t")] nuint max_vertices, [NativeTypeName("size_t")] nuint max_triangles);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_decodeMeshlet"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int meshopt_decodeMeshlet(void* vertices, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_size, void* triangles, [NativeTypeName("size_t")] nuint triangle_count, [NativeTypeName("size_t")] nuint triangle_size, [NativeTypeName("const unsigned char *")] byte* buffer, [NativeTypeName("size_t")] nuint buffer_size);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_decodeMeshletRaw"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int meshopt_decodeMeshletRaw([NativeTypeName("unsigned int *")] uint* vertices, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("unsigned int *")] uint* triangles, [NativeTypeName("size_t")] nuint triangle_count, [NativeTypeName("const unsigned char *")] byte* buffer, [NativeTypeName("size_t")] nuint buffer_size);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_encodeVertexBuffer"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint meshopt_encodeVertexBuffer([NativeTypeName("unsigned char *")] byte* buffer, [NativeTypeName("size_t")] nuint buffer_size, [NativeTypeName("const void *")] void* vertices, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_size);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_encodeVertexBufferBound"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint meshopt_encodeVertexBufferBound([NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_size);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_encodeVertexBufferLevel"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint meshopt_encodeVertexBufferLevel([NativeTypeName("unsigned char *")] byte* buffer, [NativeTypeName("size_t")] nuint buffer_size, [NativeTypeName("const void *")] void* vertices, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_size, int level, int version);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_encodeVertexVersion"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void meshopt_encodeVertexVersion(int version);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_decodeVertexBuffer"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int meshopt_decodeVertexBuffer(void* destination, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_size, [NativeTypeName("const unsigned char *")] byte* buffer, [NativeTypeName("size_t")] nuint buffer_size);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_decodeVertexVersion"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int meshopt_decodeVertexVersion([NativeTypeName("const unsigned char *")] byte* buffer, [NativeTypeName("size_t")] nuint buffer_size);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_decodeFilterOct"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void meshopt_decodeFilterOct(void* buffer, [NativeTypeName("size_t")] nuint count, [NativeTypeName("size_t")] nuint stride);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_decodeFilterQuat"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void meshopt_decodeFilterQuat(void* buffer, [NativeTypeName("size_t")] nuint count, [NativeTypeName("size_t")] nuint stride);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_decodeFilterExp"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void meshopt_decodeFilterExp(void* buffer, [NativeTypeName("size_t")] nuint count, [NativeTypeName("size_t")] nuint stride);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_decodeFilterColor"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void meshopt_decodeFilterColor(void* buffer, [NativeTypeName("size_t")] nuint count, [NativeTypeName("size_t")] nuint stride);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_encodeFilterOct"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void meshopt_encodeFilterOct(void* destination, [NativeTypeName("size_t")] nuint count, [NativeTypeName("size_t")] nuint stride, int bits, [NativeTypeName("const float *")] float* data);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_encodeFilterQuat"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void meshopt_encodeFilterQuat(void* destination, [NativeTypeName("size_t")] nuint count, [NativeTypeName("size_t")] nuint stride, int bits, [NativeTypeName("const float *")] float* data);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_encodeFilterExp"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void meshopt_encodeFilterExp(void* destination, [NativeTypeName("size_t")] nuint count, [NativeTypeName("size_t")] nuint stride, int bits, [NativeTypeName("const float *")] float* data, [NativeTypeName("enum meshopt_EncodeExpMode")] meshopt_EncodeExpMode mode);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_encodeFilterColor"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void meshopt_encodeFilterColor(void* destination, [NativeTypeName("size_t")] nuint count, [NativeTypeName("size_t")] nuint stride, int bits, [NativeTypeName("const float *")] float* data);

        public const int meshopt_SimplifyLockBorder = 1 << 0;
        public const int meshopt_SimplifySparse = 1 << 1;
        public const int meshopt_SimplifyErrorAbsolute = 1 << 2;
        public const int meshopt_SimplifyPrune = 1 << 3;
        public const int meshopt_SimplifyRegularize = 1 << 4;
        public const int meshopt_SimplifyPermissive = 1 << 5;
        public const int meshopt_SimplifyRegularizeLight = 1 << 6;
        public const int meshopt_SimplifyPreserveFolds = 1 << 7;

        public const int meshopt_SimplifyVertex_Lock = 1 << 0;
        public const int meshopt_SimplifyVertex_Protect = 1 << 1;
        public const int meshopt_SimplifyVertex_Priority = 1 << 2;

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_simplify"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint meshopt_simplify([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride, [NativeTypeName("size_t")] nuint target_index_count, float target_error, [NativeTypeName("unsigned int")] uint options, float* result_error);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_simplifyWithAttributes"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint meshopt_simplifyWithAttributes([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride, [NativeTypeName("const float *")] float* vertex_attributes, [NativeTypeName("size_t")] nuint vertex_attributes_stride, [NativeTypeName("const float *")] float* attribute_weights, [NativeTypeName("size_t")] nuint attribute_count, [NativeTypeName("const unsigned char *")] byte* vertex_lock, [NativeTypeName("size_t")] nuint target_index_count, float target_error, [NativeTypeName("unsigned int")] uint options, float* result_error);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_simplifyWithUpdate"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint meshopt_simplifyWithUpdate([NativeTypeName("unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride, float* vertex_attributes, [NativeTypeName("size_t")] nuint vertex_attributes_stride, [NativeTypeName("const float *")] float* attribute_weights, [NativeTypeName("size_t")] nuint attribute_count, [NativeTypeName("const unsigned char *")] byte* vertex_lock, [NativeTypeName("size_t")] nuint target_index_count, float target_error, [NativeTypeName("unsigned int")] uint options, float* result_error);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_simplifySloppy"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint meshopt_simplifySloppy([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride, [NativeTypeName("const unsigned char *")] byte* vertex_lock, [NativeTypeName("size_t")] nuint target_index_count, float target_error, float* result_error);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_simplifyPrune"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint meshopt_simplifyPrune([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride, float target_error);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_simplifyPoints"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint meshopt_simplifyPoints([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride, [NativeTypeName("const float *")] float* vertex_colors, [NativeTypeName("size_t")] nuint vertex_colors_stride, float color_weight, [NativeTypeName("size_t")] nuint target_vertex_count);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_simplifyScale"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float meshopt_simplifyScale([NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_stripify"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint meshopt_stripify([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("unsigned int")] uint restart_index);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_stripifyBound"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint meshopt_stripifyBound([NativeTypeName("size_t")] nuint index_count);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_unstripify"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint meshopt_unstripify([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("unsigned int")] uint restart_index);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_unstripifyBound"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint meshopt_unstripifyBound([NativeTypeName("size_t")] nuint index_count);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_analyzeVertexCache"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("struct meshopt_VertexCacheStatistics")]
        public static extern meshopt_VertexCacheStatistics meshopt_analyzeVertexCache([NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("unsigned int")] uint cache_size, [NativeTypeName("unsigned int")] uint warp_size, [NativeTypeName("unsigned int")] uint primgroup_size);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_analyzeVertexFetch"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("struct meshopt_VertexFetchStatistics")]
        public static extern meshopt_VertexFetchStatistics meshopt_analyzeVertexFetch([NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_size);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_analyzeOverdraw"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("struct meshopt_OverdrawStatistics")]
        public static extern meshopt_OverdrawStatistics meshopt_analyzeOverdraw([NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_analyzeCoverage"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("struct meshopt_CoverageStatistics")]
        public static extern meshopt_CoverageStatistics meshopt_analyzeCoverage([NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_buildMeshlets"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint meshopt_buildMeshlets([NativeTypeName("struct meshopt_Meshlet *")] meshopt_Meshlet* meshlets, [NativeTypeName("unsigned int *")] uint* meshlet_vertices, [NativeTypeName("unsigned char *")] byte* meshlet_triangles, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride, [NativeTypeName("size_t")] nuint max_vertices, [NativeTypeName("size_t")] nuint max_triangles, float cone_weight);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_buildMeshletsScan"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint meshopt_buildMeshletsScan([NativeTypeName("struct meshopt_Meshlet *")] meshopt_Meshlet* meshlets, [NativeTypeName("unsigned int *")] uint* meshlet_vertices, [NativeTypeName("unsigned char *")] byte* meshlet_triangles, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint max_vertices, [NativeTypeName("size_t")] nuint max_triangles);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_buildMeshletsBound"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint meshopt_buildMeshletsBound([NativeTypeName("size_t")] nuint index_count, [NativeTypeName("size_t")] nuint max_vertices, [NativeTypeName("size_t")] nuint max_triangles);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_buildMeshletsFlex"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint meshopt_buildMeshletsFlex([NativeTypeName("struct meshopt_Meshlet *")] meshopt_Meshlet* meshlets, [NativeTypeName("unsigned int *")] uint* meshlet_vertices, [NativeTypeName("unsigned char *")] byte* meshlet_triangles, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride, [NativeTypeName("size_t")] nuint max_vertices, [NativeTypeName("size_t")] nuint min_triangles, [NativeTypeName("size_t")] nuint max_triangles, float cone_weight, float split_factor);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_buildMeshletsSpatial"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint meshopt_buildMeshletsSpatial([NativeTypeName("struct meshopt_Meshlet *")] meshopt_Meshlet* meshlets, [NativeTypeName("unsigned int *")] uint* meshlet_vertices, [NativeTypeName("unsigned char *")] byte* meshlet_triangles, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride, [NativeTypeName("size_t")] nuint max_vertices, [NativeTypeName("size_t")] nuint min_triangles, [NativeTypeName("size_t")] nuint max_triangles, float fill_weight);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_optimizeMeshlet"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void meshopt_optimizeMeshlet([NativeTypeName("unsigned int *")] uint* meshlet_vertices, [NativeTypeName("unsigned char *")] byte* meshlet_triangles, [NativeTypeName("size_t")] nuint triangle_count, [NativeTypeName("size_t")] nuint vertex_count);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_optimizeMeshletLevel"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void meshopt_optimizeMeshletLevel([NativeTypeName("unsigned int *")] uint* meshlet_vertices, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("unsigned char *")] byte* meshlet_triangles, [NativeTypeName("size_t")] nuint triangle_count, int level);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_computeClusterBounds"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("struct meshopt_Bounds")]
        public static extern meshopt_Bounds meshopt_computeClusterBounds([NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_computeMeshletBounds"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("struct meshopt_Bounds")]
        public static extern meshopt_Bounds meshopt_computeMeshletBounds([NativeTypeName("const unsigned int *")] uint* meshlet_vertices, [NativeTypeName("const unsigned char *")] byte* meshlet_triangles, [NativeTypeName("size_t")] nuint triangle_count, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_computeSphereBounds"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("struct meshopt_Bounds")]
        public static extern meshopt_Bounds meshopt_computeSphereBounds([NativeTypeName("const float *")] float* positions, [NativeTypeName("size_t")] nuint count, [NativeTypeName("size_t")] nuint positions_stride, [NativeTypeName("const float *")] float* radii, [NativeTypeName("size_t")] nuint radii_stride);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_extractMeshletIndices"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint meshopt_extractMeshletIndices([NativeTypeName("unsigned int *")] uint* vertices, [NativeTypeName("unsigned char *")] byte* triangles, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_partitionClusters"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint meshopt_partitionClusters([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* cluster_indices, [NativeTypeName("size_t")] nuint total_index_count, [NativeTypeName("const unsigned int *")] uint* cluster_index_counts, [NativeTypeName("size_t")] nuint cluster_count, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride, [NativeTypeName("size_t")] nuint target_partition_size);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_spatialSortRemap"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void meshopt_spatialSortRemap([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_spatialSortTriangles"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void meshopt_spatialSortTriangles([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_spatialClusterPoints"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void meshopt_spatialClusterPoints([NativeTypeName("unsigned int *")] uint* destination, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride, [NativeTypeName("size_t")] nuint cluster_size);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_opacityMapMeasure"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint meshopt_opacityMapMeasure([NativeTypeName("unsigned char *")] byte* levels, [NativeTypeName("unsigned int *")] uint* sources, int* omm_indices, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const float *")] float* vertex_uvs, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_uvs_stride, [NativeTypeName("unsigned int")] uint texture_width, [NativeTypeName("unsigned int")] uint texture_height, int max_level, float target_edge);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_opacityMapRasterize"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void meshopt_opacityMapRasterize([NativeTypeName("unsigned char *")] byte* result, int level, int states, [NativeTypeName("const float *")] float* uv0, [NativeTypeName("const float *")] float* uv1, [NativeTypeName("const float *")] float* uv2, [NativeTypeName("const unsigned char *")] byte* texture_data, [NativeTypeName("size_t")] nuint texture_stride, [NativeTypeName("size_t")] nuint texture_pitch, [NativeTypeName("unsigned int")] uint texture_width, [NativeTypeName("unsigned int")] uint texture_height);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_opacityMapEntrySize"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint meshopt_opacityMapEntrySize(int level, int states);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_opacityMapCompact"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint meshopt_opacityMapCompact([NativeTypeName("unsigned char *")] byte* data, [NativeTypeName("size_t")] nuint data_size, [NativeTypeName("unsigned char *")] byte* levels, [NativeTypeName("unsigned int *")] uint* offsets, [NativeTypeName("size_t")] nuint omm_count, int* omm_indices, [NativeTypeName("size_t")] nuint triangle_count, int states);

        public const int meshopt_TangentCompatible = 1 << 0;
        public const int meshopt_TangentZeroFallback = 1 << 1;

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_generateTangents"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void meshopt_generateTangents(float* result, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride, [NativeTypeName("const float *")] float* vertex_normals, [NativeTypeName("size_t")] nuint vertex_normals_stride, [NativeTypeName("const float *")] float* vertex_uvs, [NativeTypeName("size_t")] nuint vertex_uvs_stride, [NativeTypeName("unsigned int")] uint options);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_generateNormals"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void meshopt_generateNormals(float* result, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride, float crease_angle, float smoothing);

        public const int meshopt_RemeshThicken = 1 << 0;
        public const int meshopt_RemeshShell = 1 << 1;
        public const int meshopt_RemeshSolve = 1 << 2;

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_remesh"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint meshopt_remesh(float* destination, [NativeTypeName("size_t")] nuint max_triangle_count, [NativeTypeName("const unsigned int *")] uint* indices, [NativeTypeName("size_t")] nuint index_count, [NativeTypeName("const float *")] float* vertex_positions, [NativeTypeName("size_t")] nuint vertex_count, [NativeTypeName("size_t")] nuint vertex_positions_stride, int resolution, [NativeTypeName("unsigned int")] uint options);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_quantizeHalf"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned short")]
        public static extern ushort meshopt_quantizeHalf(float v);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_quantizeFloat"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float meshopt_quantizeFloat(float v, int N);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_dequantizeHalf"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float meshopt_dequantizeHalf([NativeTypeName("unsigned short")] ushort h);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_computePositionExponent"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int meshopt_computePositionExponent([NativeTypeName("const float *")] float* minv, [NativeTypeName("const float *")] float* maxv, int min_exp, int max_bits);

        /// <include file='Api.xml' path='doc/member[@name="Api.meshopt_setAllocator"]/*' />
        [DllImport("meshoptimizer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void meshopt_setAllocator([NativeTypeName("void *(*)(size_t) __attribute__((cdecl))")] delegate* unmanaged[Cdecl]<nuint, void*> allocate, [NativeTypeName("void (*)(void *) __attribute__((cdecl))")] delegate* unmanaged[Cdecl]<void*, void> deallocate);

        [NativeTypeName("#define MESHOPTIMIZER_VERSION 1020")]
        public const int MESHOPTIMIZER_VERSION = 1020;
    }
}
