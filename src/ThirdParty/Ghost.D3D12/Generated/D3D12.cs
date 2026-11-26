using System.Runtime.InteropServices;

namespace Ghost.D3D12;

public static unsafe partial class D3D12
{
    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D10Blob = new Guid(0x8ba5fb08, 0x5195, 0x40e2, 0xac, 0x58, 0xd, 0x98, 0x9c, 0x3a, 0x1, 0x2);

    [NativeTypeName("const GUID")]
    public static readonly Guid WKPDID_D3DDebugObjectName = new Guid(0x429b8c22, 0x9188, 0x4b0c, 0x87, 0x42, 0xac, 0xb0, 0xbf, 0x85, 0xc2, 0x00);

    [NativeTypeName("const GUID")]
    public static readonly Guid WKPDID_D3DDebugObjectNameW = new Guid(0x4cca5fd8, 0x921f, 0x42c8, 0x85, 0x66, 0x70, 0xca, 0xf2, 0xa9, 0xb7, 0x41);

    [NativeTypeName("const GUID")]
    public static readonly Guid WKPDID_CommentStringW = new Guid(0xd0149dc0, 0x90e8, 0x4ec8, 0x81, 0x44, 0xe9, 0x00, 0xad, 0x26, 0x6b, 0xb2);

    [NativeTypeName("const GUID")]
    public static readonly Guid WKPDID_D3D12UniqueObjectId = new Guid(0x1b39de15, 0xec04, 0x4bae, 0xba, 0x4d, 0x8c, 0xef, 0x79, 0xfc, 0x04, 0xc1);

    [NativeTypeName("const GUID")]
    public static readonly Guid D3D_TEXTURE_LAYOUT_ROW_MAJOR = new Guid(0xb5dc234f, 0x72bb, 0x4bec, 0x97, 0x05, 0x8c, 0xf2, 0x58, 0xdf, 0x6b, 0x6c);

    [NativeTypeName("const GUID")]
    public static readonly Guid D3D_TEXTURE_LAYOUT_64KB_STANDARD_SWIZZLE = new Guid(0x4c0f29e3, 0x3f5f, 0x4d35, 0x84, 0xc9, 0xbc, 0x09, 0x83, 0xb6, 0x2c, 0x28);

    [DllImport("d3d12", CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    public static extern HRESULT D3D12SerializeRootSignature([NativeTypeName("const D3D12_ROOT_SIGNATURE_DESC *")] D3D12_ROOT_SIGNATURE_DESC* pRootSignature, D3D_ROOT_SIGNATURE_VERSION Version, [NativeTypeName("ID3DBlob **")] ID3D10Blob** ppBlob, [NativeTypeName("ID3DBlob **")] ID3D10Blob** ppErrorBlob);

    [DllImport("d3d12", CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    public static extern HRESULT D3D12CreateRootSignatureDeserializer([NativeTypeName("LPCVOID")] void* pSrcData, [NativeTypeName("SIZE_T")] nuint SrcDataSizeInBytes, [NativeTypeName("const IID &")] Guid* pRootSignatureDeserializerInterface, void** ppRootSignatureDeserializer);

    [DllImport("d3d12", CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    public static extern HRESULT D3D12SerializeVersionedRootSignature([NativeTypeName("const D3D12_VERSIONED_ROOT_SIGNATURE_DESC *")] D3D12_VERSIONED_ROOT_SIGNATURE_DESC* pRootSignature, [NativeTypeName("ID3DBlob **")] ID3D10Blob** ppBlob, [NativeTypeName("ID3DBlob **")] ID3D10Blob** ppErrorBlob);

    [DllImport("d3d12", CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    public static extern HRESULT D3D12CreateVersionedRootSignatureDeserializer([NativeTypeName("LPCVOID")] void* pSrcData, [NativeTypeName("SIZE_T")] nuint SrcDataSizeInBytes, [NativeTypeName("const IID &")] Guid* pRootSignatureDeserializerInterface, void** ppRootSignatureDeserializer);

    [DllImport("d3d12", CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    public static extern HRESULT D3D12CreateVersionedRootSignatureDeserializerFromSubobjectInLibrary([NativeTypeName("LPCVOID")] void* pSrcData, [NativeTypeName("SIZE_T")] nuint SrcDataSizeInBytes, [NativeTypeName("LPCWSTR")] char* RootSignatureSubobjectName, [NativeTypeName("const IID &")] Guid* pRootSignatureDeserializerInterface, void** ppRootSignatureDeserializer);

    [NativeTypeName("const GUID")]
    public static readonly Guid D3D12_PROTECTED_RESOURCES_SESSION_HARDWARE_PROTECTED = new Guid(0x62B0084E, 0xC70E, 0x4DAA, 0xA1, 0x09, 0x30, 0xFF, 0x8D, 0x5A, 0x04, 0x82);

    [NativeTypeName("const GUID")]
    public static readonly Guid D3D12_GUID_TEXTURE_LAYOUT_NULL = new Guid(0x00000000, 0x0000, 0x0000, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);

    [NativeTypeName("const GUID")]
    public static readonly Guid D3D12_GUID_TEXTURE_LAYOUT_ROW_MAJOR_WITH_RENDER_AND_VPBLT_CAPABILITY = new Guid(0x451b3cc7, 0x91ce, 0x49fa, 0xba, 0x67, 0x0d, 0xca, 0x71, 0x9c, 0xcb, 0xb8);

    [NativeTypeName("#define __REQUIRED_RPCNDR_H_VERSION__ 500")]
    public const int __REQUIRED_RPCNDR_H_VERSION__ = 500;

    [NativeTypeName("#define __REQUIRED_RPCSAL_H_VERSION__ 100")]
    public const int __REQUIRED_RPCSAL_H_VERSION__ = 100;

    [NativeTypeName("#define D3D_FL9_1_REQ_TEXTURE1D_U_DIMENSION 2048")]
    public const int D3D_FL9_1_REQ_TEXTURE1D_U_DIMENSION = 2048;

    [NativeTypeName("#define D3D_FL9_3_REQ_TEXTURE1D_U_DIMENSION 4096")]
    public const int D3D_FL9_3_REQ_TEXTURE1D_U_DIMENSION = 4096;

    [NativeTypeName("#define D3D_FL9_1_REQ_TEXTURE2D_U_OR_V_DIMENSION 2048")]
    public const int D3D_FL9_1_REQ_TEXTURE2D_U_OR_V_DIMENSION = 2048;

    [NativeTypeName("#define D3D_FL9_3_REQ_TEXTURE2D_U_OR_V_DIMENSION 4096")]
    public const int D3D_FL9_3_REQ_TEXTURE2D_U_OR_V_DIMENSION = 4096;

    [NativeTypeName("#define D3D_FL9_1_REQ_TEXTURECUBE_DIMENSION 512")]
    public const int D3D_FL9_1_REQ_TEXTURECUBE_DIMENSION = 512;

    [NativeTypeName("#define D3D_FL9_3_REQ_TEXTURECUBE_DIMENSION 4096")]
    public const int D3D_FL9_3_REQ_TEXTURECUBE_DIMENSION = 4096;

    [NativeTypeName("#define D3D_FL9_1_REQ_TEXTURE3D_U_V_OR_W_DIMENSION 256")]
    public const int D3D_FL9_1_REQ_TEXTURE3D_U_V_OR_W_DIMENSION = 256;

    [NativeTypeName("#define D3D_FL9_1_DEFAULT_MAX_ANISOTROPY 2")]
    public const int D3D_FL9_1_DEFAULT_MAX_ANISOTROPY = 2;

    [NativeTypeName("#define D3D_FL9_1_IA_PRIMITIVE_MAX_COUNT 65535")]
    public const int D3D_FL9_1_IA_PRIMITIVE_MAX_COUNT = 65535;

    [NativeTypeName("#define D3D_FL9_2_IA_PRIMITIVE_MAX_COUNT 1048575")]
    public const int D3D_FL9_2_IA_PRIMITIVE_MAX_COUNT = 1048575;

    [NativeTypeName("#define D3D_FL9_1_SIMULTANEOUS_RENDER_TARGET_COUNT 1")]
    public const int D3D_FL9_1_SIMULTANEOUS_RENDER_TARGET_COUNT = 1;

    [NativeTypeName("#define D3D_FL9_3_SIMULTANEOUS_RENDER_TARGET_COUNT 4")]
    public const int D3D_FL9_3_SIMULTANEOUS_RENDER_TARGET_COUNT = 4;

    [NativeTypeName("#define D3D_FL9_1_MAX_TEXTURE_REPEAT 128")]
    public const int D3D_FL9_1_MAX_TEXTURE_REPEAT = 128;

    [NativeTypeName("#define D3D_FL9_2_MAX_TEXTURE_REPEAT 2048")]
    public const int D3D_FL9_2_MAX_TEXTURE_REPEAT = 2048;

    [NativeTypeName("#define D3D_FL9_3_MAX_TEXTURE_REPEAT 8192")]
    public const int D3D_FL9_3_MAX_TEXTURE_REPEAT = 8192;

    [NativeTypeName("#define D3D_SHADER_FEATURE_DOUBLES 0x00001")]
    public const int D3D_SHADER_FEATURE_DOUBLES = 0x00001;

    [NativeTypeName("#define D3D_SHADER_FEATURE_COMPUTE_SHADERS_PLUS_RAW_AND_STRUCTURED_BUFFERS_VIA_SHADER_4_X 0x00002")]
    public const int D3D_SHADER_FEATURE_COMPUTE_SHADERS_PLUS_RAW_AND_STRUCTURED_BUFFERS_VIA_SHADER_4_X = 0x00002;

    [NativeTypeName("#define D3D_SHADER_FEATURE_UAVS_AT_EVERY_STAGE 0x00004")]
    public const int D3D_SHADER_FEATURE_UAVS_AT_EVERY_STAGE = 0x00004;

    [NativeTypeName("#define D3D_SHADER_FEATURE_64_UAVS 0x00008")]
    public const int D3D_SHADER_FEATURE_64_UAVS = 0x00008;

    [NativeTypeName("#define D3D_SHADER_FEATURE_MINIMUM_PRECISION 0x00010")]
    public const int D3D_SHADER_FEATURE_MINIMUM_PRECISION = 0x00010;

    [NativeTypeName("#define D3D_SHADER_FEATURE_11_1_DOUBLE_EXTENSIONS 0x00020")]
    public const int D3D_SHADER_FEATURE_11_1_DOUBLE_EXTENSIONS = 0x00020;

    [NativeTypeName("#define D3D_SHADER_FEATURE_11_1_SHADER_EXTENSIONS 0x00040")]
    public const int D3D_SHADER_FEATURE_11_1_SHADER_EXTENSIONS = 0x00040;

    [NativeTypeName("#define D3D_SHADER_FEATURE_LEVEL_9_COMPARISON_FILTERING 0x00080")]
    public const int D3D_SHADER_FEATURE_LEVEL_9_COMPARISON_FILTERING = 0x00080;

    [NativeTypeName("#define D3D_SHADER_FEATURE_TILED_RESOURCES 0x00100")]
    public const int D3D_SHADER_FEATURE_TILED_RESOURCES = 0x00100;

    [NativeTypeName("#define D3D_SHADER_FEATURE_STENCIL_REF 0x00200")]
    public const int D3D_SHADER_FEATURE_STENCIL_REF = 0x00200;

    [NativeTypeName("#define D3D_SHADER_FEATURE_INNER_COVERAGE 0x00400")]
    public const int D3D_SHADER_FEATURE_INNER_COVERAGE = 0x00400;

    [NativeTypeName("#define D3D_SHADER_FEATURE_TYPED_UAV_LOAD_ADDITIONAL_FORMATS 0x00800")]
    public const int D3D_SHADER_FEATURE_TYPED_UAV_LOAD_ADDITIONAL_FORMATS = 0x00800;

    [NativeTypeName("#define D3D_SHADER_FEATURE_ROVS 0x01000")]
    public const int D3D_SHADER_FEATURE_ROVS = 0x01000;

    [NativeTypeName("#define D3D_SHADER_FEATURE_VIEWPORT_AND_RT_ARRAY_INDEX_FROM_ANY_SHADER_FEEDING_RASTERIZER 0x02000")]
    public const int D3D_SHADER_FEATURE_VIEWPORT_AND_RT_ARRAY_INDEX_FROM_ANY_SHADER_FEEDING_RASTERIZER = 0x02000;

    [NativeTypeName("#define D3D_SHADER_FEATURE_WAVE_OPS 0x04000")]
    public const int D3D_SHADER_FEATURE_WAVE_OPS = 0x04000;

    [NativeTypeName("#define D3D_SHADER_FEATURE_INT64_OPS 0x08000")]
    public const int D3D_SHADER_FEATURE_INT64_OPS = 0x08000;

    [NativeTypeName("#define D3D_SHADER_FEATURE_VIEW_ID 0x10000")]
    public const int D3D_SHADER_FEATURE_VIEW_ID = 0x10000;

    [NativeTypeName("#define D3D_SHADER_FEATURE_BARYCENTRICS 0x20000")]
    public const int D3D_SHADER_FEATURE_BARYCENTRICS = 0x20000;

    [NativeTypeName("#define D3D_SHADER_FEATURE_NATIVE_16BIT_OPS 0x40000")]
    public const int D3D_SHADER_FEATURE_NATIVE_16BIT_OPS = 0x40000;

    [NativeTypeName("#define D3D_SHADER_FEATURE_SHADING_RATE 0x80000")]
    public const int D3D_SHADER_FEATURE_SHADING_RATE = 0x80000;

    [NativeTypeName("#define D3D_SHADER_FEATURE_RAYTRACING_TIER_1_1 0x100000")]
    public const int D3D_SHADER_FEATURE_RAYTRACING_TIER_1_1 = 0x100000;

    [NativeTypeName("#define D3D_SHADER_FEATURE_SAMPLER_FEEDBACK 0x200000")]
    public const int D3D_SHADER_FEATURE_SAMPLER_FEEDBACK = 0x200000;

    [NativeTypeName("#define D3D_SHADER_FEATURE_ATOMIC_INT64_ON_TYPED_RESOURCE 0x400000")]
    public const int D3D_SHADER_FEATURE_ATOMIC_INT64_ON_TYPED_RESOURCE = 0x400000;

    [NativeTypeName("#define D3D_SHADER_FEATURE_ATOMIC_INT64_ON_GROUP_SHARED 0x800000")]
    public const int D3D_SHADER_FEATURE_ATOMIC_INT64_ON_GROUP_SHARED = 0x800000;

    [NativeTypeName("#define D3D_SHADER_FEATURE_DERIVATIVES_IN_MESH_AND_AMPLIFICATION_SHADERS 0x1000000")]
    public const int D3D_SHADER_FEATURE_DERIVATIVES_IN_MESH_AND_AMPLIFICATION_SHADERS = 0x1000000;

    [NativeTypeName("#define D3D_SHADER_FEATURE_RESOURCE_DESCRIPTOR_HEAP_INDEXING 0x2000000")]
    public const int D3D_SHADER_FEATURE_RESOURCE_DESCRIPTOR_HEAP_INDEXING = 0x2000000;

    [NativeTypeName("#define D3D_SHADER_FEATURE_SAMPLER_DESCRIPTOR_HEAP_INDEXING 0x4000000")]
    public const int D3D_SHADER_FEATURE_SAMPLER_DESCRIPTOR_HEAP_INDEXING = 0x4000000;

    [NativeTypeName("#define D3D_SHADER_FEATURE_WAVE_MMA 0x8000000")]
    public const int D3D_SHADER_FEATURE_WAVE_MMA = 0x8000000;

    [NativeTypeName("#define D3D_SHADER_FEATURE_ATOMIC_INT64_ON_DESCRIPTOR_HEAP_RESOURCE 0x10000000")]
    public const int D3D_SHADER_FEATURE_ATOMIC_INT64_ON_DESCRIPTOR_HEAP_RESOURCE = 0x10000000;

    [NativeTypeName("#define D3D_SHADER_FEATURE_ADVANCED_TEXTURE_OPS 0x20000000")]
    public const int D3D_SHADER_FEATURE_ADVANCED_TEXTURE_OPS = 0x20000000;

    [NativeTypeName("#define D3D_SHADER_FEATURE_WRITEABLE_MSAA_TEXTURES 0x40000000")]
    public const int D3D_SHADER_FEATURE_WRITEABLE_MSAA_TEXTURES = 0x40000000;

    [NativeTypeName("#define D3D_SHADER_FEATURE_SAMPLE_CMP_GRADIENT_OR_BIAS 0x80000000")]
    public const uint D3D_SHADER_FEATURE_SAMPLE_CMP_GRADIENT_OR_BIAS = 0x80000000;

    [NativeTypeName("#define D3D_SHADER_FEATURE_EXTENDED_COMMAND_INFO 0x100000000ull")]
    public const ulong D3D_SHADER_FEATURE_EXTENDED_COMMAND_INFO = 0x100000000UL;

    [NativeTypeName("#define D3D_OPT_SHADER_FEATURE_USES_DERIVATIVES 0x0000010000000000ull")]
    public const ulong D3D_OPT_SHADER_FEATURE_USES_DERIVATIVES = 0x0000010000000000UL;

    [NativeTypeName("#define D3D_OPT_SHADER_FEATURE_REQUIRES_GROUP 0x0000020000000000ull")]
    public const ulong D3D_OPT_SHADER_FEATURE_REQUIRES_GROUP = 0x0000020000000000UL;

    [NativeTypeName("#define D3D_COMPONENT_MASK_X 1")]
    public const int D3D_COMPONENT_MASK_X = 1;

    [NativeTypeName("#define D3D_COMPONENT_MASK_Y 2")]
    public const int D3D_COMPONENT_MASK_Y = 2;

    [NativeTypeName("#define D3D_COMPONENT_MASK_Z 4")]
    public const int D3D_COMPONENT_MASK_Z = 4;

    [NativeTypeName("#define D3D_COMPONENT_MASK_W 8")]
    public const int D3D_COMPONENT_MASK_W = 8;

    [NativeTypeName("#define D3D12_16BIT_INDEX_STRIP_CUT_VALUE ( 0xffff )")]
    public const int D3D12_16BIT_INDEX_STRIP_CUT_VALUE = (0xffff);

    [NativeTypeName("#define D3D12_32BIT_INDEX_STRIP_CUT_VALUE ( 0xffffffff )")]
    public const uint D3D12_32BIT_INDEX_STRIP_CUT_VALUE = (0xffffffff);

    [NativeTypeName("#define D3D12_8BIT_INDEX_STRIP_CUT_VALUE ( 0xff )")]
    public const int D3D12_8BIT_INDEX_STRIP_CUT_VALUE = (0xff);

    [NativeTypeName("#define D3D12_APPEND_ALIGNED_ELEMENT ( 0xffffffff )")]
    public const uint D3D12_APPEND_ALIGNED_ELEMENT = (0xffffffff);

    [NativeTypeName("#define D3D12_ARRAY_AXIS_ADDRESS_RANGE_BIT_COUNT ( 9 )")]
    public const int D3D12_ARRAY_AXIS_ADDRESS_RANGE_BIT_COUNT = (9);

    [NativeTypeName("#define D3D12_AS_TGSM_BYTES_MINIMUM_SUPPORT ( 32768 )")]
    public const int D3D12_AS_TGSM_BYTES_MINIMUM_SUPPORT = (32768);

    [NativeTypeName("#define D3D12_CLIP_OR_CULL_DISTANCE_COUNT ( 8 )")]
    public const int D3D12_CLIP_OR_CULL_DISTANCE_COUNT = (8);

    [NativeTypeName("#define D3D12_CLIP_OR_CULL_DISTANCE_ELEMENT_COUNT ( 2 )")]
    public const int D3D12_CLIP_OR_CULL_DISTANCE_ELEMENT_COUNT = (2);

    [NativeTypeName("#define D3D12_COMMONSHADER_CONSTANT_BUFFER_API_SLOT_COUNT ( 14 )")]
    public const int D3D12_COMMONSHADER_CONSTANT_BUFFER_API_SLOT_COUNT = (14);

    [NativeTypeName("#define D3D12_COMMONSHADER_CONSTANT_BUFFER_COMPONENTS ( 4 )")]
    public const int D3D12_COMMONSHADER_CONSTANT_BUFFER_COMPONENTS = (4);

    [NativeTypeName("#define D3D12_COMMONSHADER_CONSTANT_BUFFER_COMPONENT_BIT_COUNT ( 32 )")]
    public const int D3D12_COMMONSHADER_CONSTANT_BUFFER_COMPONENT_BIT_COUNT = (32);

    [NativeTypeName("#define D3D12_COMMONSHADER_CONSTANT_BUFFER_HW_SLOT_COUNT ( 15 )")]
    public const int D3D12_COMMONSHADER_CONSTANT_BUFFER_HW_SLOT_COUNT = (15);

    [NativeTypeName("#define D3D12_COMMONSHADER_CONSTANT_BUFFER_PARTIAL_UPDATE_EXTENTS_BYTE_ALIGNMENT ( 16 )")]
    public const int D3D12_COMMONSHADER_CONSTANT_BUFFER_PARTIAL_UPDATE_EXTENTS_BYTE_ALIGNMENT = (16);

    [NativeTypeName("#define D3D12_COMMONSHADER_CONSTANT_BUFFER_REGISTER_COMPONENTS ( 4 )")]
    public const int D3D12_COMMONSHADER_CONSTANT_BUFFER_REGISTER_COMPONENTS = (4);

    [NativeTypeName("#define D3D12_COMMONSHADER_CONSTANT_BUFFER_REGISTER_COUNT ( 15 )")]
    public const int D3D12_COMMONSHADER_CONSTANT_BUFFER_REGISTER_COUNT = (15);

    [NativeTypeName("#define D3D12_COMMONSHADER_CONSTANT_BUFFER_REGISTER_READS_PER_INST ( 1 )")]
    public const int D3D12_COMMONSHADER_CONSTANT_BUFFER_REGISTER_READS_PER_INST = (1);

    [NativeTypeName("#define D3D12_COMMONSHADER_CONSTANT_BUFFER_REGISTER_READ_PORTS ( 1 )")]
    public const int D3D12_COMMONSHADER_CONSTANT_BUFFER_REGISTER_READ_PORTS = (1);

    [NativeTypeName("#define D3D12_COMMONSHADER_FLOWCONTROL_NESTING_LIMIT ( 64 )")]
    public const int D3D12_COMMONSHADER_FLOWCONTROL_NESTING_LIMIT = (64);

    [NativeTypeName("#define D3D12_COMMONSHADER_IMMEDIATE_CONSTANT_BUFFER_REGISTER_COMPONENTS ( 4 )")]
    public const int D3D12_COMMONSHADER_IMMEDIATE_CONSTANT_BUFFER_REGISTER_COMPONENTS = (4);

    [NativeTypeName("#define D3D12_COMMONSHADER_IMMEDIATE_CONSTANT_BUFFER_REGISTER_COUNT ( 1 )")]
    public const int D3D12_COMMONSHADER_IMMEDIATE_CONSTANT_BUFFER_REGISTER_COUNT = (1);

    [NativeTypeName("#define D3D12_COMMONSHADER_IMMEDIATE_CONSTANT_BUFFER_REGISTER_READS_PER_INST ( 1 )")]
    public const int D3D12_COMMONSHADER_IMMEDIATE_CONSTANT_BUFFER_REGISTER_READS_PER_INST = (1);

    [NativeTypeName("#define D3D12_COMMONSHADER_IMMEDIATE_CONSTANT_BUFFER_REGISTER_READ_PORTS ( 1 )")]
    public const int D3D12_COMMONSHADER_IMMEDIATE_CONSTANT_BUFFER_REGISTER_READ_PORTS = (1);

    [NativeTypeName("#define D3D12_COMMONSHADER_IMMEDIATE_VALUE_COMPONENT_BIT_COUNT ( 32 )")]
    public const int D3D12_COMMONSHADER_IMMEDIATE_VALUE_COMPONENT_BIT_COUNT = (32);

    [NativeTypeName("#define D3D12_COMMONSHADER_INPUT_RESOURCE_REGISTER_COMPONENTS ( 1 )")]
    public const int D3D12_COMMONSHADER_INPUT_RESOURCE_REGISTER_COMPONENTS = (1);

    [NativeTypeName("#define D3D12_COMMONSHADER_INPUT_RESOURCE_REGISTER_COUNT ( 128 )")]
    public const int D3D12_COMMONSHADER_INPUT_RESOURCE_REGISTER_COUNT = (128);

    [NativeTypeName("#define D3D12_COMMONSHADER_INPUT_RESOURCE_REGISTER_READS_PER_INST ( 1 )")]
    public const int D3D12_COMMONSHADER_INPUT_RESOURCE_REGISTER_READS_PER_INST = (1);

    [NativeTypeName("#define D3D12_COMMONSHADER_INPUT_RESOURCE_REGISTER_READ_PORTS ( 1 )")]
    public const int D3D12_COMMONSHADER_INPUT_RESOURCE_REGISTER_READ_PORTS = (1);

    [NativeTypeName("#define D3D12_COMMONSHADER_INPUT_RESOURCE_SLOT_COUNT ( 128 )")]
    public const int D3D12_COMMONSHADER_INPUT_RESOURCE_SLOT_COUNT = (128);

    [NativeTypeName("#define D3D12_COMMONSHADER_SAMPLER_REGISTER_COMPONENTS ( 1 )")]
    public const int D3D12_COMMONSHADER_SAMPLER_REGISTER_COMPONENTS = (1);

    [NativeTypeName("#define D3D12_COMMONSHADER_SAMPLER_REGISTER_COUNT ( 16 )")]
    public const int D3D12_COMMONSHADER_SAMPLER_REGISTER_COUNT = (16);

    [NativeTypeName("#define D3D12_COMMONSHADER_SAMPLER_REGISTER_READS_PER_INST ( 1 )")]
    public const int D3D12_COMMONSHADER_SAMPLER_REGISTER_READS_PER_INST = (1);

    [NativeTypeName("#define D3D12_COMMONSHADER_SAMPLER_REGISTER_READ_PORTS ( 1 )")]
    public const int D3D12_COMMONSHADER_SAMPLER_REGISTER_READ_PORTS = (1);

    [NativeTypeName("#define D3D12_COMMONSHADER_SAMPLER_SLOT_COUNT ( 16 )")]
    public const int D3D12_COMMONSHADER_SAMPLER_SLOT_COUNT = (16);

    [NativeTypeName("#define D3D12_COMMONSHADER_SUBROUTINE_NESTING_LIMIT ( 32 )")]
    public const int D3D12_COMMONSHADER_SUBROUTINE_NESTING_LIMIT = (32);

    [NativeTypeName("#define D3D12_COMMONSHADER_TEMP_REGISTER_COMPONENTS ( 4 )")]
    public const int D3D12_COMMONSHADER_TEMP_REGISTER_COMPONENTS = (4);

    [NativeTypeName("#define D3D12_COMMONSHADER_TEMP_REGISTER_COMPONENT_BIT_COUNT ( 32 )")]
    public const int D3D12_COMMONSHADER_TEMP_REGISTER_COMPONENT_BIT_COUNT = (32);

    [NativeTypeName("#define D3D12_COMMONSHADER_TEMP_REGISTER_COUNT ( 4096 )")]
    public const int D3D12_COMMONSHADER_TEMP_REGISTER_COUNT = (4096);

    [NativeTypeName("#define D3D12_COMMONSHADER_TEMP_REGISTER_READS_PER_INST ( 3 )")]
    public const int D3D12_COMMONSHADER_TEMP_REGISTER_READS_PER_INST = (3);

    [NativeTypeName("#define D3D12_COMMONSHADER_TEMP_REGISTER_READ_PORTS ( 3 )")]
    public const int D3D12_COMMONSHADER_TEMP_REGISTER_READ_PORTS = (3);

    [NativeTypeName("#define D3D12_COMMONSHADER_TEXCOORD_RANGE_REDUCTION_MAX ( 10 )")]
    public const int D3D12_COMMONSHADER_TEXCOORD_RANGE_REDUCTION_MAX = (10);

    [NativeTypeName("#define D3D12_COMMONSHADER_TEXCOORD_RANGE_REDUCTION_MIN ( -10 )")]
    public const int D3D12_COMMONSHADER_TEXCOORD_RANGE_REDUCTION_MIN = (-10);

    [NativeTypeName("#define D3D12_COMMONSHADER_TEXEL_OFFSET_MAX_NEGATIVE ( -8 )")]
    public const int D3D12_COMMONSHADER_TEXEL_OFFSET_MAX_NEGATIVE = (-8);

    [NativeTypeName("#define D3D12_COMMONSHADER_TEXEL_OFFSET_MAX_POSITIVE ( 7 )")]
    public const int D3D12_COMMONSHADER_TEXEL_OFFSET_MAX_POSITIVE = (7);

    [NativeTypeName("#define D3D12_CONSTANT_BUFFER_DATA_PLACEMENT_ALIGNMENT ( 256 )")]
    public const int D3D12_CONSTANT_BUFFER_DATA_PLACEMENT_ALIGNMENT = (256);

    [NativeTypeName("#define D3D12_CS_4_X_BUCKET00_MAX_BYTES_TGSM_WRITABLE_PER_THREAD ( 256 )")]
    public const int D3D12_CS_4_X_BUCKET00_MAX_BYTES_TGSM_WRITABLE_PER_THREAD = (256);

    [NativeTypeName("#define D3D12_CS_4_X_BUCKET00_MAX_NUM_THREADS_PER_GROUP ( 64 )")]
    public const int D3D12_CS_4_X_BUCKET00_MAX_NUM_THREADS_PER_GROUP = (64);

    [NativeTypeName("#define D3D12_CS_4_X_BUCKET01_MAX_BYTES_TGSM_WRITABLE_PER_THREAD ( 240 )")]
    public const int D3D12_CS_4_X_BUCKET01_MAX_BYTES_TGSM_WRITABLE_PER_THREAD = (240);

    [NativeTypeName("#define D3D12_CS_4_X_BUCKET01_MAX_NUM_THREADS_PER_GROUP ( 68 )")]
    public const int D3D12_CS_4_X_BUCKET01_MAX_NUM_THREADS_PER_GROUP = (68);

    [NativeTypeName("#define D3D12_CS_4_X_BUCKET02_MAX_BYTES_TGSM_WRITABLE_PER_THREAD ( 224 )")]
    public const int D3D12_CS_4_X_BUCKET02_MAX_BYTES_TGSM_WRITABLE_PER_THREAD = (224);

    [NativeTypeName("#define D3D12_CS_4_X_BUCKET02_MAX_NUM_THREADS_PER_GROUP ( 72 )")]
    public const int D3D12_CS_4_X_BUCKET02_MAX_NUM_THREADS_PER_GROUP = (72);

    [NativeTypeName("#define D3D12_CS_4_X_BUCKET03_MAX_BYTES_TGSM_WRITABLE_PER_THREAD ( 208 )")]
    public const int D3D12_CS_4_X_BUCKET03_MAX_BYTES_TGSM_WRITABLE_PER_THREAD = (208);

    [NativeTypeName("#define D3D12_CS_4_X_BUCKET03_MAX_NUM_THREADS_PER_GROUP ( 76 )")]
    public const int D3D12_CS_4_X_BUCKET03_MAX_NUM_THREADS_PER_GROUP = (76);

    [NativeTypeName("#define D3D12_CS_4_X_BUCKET04_MAX_BYTES_TGSM_WRITABLE_PER_THREAD ( 192 )")]
    public const int D3D12_CS_4_X_BUCKET04_MAX_BYTES_TGSM_WRITABLE_PER_THREAD = (192);

    [NativeTypeName("#define D3D12_CS_4_X_BUCKET04_MAX_NUM_THREADS_PER_GROUP ( 84 )")]
    public const int D3D12_CS_4_X_BUCKET04_MAX_NUM_THREADS_PER_GROUP = (84);

    [NativeTypeName("#define D3D12_CS_4_X_BUCKET05_MAX_BYTES_TGSM_WRITABLE_PER_THREAD ( 176 )")]
    public const int D3D12_CS_4_X_BUCKET05_MAX_BYTES_TGSM_WRITABLE_PER_THREAD = (176);

    [NativeTypeName("#define D3D12_CS_4_X_BUCKET05_MAX_NUM_THREADS_PER_GROUP ( 92 )")]
    public const int D3D12_CS_4_X_BUCKET05_MAX_NUM_THREADS_PER_GROUP = (92);

    [NativeTypeName("#define D3D12_CS_4_X_BUCKET06_MAX_BYTES_TGSM_WRITABLE_PER_THREAD ( 160 )")]
    public const int D3D12_CS_4_X_BUCKET06_MAX_BYTES_TGSM_WRITABLE_PER_THREAD = (160);

    [NativeTypeName("#define D3D12_CS_4_X_BUCKET06_MAX_NUM_THREADS_PER_GROUP ( 100 )")]
    public const int D3D12_CS_4_X_BUCKET06_MAX_NUM_THREADS_PER_GROUP = (100);

    [NativeTypeName("#define D3D12_CS_4_X_BUCKET07_MAX_BYTES_TGSM_WRITABLE_PER_THREAD ( 144 )")]
    public const int D3D12_CS_4_X_BUCKET07_MAX_BYTES_TGSM_WRITABLE_PER_THREAD = (144);

    [NativeTypeName("#define D3D12_CS_4_X_BUCKET07_MAX_NUM_THREADS_PER_GROUP ( 112 )")]
    public const int D3D12_CS_4_X_BUCKET07_MAX_NUM_THREADS_PER_GROUP = (112);

    [NativeTypeName("#define D3D12_CS_4_X_BUCKET08_MAX_BYTES_TGSM_WRITABLE_PER_THREAD ( 128 )")]
    public const int D3D12_CS_4_X_BUCKET08_MAX_BYTES_TGSM_WRITABLE_PER_THREAD = (128);

    [NativeTypeName("#define D3D12_CS_4_X_BUCKET08_MAX_NUM_THREADS_PER_GROUP ( 128 )")]
    public const int D3D12_CS_4_X_BUCKET08_MAX_NUM_THREADS_PER_GROUP = (128);

    [NativeTypeName("#define D3D12_CS_4_X_BUCKET09_MAX_BYTES_TGSM_WRITABLE_PER_THREAD ( 112 )")]
    public const int D3D12_CS_4_X_BUCKET09_MAX_BYTES_TGSM_WRITABLE_PER_THREAD = (112);

    [NativeTypeName("#define D3D12_CS_4_X_BUCKET09_MAX_NUM_THREADS_PER_GROUP ( 144 )")]
    public const int D3D12_CS_4_X_BUCKET09_MAX_NUM_THREADS_PER_GROUP = (144);

    [NativeTypeName("#define D3D12_CS_4_X_BUCKET10_MAX_BYTES_TGSM_WRITABLE_PER_THREAD ( 96 )")]
    public const int D3D12_CS_4_X_BUCKET10_MAX_BYTES_TGSM_WRITABLE_PER_THREAD = (96);

    [NativeTypeName("#define D3D12_CS_4_X_BUCKET10_MAX_NUM_THREADS_PER_GROUP ( 168 )")]
    public const int D3D12_CS_4_X_BUCKET10_MAX_NUM_THREADS_PER_GROUP = (168);

    [NativeTypeName("#define D3D12_CS_4_X_BUCKET11_MAX_BYTES_TGSM_WRITABLE_PER_THREAD ( 80 )")]
    public const int D3D12_CS_4_X_BUCKET11_MAX_BYTES_TGSM_WRITABLE_PER_THREAD = (80);

    [NativeTypeName("#define D3D12_CS_4_X_BUCKET11_MAX_NUM_THREADS_PER_GROUP ( 204 )")]
    public const int D3D12_CS_4_X_BUCKET11_MAX_NUM_THREADS_PER_GROUP = (204);

    [NativeTypeName("#define D3D12_CS_4_X_BUCKET12_MAX_BYTES_TGSM_WRITABLE_PER_THREAD ( 64 )")]
    public const int D3D12_CS_4_X_BUCKET12_MAX_BYTES_TGSM_WRITABLE_PER_THREAD = (64);

    [NativeTypeName("#define D3D12_CS_4_X_BUCKET12_MAX_NUM_THREADS_PER_GROUP ( 256 )")]
    public const int D3D12_CS_4_X_BUCKET12_MAX_NUM_THREADS_PER_GROUP = (256);

    [NativeTypeName("#define D3D12_CS_4_X_BUCKET13_MAX_BYTES_TGSM_WRITABLE_PER_THREAD ( 48 )")]
    public const int D3D12_CS_4_X_BUCKET13_MAX_BYTES_TGSM_WRITABLE_PER_THREAD = (48);

    [NativeTypeName("#define D3D12_CS_4_X_BUCKET13_MAX_NUM_THREADS_PER_GROUP ( 340 )")]
    public const int D3D12_CS_4_X_BUCKET13_MAX_NUM_THREADS_PER_GROUP = (340);

    [NativeTypeName("#define D3D12_CS_4_X_BUCKET14_MAX_BYTES_TGSM_WRITABLE_PER_THREAD ( 32 )")]
    public const int D3D12_CS_4_X_BUCKET14_MAX_BYTES_TGSM_WRITABLE_PER_THREAD = (32);

    [NativeTypeName("#define D3D12_CS_4_X_BUCKET14_MAX_NUM_THREADS_PER_GROUP ( 512 )")]
    public const int D3D12_CS_4_X_BUCKET14_MAX_NUM_THREADS_PER_GROUP = (512);

    [NativeTypeName("#define D3D12_CS_4_X_BUCKET15_MAX_BYTES_TGSM_WRITABLE_PER_THREAD ( 16 )")]
    public const int D3D12_CS_4_X_BUCKET15_MAX_BYTES_TGSM_WRITABLE_PER_THREAD = (16);

    [NativeTypeName("#define D3D12_CS_4_X_BUCKET15_MAX_NUM_THREADS_PER_GROUP ( 768 )")]
    public const int D3D12_CS_4_X_BUCKET15_MAX_NUM_THREADS_PER_GROUP = (768);

    [NativeTypeName("#define D3D12_CS_4_X_DISPATCH_MAX_THREAD_GROUPS_IN_Z_DIMENSION ( 1 )")]
    public const int D3D12_CS_4_X_DISPATCH_MAX_THREAD_GROUPS_IN_Z_DIMENSION = (1);

    [NativeTypeName("#define D3D12_CS_4_X_RAW_UAV_BYTE_ALIGNMENT ( 256 )")]
    public const int D3D12_CS_4_X_RAW_UAV_BYTE_ALIGNMENT = (256);

    [NativeTypeName("#define D3D12_CS_4_X_THREAD_GROUP_MAX_THREADS_PER_GROUP ( 768 )")]
    public const int D3D12_CS_4_X_THREAD_GROUP_MAX_THREADS_PER_GROUP = (768);

    [NativeTypeName("#define D3D12_CS_4_X_THREAD_GROUP_MAX_X ( 768 )")]
    public const int D3D12_CS_4_X_THREAD_GROUP_MAX_X = (768);

    [NativeTypeName("#define D3D12_CS_4_X_THREAD_GROUP_MAX_Y ( 768 )")]
    public const int D3D12_CS_4_X_THREAD_GROUP_MAX_Y = (768);

    [NativeTypeName("#define D3D12_CS_4_X_UAV_REGISTER_COUNT ( 1 )")]
    public const int D3D12_CS_4_X_UAV_REGISTER_COUNT = (1);

    [NativeTypeName("#define D3D12_CS_DISPATCH_MAX_THREAD_GROUPS_PER_DIMENSION ( 65535 )")]
    public const int D3D12_CS_DISPATCH_MAX_THREAD_GROUPS_PER_DIMENSION = (65535);

    [NativeTypeName("#define D3D12_CS_TGSM_BYTES_MINIMUM_SUPPORT ( 32768 )")]
    public const int D3D12_CS_TGSM_BYTES_MINIMUM_SUPPORT = (32768);

    [NativeTypeName("#define D3D12_CS_TGSM_REGISTER_COUNT ( 8192 )")]
    public const int D3D12_CS_TGSM_REGISTER_COUNT = (8192);

    [NativeTypeName("#define D3D12_CS_TGSM_REGISTER_READS_PER_INST ( 1 )")]
    public const int D3D12_CS_TGSM_REGISTER_READS_PER_INST = (1);

    [NativeTypeName("#define D3D12_CS_TGSM_RESOURCE_REGISTER_COMPONENTS ( 1 )")]
    public const int D3D12_CS_TGSM_RESOURCE_REGISTER_COMPONENTS = (1);

    [NativeTypeName("#define D3D12_CS_TGSM_RESOURCE_REGISTER_READ_PORTS ( 1 )")]
    public const int D3D12_CS_TGSM_RESOURCE_REGISTER_READ_PORTS = (1);

    [NativeTypeName("#define D3D12_CS_THREADGROUPID_REGISTER_COMPONENTS ( 3 )")]
    public const int D3D12_CS_THREADGROUPID_REGISTER_COMPONENTS = (3);

    [NativeTypeName("#define D3D12_CS_THREADGROUPID_REGISTER_COUNT ( 1 )")]
    public const int D3D12_CS_THREADGROUPID_REGISTER_COUNT = (1);

    [NativeTypeName("#define D3D12_CS_THREADIDINGROUPFLATTENED_REGISTER_COMPONENTS ( 1 )")]
    public const int D3D12_CS_THREADIDINGROUPFLATTENED_REGISTER_COMPONENTS = (1);

    [NativeTypeName("#define D3D12_CS_THREADIDINGROUPFLATTENED_REGISTER_COUNT ( 1 )")]
    public const int D3D12_CS_THREADIDINGROUPFLATTENED_REGISTER_COUNT = (1);

    [NativeTypeName("#define D3D12_CS_THREADIDINGROUP_REGISTER_COMPONENTS ( 3 )")]
    public const int D3D12_CS_THREADIDINGROUP_REGISTER_COMPONENTS = (3);

    [NativeTypeName("#define D3D12_CS_THREADIDINGROUP_REGISTER_COUNT ( 1 )")]
    public const int D3D12_CS_THREADIDINGROUP_REGISTER_COUNT = (1);

    [NativeTypeName("#define D3D12_CS_THREADID_REGISTER_COMPONENTS ( 3 )")]
    public const int D3D12_CS_THREADID_REGISTER_COMPONENTS = (3);

    [NativeTypeName("#define D3D12_CS_THREADID_REGISTER_COUNT ( 1 )")]
    public const int D3D12_CS_THREADID_REGISTER_COUNT = (1);

    [NativeTypeName("#define D3D12_CS_THREAD_GROUP_MAX_THREADS_PER_GROUP ( 1024 )")]
    public const int D3D12_CS_THREAD_GROUP_MAX_THREADS_PER_GROUP = (1024);

    [NativeTypeName("#define D3D12_CS_THREAD_GROUP_MAX_X ( 1024 )")]
    public const int D3D12_CS_THREAD_GROUP_MAX_X = (1024);

    [NativeTypeName("#define D3D12_CS_THREAD_GROUP_MAX_Y ( 1024 )")]
    public const int D3D12_CS_THREAD_GROUP_MAX_Y = (1024);

    [NativeTypeName("#define D3D12_CS_THREAD_GROUP_MAX_Z ( 64 )")]
    public const int D3D12_CS_THREAD_GROUP_MAX_Z = (64);

    [NativeTypeName("#define D3D12_CS_THREAD_GROUP_MIN_X ( 1 )")]
    public const int D3D12_CS_THREAD_GROUP_MIN_X = (1);

    [NativeTypeName("#define D3D12_CS_THREAD_GROUP_MIN_Y ( 1 )")]
    public const int D3D12_CS_THREAD_GROUP_MIN_Y = (1);

    [NativeTypeName("#define D3D12_CS_THREAD_GROUP_MIN_Z ( 1 )")]
    public const int D3D12_CS_THREAD_GROUP_MIN_Z = (1);

    [NativeTypeName("#define D3D12_CS_THREAD_LOCAL_TEMP_REGISTER_POOL ( 16384 )")]
    public const int D3D12_CS_THREAD_LOCAL_TEMP_REGISTER_POOL = (16384);

    [NativeTypeName("#define D3D12_DEFAULT_BLEND_FACTOR_ALPHA ( 1.0f )")]
    public const float D3D12_DEFAULT_BLEND_FACTOR_ALPHA = (1.0f);

    [NativeTypeName("#define D3D12_DEFAULT_BLEND_FACTOR_BLUE ( 1.0f )")]
    public const float D3D12_DEFAULT_BLEND_FACTOR_BLUE = (1.0f);

    [NativeTypeName("#define D3D12_DEFAULT_BLEND_FACTOR_GREEN ( 1.0f )")]
    public const float D3D12_DEFAULT_BLEND_FACTOR_GREEN = (1.0f);

    [NativeTypeName("#define D3D12_DEFAULT_BLEND_FACTOR_RED ( 1.0f )")]
    public const float D3D12_DEFAULT_BLEND_FACTOR_RED = (1.0f);

    [NativeTypeName("#define D3D12_DEFAULT_BORDER_COLOR_COMPONENT ( 0.0f )")]
    public const float D3D12_DEFAULT_BORDER_COLOR_COMPONENT = (0.0f);

    [NativeTypeName("#define D3D12_DEFAULT_DEPTH_BIAS ( 0 )")]
    public const int D3D12_DEFAULT_DEPTH_BIAS = (0);

    [NativeTypeName("#define D3D12_DEFAULT_DEPTH_BIAS_CLAMP ( 0.0f )")]
    public const float D3D12_DEFAULT_DEPTH_BIAS_CLAMP = (0.0f);

    [NativeTypeName("#define D3D12_DEFAULT_MAX_ANISOTROPY ( 16 )")]
    public const int D3D12_DEFAULT_MAX_ANISOTROPY = (16);

    [NativeTypeName("#define D3D12_DEFAULT_MIP_LOD_BIAS ( 0.0f )")]
    public const float D3D12_DEFAULT_MIP_LOD_BIAS = (0.0f);

    [NativeTypeName("#define D3D12_DEFAULT_MSAA_RESOURCE_PLACEMENT_ALIGNMENT ( 4194304 )")]
    public const int D3D12_DEFAULT_MSAA_RESOURCE_PLACEMENT_ALIGNMENT = (4194304);

    [NativeTypeName("#define D3D12_DEFAULT_RENDER_TARGET_ARRAY_INDEX ( 0 )")]
    public const int D3D12_DEFAULT_RENDER_TARGET_ARRAY_INDEX = (0);

    [NativeTypeName("#define D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT ( 65536 )")]
    public const int D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT = (65536);

    [NativeTypeName("#define D3D12_DEFAULT_SAMPLE_MASK ( 0xffffffff )")]
    public const uint D3D12_DEFAULT_SAMPLE_MASK = (0xffffffff);

    [NativeTypeName("#define D3D12_DEFAULT_SCISSOR_ENDX ( 0 )")]
    public const int D3D12_DEFAULT_SCISSOR_ENDX = (0);

    [NativeTypeName("#define D3D12_DEFAULT_SCISSOR_ENDY ( 0 )")]
    public const int D3D12_DEFAULT_SCISSOR_ENDY = (0);

    [NativeTypeName("#define D3D12_DEFAULT_SCISSOR_STARTX ( 0 )")]
    public const int D3D12_DEFAULT_SCISSOR_STARTX = (0);

    [NativeTypeName("#define D3D12_DEFAULT_SCISSOR_STARTY ( 0 )")]
    public const int D3D12_DEFAULT_SCISSOR_STARTY = (0);

    [NativeTypeName("#define D3D12_DEFAULT_SLOPE_SCALED_DEPTH_BIAS ( 0.0f )")]
    public const float D3D12_DEFAULT_SLOPE_SCALED_DEPTH_BIAS = (0.0f);

    [NativeTypeName("#define D3D12_DEFAULT_STENCIL_READ_MASK ( 0xff )")]
    public const int D3D12_DEFAULT_STENCIL_READ_MASK = (0xff);

    [NativeTypeName("#define D3D12_DEFAULT_STENCIL_REFERENCE ( 0 )")]
    public const int D3D12_DEFAULT_STENCIL_REFERENCE = (0);

    [NativeTypeName("#define D3D12_DEFAULT_STENCIL_WRITE_MASK ( 0xff )")]
    public const int D3D12_DEFAULT_STENCIL_WRITE_MASK = (0xff);

    [NativeTypeName("#define D3D12_DEFAULT_VIEWPORT_AND_SCISSORRECT_INDEX ( 0 )")]
    public const int D3D12_DEFAULT_VIEWPORT_AND_SCISSORRECT_INDEX = (0);

    [NativeTypeName("#define D3D12_DEFAULT_VIEWPORT_HEIGHT ( 0 )")]
    public const int D3D12_DEFAULT_VIEWPORT_HEIGHT = (0);

    [NativeTypeName("#define D3D12_DEFAULT_VIEWPORT_MAX_DEPTH ( 0.0f )")]
    public const float D3D12_DEFAULT_VIEWPORT_MAX_DEPTH = (0.0f);

    [NativeTypeName("#define D3D12_DEFAULT_VIEWPORT_MIN_DEPTH ( 0.0f )")]
    public const float D3D12_DEFAULT_VIEWPORT_MIN_DEPTH = (0.0f);

    [NativeTypeName("#define D3D12_DEFAULT_VIEWPORT_TOPLEFTX ( 0 )")]
    public const int D3D12_DEFAULT_VIEWPORT_TOPLEFTX = (0);

    [NativeTypeName("#define D3D12_DEFAULT_VIEWPORT_TOPLEFTY ( 0 )")]
    public const int D3D12_DEFAULT_VIEWPORT_TOPLEFTY = (0);

    [NativeTypeName("#define D3D12_DEFAULT_VIEWPORT_WIDTH ( 0 )")]
    public const int D3D12_DEFAULT_VIEWPORT_WIDTH = (0);

    [NativeTypeName("#define D3D12_DESCRIPTOR_RANGE_OFFSET_APPEND ( 0xffffffff )")]
    public const uint D3D12_DESCRIPTOR_RANGE_OFFSET_APPEND = (0xffffffff);

    [NativeTypeName("#define D3D12_DRIVER_RESERVED_REGISTER_SPACE_VALUES_END ( 0xfffffff7 )")]
    public const uint D3D12_DRIVER_RESERVED_REGISTER_SPACE_VALUES_END = (0xfffffff7);

    [NativeTypeName("#define D3D12_DRIVER_RESERVED_REGISTER_SPACE_VALUES_START ( 0xfffffff0 )")]
    public const uint D3D12_DRIVER_RESERVED_REGISTER_SPACE_VALUES_START = (0xfffffff0);

    [NativeTypeName("#define D3D12_DS_INPUT_CONTROL_POINTS_MAX_TOTAL_SCALARS ( 3968 )")]
    public const int D3D12_DS_INPUT_CONTROL_POINTS_MAX_TOTAL_SCALARS = (3968);

    [NativeTypeName("#define D3D12_DS_INPUT_CONTROL_POINT_REGISTER_COMPONENTS ( 4 )")]
    public const int D3D12_DS_INPUT_CONTROL_POINT_REGISTER_COMPONENTS = (4);

    [NativeTypeName("#define D3D12_DS_INPUT_CONTROL_POINT_REGISTER_COMPONENT_BIT_COUNT ( 32 )")]
    public const int D3D12_DS_INPUT_CONTROL_POINT_REGISTER_COMPONENT_BIT_COUNT = (32);

    [NativeTypeName("#define D3D12_DS_INPUT_CONTROL_POINT_REGISTER_COUNT ( 32 )")]
    public const int D3D12_DS_INPUT_CONTROL_POINT_REGISTER_COUNT = (32);

    [NativeTypeName("#define D3D12_DS_INPUT_CONTROL_POINT_REGISTER_READS_PER_INST ( 2 )")]
    public const int D3D12_DS_INPUT_CONTROL_POINT_REGISTER_READS_PER_INST = (2);

    [NativeTypeName("#define D3D12_DS_INPUT_CONTROL_POINT_REGISTER_READ_PORTS ( 1 )")]
    public const int D3D12_DS_INPUT_CONTROL_POINT_REGISTER_READ_PORTS = (1);

    [NativeTypeName("#define D3D12_DS_INPUT_DOMAIN_POINT_REGISTER_COMPONENTS ( 3 )")]
    public const int D3D12_DS_INPUT_DOMAIN_POINT_REGISTER_COMPONENTS = (3);

    [NativeTypeName("#define D3D12_DS_INPUT_DOMAIN_POINT_REGISTER_COMPONENT_BIT_COUNT ( 32 )")]
    public const int D3D12_DS_INPUT_DOMAIN_POINT_REGISTER_COMPONENT_BIT_COUNT = (32);

    [NativeTypeName("#define D3D12_DS_INPUT_DOMAIN_POINT_REGISTER_COUNT ( 1 )")]
    public const int D3D12_DS_INPUT_DOMAIN_POINT_REGISTER_COUNT = (1);

    [NativeTypeName("#define D3D12_DS_INPUT_DOMAIN_POINT_REGISTER_READS_PER_INST ( 2 )")]
    public const int D3D12_DS_INPUT_DOMAIN_POINT_REGISTER_READS_PER_INST = (2);

    [NativeTypeName("#define D3D12_DS_INPUT_DOMAIN_POINT_REGISTER_READ_PORTS ( 1 )")]
    public const int D3D12_DS_INPUT_DOMAIN_POINT_REGISTER_READ_PORTS = (1);

    [NativeTypeName("#define D3D12_DS_INPUT_PATCH_CONSTANT_REGISTER_COMPONENTS ( 4 )")]
    public const int D3D12_DS_INPUT_PATCH_CONSTANT_REGISTER_COMPONENTS = (4);

    [NativeTypeName("#define D3D12_DS_INPUT_PATCH_CONSTANT_REGISTER_COMPONENT_BIT_COUNT ( 32 )")]
    public const int D3D12_DS_INPUT_PATCH_CONSTANT_REGISTER_COMPONENT_BIT_COUNT = (32);

    [NativeTypeName("#define D3D12_DS_INPUT_PATCH_CONSTANT_REGISTER_COUNT ( 32 )")]
    public const int D3D12_DS_INPUT_PATCH_CONSTANT_REGISTER_COUNT = (32);

    [NativeTypeName("#define D3D12_DS_INPUT_PATCH_CONSTANT_REGISTER_READS_PER_INST ( 2 )")]
    public const int D3D12_DS_INPUT_PATCH_CONSTANT_REGISTER_READS_PER_INST = (2);

    [NativeTypeName("#define D3D12_DS_INPUT_PATCH_CONSTANT_REGISTER_READ_PORTS ( 1 )")]
    public const int D3D12_DS_INPUT_PATCH_CONSTANT_REGISTER_READ_PORTS = (1);

    [NativeTypeName("#define D3D12_DS_INPUT_PRIMITIVE_ID_REGISTER_COMPONENTS ( 1 )")]
    public const int D3D12_DS_INPUT_PRIMITIVE_ID_REGISTER_COMPONENTS = (1);

    [NativeTypeName("#define D3D12_DS_INPUT_PRIMITIVE_ID_REGISTER_COMPONENT_BIT_COUNT ( 32 )")]
    public const int D3D12_DS_INPUT_PRIMITIVE_ID_REGISTER_COMPONENT_BIT_COUNT = (32);

    [NativeTypeName("#define D3D12_DS_INPUT_PRIMITIVE_ID_REGISTER_COUNT ( 1 )")]
    public const int D3D12_DS_INPUT_PRIMITIVE_ID_REGISTER_COUNT = (1);

    [NativeTypeName("#define D3D12_DS_INPUT_PRIMITIVE_ID_REGISTER_READS_PER_INST ( 2 )")]
    public const int D3D12_DS_INPUT_PRIMITIVE_ID_REGISTER_READS_PER_INST = (2);

    [NativeTypeName("#define D3D12_DS_INPUT_PRIMITIVE_ID_REGISTER_READ_PORTS ( 1 )")]
    public const int D3D12_DS_INPUT_PRIMITIVE_ID_REGISTER_READ_PORTS = (1);

    [NativeTypeName("#define D3D12_DS_OUTPUT_REGISTER_COMPONENTS ( 4 )")]
    public const int D3D12_DS_OUTPUT_REGISTER_COMPONENTS = (4);

    [NativeTypeName("#define D3D12_DS_OUTPUT_REGISTER_COMPONENT_BIT_COUNT ( 32 )")]
    public const int D3D12_DS_OUTPUT_REGISTER_COMPONENT_BIT_COUNT = (32);

    [NativeTypeName("#define D3D12_DS_OUTPUT_REGISTER_COUNT ( 32 )")]
    public const int D3D12_DS_OUTPUT_REGISTER_COUNT = (32);

    [NativeTypeName("#define D3D12_DUMP_FILE_DRIVER_OPTIONS_DEFAULT ( 1 )")]
    public const int D3D12_DUMP_FILE_DRIVER_OPTIONS_DEFAULT = (1);

    [NativeTypeName("#define D3D12_FLOAT16_FUSED_TOLERANCE_IN_ULP ( 0.6 )")]
    public const double D3D12_FLOAT16_FUSED_TOLERANCE_IN_ULP = (0.6);

    [NativeTypeName("#define D3D12_FLOAT32_MAX ( 3.402823466e+38f )")]
    public const float D3D12_FLOAT32_MAX = (3.402823466e+38f);

    [NativeTypeName("#define D3D12_FLOAT32_TO_INTEGER_TOLERANCE_IN_ULP ( 0.6f )")]
    public const float D3D12_FLOAT32_TO_INTEGER_TOLERANCE_IN_ULP = (0.6f);

    [NativeTypeName("#define D3D12_FLOAT_TO_SRGB_EXPONENT_DENOMINATOR ( 2.4f )")]
    public const float D3D12_FLOAT_TO_SRGB_EXPONENT_DENOMINATOR = (2.4f);

    [NativeTypeName("#define D3D12_FLOAT_TO_SRGB_EXPONENT_NUMERATOR ( 1.0f )")]
    public const float D3D12_FLOAT_TO_SRGB_EXPONENT_NUMERATOR = (1.0f);

    [NativeTypeName("#define D3D12_FLOAT_TO_SRGB_OFFSET ( 0.055f )")]
    public const float D3D12_FLOAT_TO_SRGB_OFFSET = (0.055f);

    [NativeTypeName("#define D3D12_FLOAT_TO_SRGB_SCALE_1 ( 12.92f )")]
    public const float D3D12_FLOAT_TO_SRGB_SCALE_1 = (12.92f);

    [NativeTypeName("#define D3D12_FLOAT_TO_SRGB_SCALE_2 ( 1.055f )")]
    public const float D3D12_FLOAT_TO_SRGB_SCALE_2 = (1.055f);

    [NativeTypeName("#define D3D12_FLOAT_TO_SRGB_THRESHOLD ( 0.0031308f )")]
    public const float D3D12_FLOAT_TO_SRGB_THRESHOLD = (0.0031308f);

    [NativeTypeName("#define D3D12_FTOI_INSTRUCTION_MAX_INPUT ( 2147483647.999f )")]
    public const float D3D12_FTOI_INSTRUCTION_MAX_INPUT = (2147483647.999f);

    [NativeTypeName("#define D3D12_FTOI_INSTRUCTION_MIN_INPUT ( -2147483648.999f )")]
    public const float D3D12_FTOI_INSTRUCTION_MIN_INPUT = (-2147483648.999f);

    [NativeTypeName("#define D3D12_FTOU_INSTRUCTION_MAX_INPUT ( 4294967295.999f )")]
    public const float D3D12_FTOU_INSTRUCTION_MAX_INPUT = (4294967295.999f);

    [NativeTypeName("#define D3D12_FTOU_INSTRUCTION_MIN_INPUT ( 0.0f )")]
    public const float D3D12_FTOU_INSTRUCTION_MIN_INPUT = (0.0f);

    [NativeTypeName("#define D3D12_GS_INPUT_INSTANCE_ID_READS_PER_INST ( 2 )")]
    public const int D3D12_GS_INPUT_INSTANCE_ID_READS_PER_INST = (2);

    [NativeTypeName("#define D3D12_GS_INPUT_INSTANCE_ID_READ_PORTS ( 1 )")]
    public const int D3D12_GS_INPUT_INSTANCE_ID_READ_PORTS = (1);

    [NativeTypeName("#define D3D12_GS_INPUT_INSTANCE_ID_REGISTER_COMPONENTS ( 1 )")]
    public const int D3D12_GS_INPUT_INSTANCE_ID_REGISTER_COMPONENTS = (1);

    [NativeTypeName("#define D3D12_GS_INPUT_INSTANCE_ID_REGISTER_COMPONENT_BIT_COUNT ( 32 )")]
    public const int D3D12_GS_INPUT_INSTANCE_ID_REGISTER_COMPONENT_BIT_COUNT = (32);

    [NativeTypeName("#define D3D12_GS_INPUT_INSTANCE_ID_REGISTER_COUNT ( 1 )")]
    public const int D3D12_GS_INPUT_INSTANCE_ID_REGISTER_COUNT = (1);

    [NativeTypeName("#define D3D12_GS_INPUT_PRIM_CONST_REGISTER_COMPONENTS ( 1 )")]
    public const int D3D12_GS_INPUT_PRIM_CONST_REGISTER_COMPONENTS = (1);

    [NativeTypeName("#define D3D12_GS_INPUT_PRIM_CONST_REGISTER_COMPONENT_BIT_COUNT ( 32 )")]
    public const int D3D12_GS_INPUT_PRIM_CONST_REGISTER_COMPONENT_BIT_COUNT = (32);

    [NativeTypeName("#define D3D12_GS_INPUT_PRIM_CONST_REGISTER_COUNT ( 1 )")]
    public const int D3D12_GS_INPUT_PRIM_CONST_REGISTER_COUNT = (1);

    [NativeTypeName("#define D3D12_GS_INPUT_PRIM_CONST_REGISTER_READS_PER_INST ( 2 )")]
    public const int D3D12_GS_INPUT_PRIM_CONST_REGISTER_READS_PER_INST = (2);

    [NativeTypeName("#define D3D12_GS_INPUT_PRIM_CONST_REGISTER_READ_PORTS ( 1 )")]
    public const int D3D12_GS_INPUT_PRIM_CONST_REGISTER_READ_PORTS = (1);

    [NativeTypeName("#define D3D12_GS_INPUT_REGISTER_COMPONENTS ( 4 )")]
    public const int D3D12_GS_INPUT_REGISTER_COMPONENTS = (4);

    [NativeTypeName("#define D3D12_GS_INPUT_REGISTER_COMPONENT_BIT_COUNT ( 32 )")]
    public const int D3D12_GS_INPUT_REGISTER_COMPONENT_BIT_COUNT = (32);

    [NativeTypeName("#define D3D12_GS_INPUT_REGISTER_COUNT ( 32 )")]
    public const int D3D12_GS_INPUT_REGISTER_COUNT = (32);

    [NativeTypeName("#define D3D12_GS_INPUT_REGISTER_READS_PER_INST ( 2 )")]
    public const int D3D12_GS_INPUT_REGISTER_READS_PER_INST = (2);

    [NativeTypeName("#define D3D12_GS_INPUT_REGISTER_READ_PORTS ( 1 )")]
    public const int D3D12_GS_INPUT_REGISTER_READ_PORTS = (1);

    [NativeTypeName("#define D3D12_GS_INPUT_REGISTER_VERTICES ( 32 )")]
    public const int D3D12_GS_INPUT_REGISTER_VERTICES = (32);

    [NativeTypeName("#define D3D12_GS_MAX_INSTANCE_COUNT ( 32 )")]
    public const int D3D12_GS_MAX_INSTANCE_COUNT = (32);

    [NativeTypeName("#define D3D12_GS_MAX_OUTPUT_VERTEX_COUNT_ACROSS_INSTANCES ( 1024 )")]
    public const int D3D12_GS_MAX_OUTPUT_VERTEX_COUNT_ACROSS_INSTANCES = (1024);

    [NativeTypeName("#define D3D12_GS_OUTPUT_ELEMENTS ( 32 )")]
    public const int D3D12_GS_OUTPUT_ELEMENTS = (32);

    [NativeTypeName("#define D3D12_GS_OUTPUT_REGISTER_COMPONENTS ( 4 )")]
    public const int D3D12_GS_OUTPUT_REGISTER_COMPONENTS = (4);

    [NativeTypeName("#define D3D12_GS_OUTPUT_REGISTER_COMPONENT_BIT_COUNT ( 32 )")]
    public const int D3D12_GS_OUTPUT_REGISTER_COMPONENT_BIT_COUNT = (32);

    [NativeTypeName("#define D3D12_GS_OUTPUT_REGISTER_COUNT ( 32 )")]
    public const int D3D12_GS_OUTPUT_REGISTER_COUNT = (32);

    [NativeTypeName("#define D3D12_GUID_TEXTURE_LAYOUT_ROW_MAJOR_HEIGHT_ALIGNMENT ( 4 )")]
    public const int D3D12_GUID_TEXTURE_LAYOUT_ROW_MAJOR_HEIGHT_ALIGNMENT = (4);

    [NativeTypeName("#define D3D12_GUID_TEXTURE_LAYOUT_ROW_MAJOR_PITCH_ALIGNMENT ( 256 )")]
    public const int D3D12_GUID_TEXTURE_LAYOUT_ROW_MAJOR_PITCH_ALIGNMENT = (256);

    [NativeTypeName("#define D3D12_GUID_TEXTURE_LAYOUT_ROW_MAJOR_PLANE_ALIGNMENT ( 4096 )")]
    public const int D3D12_GUID_TEXTURE_LAYOUT_ROW_MAJOR_PLANE_ALIGNMENT = (4096);

    [NativeTypeName("#define D3D12_HS_CONTROL_POINT_PHASE_INPUT_REGISTER_COUNT ( 32 )")]
    public const int D3D12_HS_CONTROL_POINT_PHASE_INPUT_REGISTER_COUNT = (32);

    [NativeTypeName("#define D3D12_HS_CONTROL_POINT_PHASE_OUTPUT_REGISTER_COUNT ( 32 )")]
    public const int D3D12_HS_CONTROL_POINT_PHASE_OUTPUT_REGISTER_COUNT = (32);

    [NativeTypeName("#define D3D12_HS_CONTROL_POINT_REGISTER_COMPONENTS ( 4 )")]
    public const int D3D12_HS_CONTROL_POINT_REGISTER_COMPONENTS = (4);

    [NativeTypeName("#define D3D12_HS_CONTROL_POINT_REGISTER_COMPONENT_BIT_COUNT ( 32 )")]
    public const int D3D12_HS_CONTROL_POINT_REGISTER_COMPONENT_BIT_COUNT = (32);

    [NativeTypeName("#define D3D12_HS_CONTROL_POINT_REGISTER_READS_PER_INST ( 2 )")]
    public const int D3D12_HS_CONTROL_POINT_REGISTER_READS_PER_INST = (2);

    [NativeTypeName("#define D3D12_HS_CONTROL_POINT_REGISTER_READ_PORTS ( 1 )")]
    public const int D3D12_HS_CONTROL_POINT_REGISTER_READ_PORTS = (1);

    [NativeTypeName("#define D3D12_HS_FORK_PHASE_INSTANCE_COUNT_UPPER_BOUND ( 0xffffffff )")]
    public const uint D3D12_HS_FORK_PHASE_INSTANCE_COUNT_UPPER_BOUND = (0xffffffff);

    [NativeTypeName("#define D3D12_HS_INPUT_FORK_INSTANCE_ID_REGISTER_COMPONENTS ( 1 )")]
    public const int D3D12_HS_INPUT_FORK_INSTANCE_ID_REGISTER_COMPONENTS = (1);

    [NativeTypeName("#define D3D12_HS_INPUT_FORK_INSTANCE_ID_REGISTER_COMPONENT_BIT_COUNT ( 32 )")]
    public const int D3D12_HS_INPUT_FORK_INSTANCE_ID_REGISTER_COMPONENT_BIT_COUNT = (32);

    [NativeTypeName("#define D3D12_HS_INPUT_FORK_INSTANCE_ID_REGISTER_COUNT ( 1 )")]
    public const int D3D12_HS_INPUT_FORK_INSTANCE_ID_REGISTER_COUNT = (1);

    [NativeTypeName("#define D3D12_HS_INPUT_FORK_INSTANCE_ID_REGISTER_READS_PER_INST ( 2 )")]
    public const int D3D12_HS_INPUT_FORK_INSTANCE_ID_REGISTER_READS_PER_INST = (2);

    [NativeTypeName("#define D3D12_HS_INPUT_FORK_INSTANCE_ID_REGISTER_READ_PORTS ( 1 )")]
    public const int D3D12_HS_INPUT_FORK_INSTANCE_ID_REGISTER_READ_PORTS = (1);

    [NativeTypeName("#define D3D12_HS_INPUT_JOIN_INSTANCE_ID_REGISTER_COMPONENTS ( 1 )")]
    public const int D3D12_HS_INPUT_JOIN_INSTANCE_ID_REGISTER_COMPONENTS = (1);

    [NativeTypeName("#define D3D12_HS_INPUT_JOIN_INSTANCE_ID_REGISTER_COMPONENT_BIT_COUNT ( 32 )")]
    public const int D3D12_HS_INPUT_JOIN_INSTANCE_ID_REGISTER_COMPONENT_BIT_COUNT = (32);

    [NativeTypeName("#define D3D12_HS_INPUT_JOIN_INSTANCE_ID_REGISTER_COUNT ( 1 )")]
    public const int D3D12_HS_INPUT_JOIN_INSTANCE_ID_REGISTER_COUNT = (1);

    [NativeTypeName("#define D3D12_HS_INPUT_JOIN_INSTANCE_ID_REGISTER_READS_PER_INST ( 2 )")]
    public const int D3D12_HS_INPUT_JOIN_INSTANCE_ID_REGISTER_READS_PER_INST = (2);

    [NativeTypeName("#define D3D12_HS_INPUT_JOIN_INSTANCE_ID_REGISTER_READ_PORTS ( 1 )")]
    public const int D3D12_HS_INPUT_JOIN_INSTANCE_ID_REGISTER_READ_PORTS = (1);

    [NativeTypeName("#define D3D12_HS_INPUT_PRIMITIVE_ID_REGISTER_COMPONENTS ( 1 )")]
    public const int D3D12_HS_INPUT_PRIMITIVE_ID_REGISTER_COMPONENTS = (1);

    [NativeTypeName("#define D3D12_HS_INPUT_PRIMITIVE_ID_REGISTER_COMPONENT_BIT_COUNT ( 32 )")]
    public const int D3D12_HS_INPUT_PRIMITIVE_ID_REGISTER_COMPONENT_BIT_COUNT = (32);

    [NativeTypeName("#define D3D12_HS_INPUT_PRIMITIVE_ID_REGISTER_COUNT ( 1 )")]
    public const int D3D12_HS_INPUT_PRIMITIVE_ID_REGISTER_COUNT = (1);

    [NativeTypeName("#define D3D12_HS_INPUT_PRIMITIVE_ID_REGISTER_READS_PER_INST ( 2 )")]
    public const int D3D12_HS_INPUT_PRIMITIVE_ID_REGISTER_READS_PER_INST = (2);

    [NativeTypeName("#define D3D12_HS_INPUT_PRIMITIVE_ID_REGISTER_READ_PORTS ( 1 )")]
    public const int D3D12_HS_INPUT_PRIMITIVE_ID_REGISTER_READ_PORTS = (1);

    [NativeTypeName("#define D3D12_HS_JOIN_PHASE_INSTANCE_COUNT_UPPER_BOUND ( 0xffffffff )")]
    public const uint D3D12_HS_JOIN_PHASE_INSTANCE_COUNT_UPPER_BOUND = (0xffffffff);

    [NativeTypeName("#define D3D12_HS_MAXTESSFACTOR_LOWER_BOUND ( 1.0f )")]
    public const float D3D12_HS_MAXTESSFACTOR_LOWER_BOUND = (1.0f);

    [NativeTypeName("#define D3D12_HS_MAXTESSFACTOR_UPPER_BOUND ( 64.0f )")]
    public const float D3D12_HS_MAXTESSFACTOR_UPPER_BOUND = (64.0f);

    [NativeTypeName("#define D3D12_HS_OUTPUT_CONTROL_POINTS_MAX_TOTAL_SCALARS ( 3968 )")]
    public const int D3D12_HS_OUTPUT_CONTROL_POINTS_MAX_TOTAL_SCALARS = (3968);

    [NativeTypeName("#define D3D12_HS_OUTPUT_CONTROL_POINT_ID_REGISTER_COMPONENTS ( 1 )")]
    public const int D3D12_HS_OUTPUT_CONTROL_POINT_ID_REGISTER_COMPONENTS = (1);

    [NativeTypeName("#define D3D12_HS_OUTPUT_CONTROL_POINT_ID_REGISTER_COMPONENT_BIT_COUNT ( 32 )")]
    public const int D3D12_HS_OUTPUT_CONTROL_POINT_ID_REGISTER_COMPONENT_BIT_COUNT = (32);

    [NativeTypeName("#define D3D12_HS_OUTPUT_CONTROL_POINT_ID_REGISTER_COUNT ( 1 )")]
    public const int D3D12_HS_OUTPUT_CONTROL_POINT_ID_REGISTER_COUNT = (1);

    [NativeTypeName("#define D3D12_HS_OUTPUT_CONTROL_POINT_ID_REGISTER_READS_PER_INST ( 2 )")]
    public const int D3D12_HS_OUTPUT_CONTROL_POINT_ID_REGISTER_READS_PER_INST = (2);

    [NativeTypeName("#define D3D12_HS_OUTPUT_CONTROL_POINT_ID_REGISTER_READ_PORTS ( 1 )")]
    public const int D3D12_HS_OUTPUT_CONTROL_POINT_ID_REGISTER_READ_PORTS = (1);

    [NativeTypeName("#define D3D12_HS_OUTPUT_PATCH_CONSTANT_REGISTER_COMPONENTS ( 4 )")]
    public const int D3D12_HS_OUTPUT_PATCH_CONSTANT_REGISTER_COMPONENTS = (4);

    [NativeTypeName("#define D3D12_HS_OUTPUT_PATCH_CONSTANT_REGISTER_COMPONENT_BIT_COUNT ( 32 )")]
    public const int D3D12_HS_OUTPUT_PATCH_CONSTANT_REGISTER_COMPONENT_BIT_COUNT = (32);

    [NativeTypeName("#define D3D12_HS_OUTPUT_PATCH_CONSTANT_REGISTER_COUNT ( 32 )")]
    public const int D3D12_HS_OUTPUT_PATCH_CONSTANT_REGISTER_COUNT = (32);

    [NativeTypeName("#define D3D12_HS_OUTPUT_PATCH_CONSTANT_REGISTER_READS_PER_INST ( 2 )")]
    public const int D3D12_HS_OUTPUT_PATCH_CONSTANT_REGISTER_READS_PER_INST = (2);

    [NativeTypeName("#define D3D12_HS_OUTPUT_PATCH_CONSTANT_REGISTER_READ_PORTS ( 1 )")]
    public const int D3D12_HS_OUTPUT_PATCH_CONSTANT_REGISTER_READ_PORTS = (1);

    [NativeTypeName("#define D3D12_HS_OUTPUT_PATCH_CONSTANT_REGISTER_SCALAR_COMPONENTS ( 128 )")]
    public const int D3D12_HS_OUTPUT_PATCH_CONSTANT_REGISTER_SCALAR_COMPONENTS = (128);

    [NativeTypeName("#define D3D12_IA_DEFAULT_INDEX_BUFFER_OFFSET_IN_BYTES ( 0 )")]
    public const int D3D12_IA_DEFAULT_INDEX_BUFFER_OFFSET_IN_BYTES = (0);

    [NativeTypeName("#define D3D12_IA_DEFAULT_PRIMITIVE_TOPOLOGY ( 0 )")]
    public const int D3D12_IA_DEFAULT_PRIMITIVE_TOPOLOGY = (0);

    [NativeTypeName("#define D3D12_IA_DEFAULT_VERTEX_BUFFER_OFFSET_IN_BYTES ( 0 )")]
    public const int D3D12_IA_DEFAULT_VERTEX_BUFFER_OFFSET_IN_BYTES = (0);

    [NativeTypeName("#define D3D12_IA_INDEX_INPUT_RESOURCE_SLOT_COUNT ( 1 )")]
    public const int D3D12_IA_INDEX_INPUT_RESOURCE_SLOT_COUNT = (1);

    [NativeTypeName("#define D3D12_IA_INSTANCE_ID_BIT_COUNT ( 32 )")]
    public const int D3D12_IA_INSTANCE_ID_BIT_COUNT = (32);

    [NativeTypeName("#define D3D12_IA_INTEGER_ARITHMETIC_BIT_COUNT ( 32 )")]
    public const int D3D12_IA_INTEGER_ARITHMETIC_BIT_COUNT = (32);

    [NativeTypeName("#define D3D12_IA_PATCH_MAX_CONTROL_POINT_COUNT ( 32 )")]
    public const int D3D12_IA_PATCH_MAX_CONTROL_POINT_COUNT = (32);

    [NativeTypeName("#define D3D12_IA_PRIMITIVE_ID_BIT_COUNT ( 32 )")]
    public const int D3D12_IA_PRIMITIVE_ID_BIT_COUNT = (32);

    [NativeTypeName("#define D3D12_IA_VERTEX_ID_BIT_COUNT ( 32 )")]
    public const int D3D12_IA_VERTEX_ID_BIT_COUNT = (32);

    [NativeTypeName("#define D3D12_IA_VERTEX_INPUT_RESOURCE_SLOT_COUNT ( 32 )")]
    public const int D3D12_IA_VERTEX_INPUT_RESOURCE_SLOT_COUNT = (32);

    [NativeTypeName("#define D3D12_IA_VERTEX_INPUT_STRUCTURE_ELEMENTS_COMPONENTS ( 128 )")]
    public const int D3D12_IA_VERTEX_INPUT_STRUCTURE_ELEMENTS_COMPONENTS = (128);

    [NativeTypeName("#define D3D12_IA_VERTEX_INPUT_STRUCTURE_ELEMENT_COUNT ( 32 )")]
    public const int D3D12_IA_VERTEX_INPUT_STRUCTURE_ELEMENT_COUNT = (32);

    [NativeTypeName("#define D3D12_INTEGER_DIVIDE_BY_ZERO_QUOTIENT ( 0xffffffff )")]
    public const uint D3D12_INTEGER_DIVIDE_BY_ZERO_QUOTIENT = (0xffffffff);

    [NativeTypeName("#define D3D12_INTEGER_DIVIDE_BY_ZERO_REMAINDER ( 0xffffffff )")]
    public const uint D3D12_INTEGER_DIVIDE_BY_ZERO_REMAINDER = (0xffffffff);

    [NativeTypeName("#define D3D12_KEEP_RENDER_TARGETS_AND_DEPTH_STENCIL ( 0xffffffff )")]
    public const uint D3D12_KEEP_RENDER_TARGETS_AND_DEPTH_STENCIL = (0xffffffff);

    [NativeTypeName("#define D3D12_KEEP_UNORDERED_ACCESS_VIEWS ( 0xffffffff )")]
    public const uint D3D12_KEEP_UNORDERED_ACCESS_VIEWS = (0xffffffff);

    [NativeTypeName("#define D3D12_LINEAR_GAMMA ( 1.0f )")]
    public const float D3D12_LINEAR_GAMMA = (1.0f);

    [NativeTypeName("#define D3D12_MAJOR_VERSION ( 12 )")]
    public const int D3D12_MAJOR_VERSION = (12);

    [NativeTypeName("#define D3D12_MAX_BORDER_COLOR_COMPONENT ( 1.0f )")]
    public const float D3D12_MAX_BORDER_COLOR_COMPONENT = (1.0f);

    [NativeTypeName("#define D3D12_MAX_DEPTH ( 1.0f )")]
    public const float D3D12_MAX_DEPTH = (1.0f);

    [NativeTypeName("#define D3D12_MAX_LIVE_STATIC_SAMPLERS ( 2032 )")]
    public const int D3D12_MAX_LIVE_STATIC_SAMPLERS = (2032);

    [NativeTypeName("#define D3D12_MAX_MAXANISOTROPY ( 16 )")]
    public const int D3D12_MAX_MAXANISOTROPY = (16);

    [NativeTypeName("#define D3D12_MAX_MULTISAMPLE_SAMPLE_COUNT ( 32 )")]
    public const int D3D12_MAX_MULTISAMPLE_SAMPLE_COUNT = (32);

    [NativeTypeName("#define D3D12_MAX_POSITION_VALUE ( 3.402823466e+34f )")]
    public const float D3D12_MAX_POSITION_VALUE = (3.402823466e+34f);

    [NativeTypeName("#define D3D12_MAX_ROOT_COST ( 64 )")]
    public const int D3D12_MAX_ROOT_COST = (64);

    [NativeTypeName("#define D3D12_MAX_SHADER_VISIBLE_DESCRIPTOR_HEAP_SIZE_TIER_1 ( 1000000 )")]
    public const int D3D12_MAX_SHADER_VISIBLE_DESCRIPTOR_HEAP_SIZE_TIER_1 = (1000000);

    [NativeTypeName("#define D3D12_MAX_SHADER_VISIBLE_DESCRIPTOR_HEAP_SIZE_TIER_2 ( 1000000 )")]
    public const int D3D12_MAX_SHADER_VISIBLE_DESCRIPTOR_HEAP_SIZE_TIER_2 = (1000000);

    [NativeTypeName("#define D3D12_MAX_SHADER_VISIBLE_SAMPLER_HEAP_SIZE ( 2048 )")]
    public const int D3D12_MAX_SHADER_VISIBLE_SAMPLER_HEAP_SIZE = (2048);

    [NativeTypeName("#define D3D12_MAX_TEXTURE_DIMENSION_2_TO_EXP ( 17 )")]
    public const int D3D12_MAX_TEXTURE_DIMENSION_2_TO_EXP = (17);

    [NativeTypeName("#define D3D12_MAX_VIEW_INSTANCE_COUNT ( 4 )")]
    public const int D3D12_MAX_VIEW_INSTANCE_COUNT = (4);

    [NativeTypeName("#define D3D12_MINOR_VERSION ( 0 )")]
    public const int D3D12_MINOR_VERSION = (0);

    [NativeTypeName("#define D3D12_MIN_BORDER_COLOR_COMPONENT ( 0.0f )")]
    public const float D3D12_MIN_BORDER_COLOR_COMPONENT = (0.0f);

    [NativeTypeName("#define D3D12_MIN_DEPTH ( 0.0f )")]
    public const float D3D12_MIN_DEPTH = (0.0f);

    [NativeTypeName("#define D3D12_MIN_MAXANISOTROPY ( 0 )")]
    public const int D3D12_MIN_MAXANISOTROPY = (0);

    [NativeTypeName("#define D3D12_MIP_LOD_BIAS_MAX ( 15.99f )")]
    public const float D3D12_MIP_LOD_BIAS_MAX = (15.99f);

    [NativeTypeName("#define D3D12_MIP_LOD_BIAS_MIN ( -16.0f )")]
    public const float D3D12_MIP_LOD_BIAS_MIN = (-16.0f);

    [NativeTypeName("#define D3D12_MIP_LOD_FRACTIONAL_BIT_COUNT ( 8 )")]
    public const int D3D12_MIP_LOD_FRACTIONAL_BIT_COUNT = (8);

    [NativeTypeName("#define D3D12_MIP_LOD_RANGE_BIT_COUNT ( 8 )")]
    public const int D3D12_MIP_LOD_RANGE_BIT_COUNT = (8);

    [NativeTypeName("#define D3D12_MS_DISPATCH_MAX_THREAD_GROUPS_PER_GRID ( 4194303 )")]
    public const int D3D12_MS_DISPATCH_MAX_THREAD_GROUPS_PER_GRID = (4194303);

    [NativeTypeName("#define D3D12_MS_TGSM_BYTES_MINIMUM_SUPPORT ( 28672 )")]
    public const int D3D12_MS_TGSM_BYTES_MINIMUM_SUPPORT = (28672);

    [NativeTypeName("#define D3D12_MULTISAMPLE_ANTIALIAS_LINE_WIDTH ( 1.4f )")]
    public const float D3D12_MULTISAMPLE_ANTIALIAS_LINE_WIDTH = (1.4f);

    [NativeTypeName("#define D3D12_NONSAMPLE_FETCH_OUT_OF_RANGE_ACCESS_RESULT ( 0 )")]
    public const int D3D12_NONSAMPLE_FETCH_OUT_OF_RANGE_ACCESS_RESULT = (0);

    [NativeTypeName("#define D3D12_OS_RESERVED_REGISTER_SPACE_VALUES_END ( 0xffffffff )")]
    public const uint D3D12_OS_RESERVED_REGISTER_SPACE_VALUES_END = (0xffffffff);

    [NativeTypeName("#define D3D12_OS_RESERVED_REGISTER_SPACE_VALUES_START ( 0xfffffff8 )")]
    public const uint D3D12_OS_RESERVED_REGISTER_SPACE_VALUES_START = (0xfffffff8);

    [NativeTypeName("#define D3D12_PACKED_TILE ( 0xffffffff )")]
    public const uint D3D12_PACKED_TILE = (0xffffffff);

    [NativeTypeName("#define D3D12_PIXEL_ADDRESS_RANGE_BIT_COUNT ( 15 )")]
    public const int D3D12_PIXEL_ADDRESS_RANGE_BIT_COUNT = (15);

    [NativeTypeName("#define D3D12_PREVIEW_SDK_VERSION ( 722 )")]
    public const int D3D12_PREVIEW_SDK_VERSION = (722);

    [NativeTypeName("#define D3D12_PRE_SCISSOR_PIXEL_ADDRESS_RANGE_BIT_COUNT ( 16 )")]
    public const int D3D12_PRE_SCISSOR_PIXEL_ADDRESS_RANGE_BIT_COUNT = (16);

    [NativeTypeName("#define D3D12_PS_CS_UAV_REGISTER_COMPONENTS ( 1 )")]
    public const int D3D12_PS_CS_UAV_REGISTER_COMPONENTS = (1);

    [NativeTypeName("#define D3D12_PS_CS_UAV_REGISTER_COUNT ( 8 )")]
    public const int D3D12_PS_CS_UAV_REGISTER_COUNT = (8);

    [NativeTypeName("#define D3D12_PS_CS_UAV_REGISTER_READS_PER_INST ( 1 )")]
    public const int D3D12_PS_CS_UAV_REGISTER_READS_PER_INST = (1);

    [NativeTypeName("#define D3D12_PS_CS_UAV_REGISTER_READ_PORTS ( 1 )")]
    public const int D3D12_PS_CS_UAV_REGISTER_READ_PORTS = (1);

    [NativeTypeName("#define D3D12_PS_FRONTFACING_DEFAULT_VALUE ( 0xffffffff )")]
    public const uint D3D12_PS_FRONTFACING_DEFAULT_VALUE = (0xffffffff);

    [NativeTypeName("#define D3D12_PS_FRONTFACING_FALSE_VALUE ( 0 )")]
    public const int D3D12_PS_FRONTFACING_FALSE_VALUE = (0);

    [NativeTypeName("#define D3D12_PS_FRONTFACING_TRUE_VALUE ( 0xffffffff )")]
    public const uint D3D12_PS_FRONTFACING_TRUE_VALUE = (0xffffffff);

    [NativeTypeName("#define D3D12_PS_INPUT_REGISTER_COMPONENTS ( 4 )")]
    public const int D3D12_PS_INPUT_REGISTER_COMPONENTS = (4);

    [NativeTypeName("#define D3D12_PS_INPUT_REGISTER_COMPONENT_BIT_COUNT ( 32 )")]
    public const int D3D12_PS_INPUT_REGISTER_COMPONENT_BIT_COUNT = (32);

    [NativeTypeName("#define D3D12_PS_INPUT_REGISTER_COUNT ( 32 )")]
    public const int D3D12_PS_INPUT_REGISTER_COUNT = (32);

    [NativeTypeName("#define D3D12_PS_INPUT_REGISTER_READS_PER_INST ( 2 )")]
    public const int D3D12_PS_INPUT_REGISTER_READS_PER_INST = (2);

    [NativeTypeName("#define D3D12_PS_INPUT_REGISTER_READ_PORTS ( 1 )")]
    public const int D3D12_PS_INPUT_REGISTER_READ_PORTS = (1);

    [NativeTypeName("#define D3D12_PS_LEGACY_PIXEL_CENTER_FRACTIONAL_COMPONENT ( 0.0f )")]
    public const float D3D12_PS_LEGACY_PIXEL_CENTER_FRACTIONAL_COMPONENT = (0.0f);

    [NativeTypeName("#define D3D12_PS_OUTPUT_DEPTH_REGISTER_COMPONENTS ( 1 )")]
    public const int D3D12_PS_OUTPUT_DEPTH_REGISTER_COMPONENTS = (1);

    [NativeTypeName("#define D3D12_PS_OUTPUT_DEPTH_REGISTER_COMPONENT_BIT_COUNT ( 32 )")]
    public const int D3D12_PS_OUTPUT_DEPTH_REGISTER_COMPONENT_BIT_COUNT = (32);

    [NativeTypeName("#define D3D12_PS_OUTPUT_DEPTH_REGISTER_COUNT ( 1 )")]
    public const int D3D12_PS_OUTPUT_DEPTH_REGISTER_COUNT = (1);

    [NativeTypeName("#define D3D12_PS_OUTPUT_MASK_REGISTER_COMPONENTS ( 1 )")]
    public const int D3D12_PS_OUTPUT_MASK_REGISTER_COMPONENTS = (1);

    [NativeTypeName("#define D3D12_PS_OUTPUT_MASK_REGISTER_COMPONENT_BIT_COUNT ( 32 )")]
    public const int D3D12_PS_OUTPUT_MASK_REGISTER_COMPONENT_BIT_COUNT = (32);

    [NativeTypeName("#define D3D12_PS_OUTPUT_MASK_REGISTER_COUNT ( 1 )")]
    public const int D3D12_PS_OUTPUT_MASK_REGISTER_COUNT = (1);

    [NativeTypeName("#define D3D12_PS_OUTPUT_REGISTER_COMPONENTS ( 4 )")]
    public const int D3D12_PS_OUTPUT_REGISTER_COMPONENTS = (4);

    [NativeTypeName("#define D3D12_PS_OUTPUT_REGISTER_COMPONENT_BIT_COUNT ( 32 )")]
    public const int D3D12_PS_OUTPUT_REGISTER_COMPONENT_BIT_COUNT = (32);

    [NativeTypeName("#define D3D12_PS_OUTPUT_REGISTER_COUNT ( 8 )")]
    public const int D3D12_PS_OUTPUT_REGISTER_COUNT = (8);

    [NativeTypeName("#define D3D12_PS_PIXEL_CENTER_FRACTIONAL_COMPONENT ( 0.5f )")]
    public const float D3D12_PS_PIXEL_CENTER_FRACTIONAL_COMPONENT = (0.5f);

    [NativeTypeName("#define D3D12_RAW_UAV_SRV_BYTE_ALIGNMENT ( 16 )")]
    public const int D3D12_RAW_UAV_SRV_BYTE_ALIGNMENT = (16);

    [NativeTypeName("#define D3D12_RAYTRACING_AABB_BYTE_ALIGNMENT ( 8 )")]
    public const int D3D12_RAYTRACING_AABB_BYTE_ALIGNMENT = (8);

    [NativeTypeName("#define D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BYTE_ALIGNMENT ( 256 )")]
    public const int D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BYTE_ALIGNMENT = (256);

    [NativeTypeName("#define D3D12_RAYTRACING_INSTANCE_DESCS_BYTE_ALIGNMENT ( 16 )")]
    public const int D3D12_RAYTRACING_INSTANCE_DESCS_BYTE_ALIGNMENT = (16);

    [NativeTypeName("#define D3D12_RAYTRACING_MAX_ATTRIBUTE_SIZE_IN_BYTES ( 32 )")]
    public const int D3D12_RAYTRACING_MAX_ATTRIBUTE_SIZE_IN_BYTES = (32);

    [NativeTypeName("#define D3D12_RAYTRACING_MAX_DECLARABLE_TRACE_RECURSION_DEPTH ( 31 )")]
    public const int D3D12_RAYTRACING_MAX_DECLARABLE_TRACE_RECURSION_DEPTH = (31);

    [NativeTypeName("#define D3D12_RAYTRACING_MAX_GEOMETRIES_PER_BOTTOM_LEVEL_ACCELERATION_STRUCTURE ( 16777216 )")]
    public const int D3D12_RAYTRACING_MAX_GEOMETRIES_PER_BOTTOM_LEVEL_ACCELERATION_STRUCTURE = (16777216);

    [NativeTypeName("#define D3D12_RAYTRACING_MAX_INSTANCES_PER_TOP_LEVEL_ACCELERATION_STRUCTURE ( 16777216 )")]
    public const int D3D12_RAYTRACING_MAX_INSTANCES_PER_TOP_LEVEL_ACCELERATION_STRUCTURE = (16777216);

    [NativeTypeName("#define D3D12_RAYTRACING_MAX_PRIMITIVES_PER_BOTTOM_LEVEL_ACCELERATION_STRUCTURE ( 536870912 )")]
    public const int D3D12_RAYTRACING_MAX_PRIMITIVES_PER_BOTTOM_LEVEL_ACCELERATION_STRUCTURE = (536870912);

    [NativeTypeName("#define D3D12_RAYTRACING_MAX_RAY_GENERATION_SHADER_THREADS ( 1073741824 )")]
    public const int D3D12_RAYTRACING_MAX_RAY_GENERATION_SHADER_THREADS = (1073741824);

    [NativeTypeName("#define D3D12_RAYTRACING_MAX_SHADER_RECORD_STRIDE ( 4096 )")]
    public const int D3D12_RAYTRACING_MAX_SHADER_RECORD_STRIDE = (4096);

    [NativeTypeName("#define D3D12_RAYTRACING_OPACITY_MICROMAP_ARRAY_BYTE_ALIGNMENT ( 128 )")]
    public const int D3D12_RAYTRACING_OPACITY_MICROMAP_ARRAY_BYTE_ALIGNMENT = (128);

    [NativeTypeName("#define D3D12_RAYTRACING_OPACITY_MICROMAP_DESCS_BYTE_ALIGNMENT ( 4 )")]
    public const int D3D12_RAYTRACING_OPACITY_MICROMAP_DESCS_BYTE_ALIGNMENT = (4);

    [NativeTypeName("#define D3D12_RAYTRACING_OPACITY_MICROMAP_OC1_MAX_SUBDIVISION_LEVEL ( 12 )")]
    public const int D3D12_RAYTRACING_OPACITY_MICROMAP_OC1_MAX_SUBDIVISION_LEVEL = (12);

    [NativeTypeName("#define D3D12_RAYTRACING_SHADER_RECORD_BYTE_ALIGNMENT ( 32 )")]
    public const int D3D12_RAYTRACING_SHADER_RECORD_BYTE_ALIGNMENT = (32);

    [NativeTypeName("#define D3D12_RAYTRACING_SHADER_TABLE_BYTE_ALIGNMENT ( 64 )")]
    public const int D3D12_RAYTRACING_SHADER_TABLE_BYTE_ALIGNMENT = (64);

    [NativeTypeName("#define D3D12_RAYTRACING_TRANSFORM3X4_BYTE_ALIGNMENT ( 16 )")]
    public const int D3D12_RAYTRACING_TRANSFORM3X4_BYTE_ALIGNMENT = (16);

    [NativeTypeName("#define D3D12_REQ_BLEND_OBJECT_COUNT_PER_DEVICE ( 4096 )")]
    public const int D3D12_REQ_BLEND_OBJECT_COUNT_PER_DEVICE = (4096);

    [NativeTypeName("#define D3D12_REQ_BUFFER_RESOURCE_TEXEL_COUNT_2_TO_EXP ( 27 )")]
    public const int D3D12_REQ_BUFFER_RESOURCE_TEXEL_COUNT_2_TO_EXP = (27);

    [NativeTypeName("#define D3D12_REQ_CONSTANT_BUFFER_ELEMENT_COUNT ( 4096 )")]
    public const int D3D12_REQ_CONSTANT_BUFFER_ELEMENT_COUNT = (4096);

    [NativeTypeName("#define D3D12_REQ_DEPTH_STENCIL_OBJECT_COUNT_PER_DEVICE ( 4096 )")]
    public const int D3D12_REQ_DEPTH_STENCIL_OBJECT_COUNT_PER_DEVICE = (4096);

    [NativeTypeName("#define D3D12_REQ_DRAWINDEXED_INDEX_COUNT_2_TO_EXP ( 32 )")]
    public const int D3D12_REQ_DRAWINDEXED_INDEX_COUNT_2_TO_EXP = (32);

    [NativeTypeName("#define D3D12_REQ_DRAW_VERTEX_COUNT_2_TO_EXP ( 32 )")]
    public const int D3D12_REQ_DRAW_VERTEX_COUNT_2_TO_EXP = (32);

    [NativeTypeName("#define D3D12_REQ_FILTERING_HW_ADDRESSABLE_RESOURCE_DIMENSION ( 16384 )")]
    public const int D3D12_REQ_FILTERING_HW_ADDRESSABLE_RESOURCE_DIMENSION = (16384);

    [NativeTypeName("#define D3D12_REQ_GS_INVOCATION_32BIT_OUTPUT_COMPONENT_LIMIT ( 1024 )")]
    public const int D3D12_REQ_GS_INVOCATION_32BIT_OUTPUT_COMPONENT_LIMIT = (1024);

    [NativeTypeName("#define D3D12_REQ_IMMEDIATE_CONSTANT_BUFFER_ELEMENT_COUNT ( 4096 )")]
    public const int D3D12_REQ_IMMEDIATE_CONSTANT_BUFFER_ELEMENT_COUNT = (4096);

    [NativeTypeName("#define D3D12_REQ_MAXANISOTROPY ( 16 )")]
    public const int D3D12_REQ_MAXANISOTROPY = (16);

    [NativeTypeName("#define D3D12_REQ_MIP_LEVELS ( 15 )")]
    public const int D3D12_REQ_MIP_LEVELS = (15);

    [NativeTypeName("#define D3D12_REQ_MULTI_ELEMENT_STRUCTURE_SIZE_IN_BYTES ( 2048 )")]
    public const int D3D12_REQ_MULTI_ELEMENT_STRUCTURE_SIZE_IN_BYTES = (2048);

    [NativeTypeName("#define D3D12_REQ_RASTERIZER_OBJECT_COUNT_PER_DEVICE ( 4096 )")]
    public const int D3D12_REQ_RASTERIZER_OBJECT_COUNT_PER_DEVICE = (4096);

    [NativeTypeName("#define D3D12_REQ_RENDER_TO_BUFFER_WINDOW_WIDTH ( 16384 )")]
    public const int D3D12_REQ_RENDER_TO_BUFFER_WINDOW_WIDTH = (16384);

    [NativeTypeName("#define D3D12_REQ_RESOURCE_SIZE_IN_MEGABYTES_EXPRESSION_A_TERM ( 128 )")]
    public const int D3D12_REQ_RESOURCE_SIZE_IN_MEGABYTES_EXPRESSION_A_TERM = (128);

    [NativeTypeName("#define D3D12_REQ_RESOURCE_SIZE_IN_MEGABYTES_EXPRESSION_B_TERM ( 0.25f )")]
    public const float D3D12_REQ_RESOURCE_SIZE_IN_MEGABYTES_EXPRESSION_B_TERM = (0.25f);

    [NativeTypeName("#define D3D12_REQ_RESOURCE_SIZE_IN_MEGABYTES_EXPRESSION_C_TERM ( 2048 )")]
    public const int D3D12_REQ_RESOURCE_SIZE_IN_MEGABYTES_EXPRESSION_C_TERM = (2048);

    [NativeTypeName("#define D3D12_REQ_RESOURCE_VIEW_COUNT_PER_DEVICE_2_TO_EXP ( 20 )")]
    public const int D3D12_REQ_RESOURCE_VIEW_COUNT_PER_DEVICE_2_TO_EXP = (20);

    [NativeTypeName("#define D3D12_REQ_SAMPLER_OBJECT_COUNT_PER_DEVICE ( 4096 )")]
    public const int D3D12_REQ_SAMPLER_OBJECT_COUNT_PER_DEVICE = (4096);

    [NativeTypeName("#define D3D12_REQ_SUBRESOURCES ( 30720 )")]
    public const int D3D12_REQ_SUBRESOURCES = (30720);

    [NativeTypeName("#define D3D12_REQ_TEXTURE1D_ARRAY_AXIS_DIMENSION ( 2048 )")]
    public const int D3D12_REQ_TEXTURE1D_ARRAY_AXIS_DIMENSION = (2048);

    [NativeTypeName("#define D3D12_REQ_TEXTURE1D_U_DIMENSION ( 16384 )")]
    public const int D3D12_REQ_TEXTURE1D_U_DIMENSION = (16384);

    [NativeTypeName("#define D3D12_REQ_TEXTURE2D_ARRAY_AXIS_DIMENSION ( 2048 )")]
    public const int D3D12_REQ_TEXTURE2D_ARRAY_AXIS_DIMENSION = (2048);

    [NativeTypeName("#define D3D12_REQ_TEXTURE2D_U_OR_V_DIMENSION ( 16384 )")]
    public const int D3D12_REQ_TEXTURE2D_U_OR_V_DIMENSION = (16384);

    [NativeTypeName("#define D3D12_REQ_TEXTURE3D_U_V_OR_W_DIMENSION ( 2048 )")]
    public const int D3D12_REQ_TEXTURE3D_U_V_OR_W_DIMENSION = (2048);

    [NativeTypeName("#define D3D12_REQ_TEXTURECUBE_DIMENSION ( 16384 )")]
    public const int D3D12_REQ_TEXTURECUBE_DIMENSION = (16384);

    [NativeTypeName("#define D3D12_RESINFO_INSTRUCTION_MISSING_COMPONENT_RETVAL ( 0 )")]
    public const int D3D12_RESINFO_INSTRUCTION_MISSING_COMPONENT_RETVAL = (0);

    [NativeTypeName("#define D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES ( 0xffffffff )")]
    public const uint D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES = (0xffffffff);

    [NativeTypeName("#define D3D12_RETAIN_DUMP_FILE_DEFAULT ( 0 )")]
    public const int D3D12_RETAIN_DUMP_FILE_DEFAULT = (0);

    [NativeTypeName("#define D3D12_RS_SET_SHADING_RATE_COMBINER_COUNT ( 2 )")]
    public const int D3D12_RS_SET_SHADING_RATE_COMBINER_COUNT = (2);

    [NativeTypeName("#define D3D12_SDK_VERSION ( 620 )")]
    public const int D3D12_SDK_VERSION = (620);

    [NativeTypeName("#define D3D12_SHADER_IDENTIFIER_SIZE_IN_BYTES ( 32 )")]
    public const int D3D12_SHADER_IDENTIFIER_SIZE_IN_BYTES = (32);

    [NativeTypeName("#define D3D12_SHADER_MAJOR_VERSION ( 5 )")]
    public const int D3D12_SHADER_MAJOR_VERSION = (5);

    [NativeTypeName("#define D3D12_SHADER_MAX_INSTANCES ( 65535 )")]
    public const int D3D12_SHADER_MAX_INSTANCES = (65535);

    [NativeTypeName("#define D3D12_SHADER_MAX_INTERFACES ( 253 )")]
    public const int D3D12_SHADER_MAX_INTERFACES = (253);

    [NativeTypeName("#define D3D12_SHADER_MAX_INTERFACE_CALL_SITES ( 4096 )")]
    public const int D3D12_SHADER_MAX_INTERFACE_CALL_SITES = (4096);

    [NativeTypeName("#define D3D12_SHADER_MAX_TYPES ( 65535 )")]
    public const int D3D12_SHADER_MAX_TYPES = (65535);

    [NativeTypeName("#define D3D12_SHADER_MINOR_VERSION ( 1 )")]
    public const int D3D12_SHADER_MINOR_VERSION = (1);

    [NativeTypeName("#define D3D12_SHIFT_INSTRUCTION_PAD_VALUE ( 0 )")]
    public const int D3D12_SHIFT_INSTRUCTION_PAD_VALUE = (0);

    [NativeTypeName("#define D3D12_SHIFT_INSTRUCTION_SHIFT_VALUE_BIT_COUNT ( 5 )")]
    public const int D3D12_SHIFT_INSTRUCTION_SHIFT_VALUE_BIT_COUNT = (5);

    [NativeTypeName("#define D3D12_SIMULTANEOUS_RENDER_TARGET_COUNT ( 8 )")]
    public const int D3D12_SIMULTANEOUS_RENDER_TARGET_COUNT = (8);

    [NativeTypeName("#define D3D12_SMALL_MSAA_RESOURCE_PLACEMENT_ALIGNMENT ( 65536 )")]
    public const int D3D12_SMALL_MSAA_RESOURCE_PLACEMENT_ALIGNMENT = (65536);

    [NativeTypeName("#define D3D12_SMALL_RESOURCE_PLACEMENT_ALIGNMENT ( 4096 )")]
    public const int D3D12_SMALL_RESOURCE_PLACEMENT_ALIGNMENT = (4096);

    [NativeTypeName("#define D3D12_SO_BUFFER_MAX_STRIDE_IN_BYTES ( 2048 )")]
    public const int D3D12_SO_BUFFER_MAX_STRIDE_IN_BYTES = (2048);

    [NativeTypeName("#define D3D12_SO_BUFFER_MAX_WRITE_WINDOW_IN_BYTES ( 512 )")]
    public const int D3D12_SO_BUFFER_MAX_WRITE_WINDOW_IN_BYTES = (512);

    [NativeTypeName("#define D3D12_SO_BUFFER_SLOT_COUNT ( 4 )")]
    public const int D3D12_SO_BUFFER_SLOT_COUNT = (4);

    [NativeTypeName("#define D3D12_SO_DDI_REGISTER_INDEX_DENOTING_GAP ( 0xffffffff )")]
    public const uint D3D12_SO_DDI_REGISTER_INDEX_DENOTING_GAP = (0xffffffff);

    [NativeTypeName("#define D3D12_SO_NO_RASTERIZED_STREAM ( 0xffffffff )")]
    public const uint D3D12_SO_NO_RASTERIZED_STREAM = (0xffffffff);

    [NativeTypeName("#define D3D12_SO_OUTPUT_COMPONENT_COUNT ( 128 )")]
    public const int D3D12_SO_OUTPUT_COMPONENT_COUNT = (128);

    [NativeTypeName("#define D3D12_SO_STREAM_COUNT ( 4 )")]
    public const int D3D12_SO_STREAM_COUNT = (4);

    [NativeTypeName("#define D3D12_SPEC_DATE_DAY ( 14 )")]
    public const int D3D12_SPEC_DATE_DAY = (14);

    [NativeTypeName("#define D3D12_SPEC_DATE_MONTH ( 11 )")]
    public const int D3D12_SPEC_DATE_MONTH = (11);

    [NativeTypeName("#define D3D12_SPEC_DATE_YEAR ( 2014 )")]
    public const int D3D12_SPEC_DATE_YEAR = (2014);

    [NativeTypeName("#define D3D12_SPEC_VERSION ( 1.16 )")]
    public const double D3D12_SPEC_VERSION = (1.16);

    [NativeTypeName("#define D3D12_SRGB_GAMMA ( 2.2f )")]
    public const float D3D12_SRGB_GAMMA = (2.2f);

    [NativeTypeName("#define D3D12_SRGB_TO_FLOAT_DENOMINATOR_1 ( 12.92f )")]
    public const float D3D12_SRGB_TO_FLOAT_DENOMINATOR_1 = (12.92f);

    [NativeTypeName("#define D3D12_SRGB_TO_FLOAT_DENOMINATOR_2 ( 1.055f )")]
    public const float D3D12_SRGB_TO_FLOAT_DENOMINATOR_2 = (1.055f);

    [NativeTypeName("#define D3D12_SRGB_TO_FLOAT_EXPONENT ( 2.4f )")]
    public const float D3D12_SRGB_TO_FLOAT_EXPONENT = (2.4f);

    [NativeTypeName("#define D3D12_SRGB_TO_FLOAT_OFFSET ( 0.055f )")]
    public const float D3D12_SRGB_TO_FLOAT_OFFSET = (0.055f);

    [NativeTypeName("#define D3D12_SRGB_TO_FLOAT_THRESHOLD ( 0.04045f )")]
    public const float D3D12_SRGB_TO_FLOAT_THRESHOLD = (0.04045f);

    [NativeTypeName("#define D3D12_SRGB_TO_FLOAT_TOLERANCE_IN_ULP ( 0.5f )")]
    public const float D3D12_SRGB_TO_FLOAT_TOLERANCE_IN_ULP = (0.5f);

    [NativeTypeName("#define D3D12_STANDARD_COMPONENT_BIT_COUNT ( 32 )")]
    public const int D3D12_STANDARD_COMPONENT_BIT_COUNT = (32);

    [NativeTypeName("#define D3D12_STANDARD_COMPONENT_BIT_COUNT_DOUBLED ( 64 )")]
    public const int D3D12_STANDARD_COMPONENT_BIT_COUNT_DOUBLED = (64);

    [NativeTypeName("#define D3D12_STANDARD_MAXIMUM_ELEMENT_ALIGNMENT_BYTE_MULTIPLE ( 4 )")]
    public const int D3D12_STANDARD_MAXIMUM_ELEMENT_ALIGNMENT_BYTE_MULTIPLE = (4);

    [NativeTypeName("#define D3D12_STANDARD_PIXEL_COMPONENT_COUNT ( 128 )")]
    public const int D3D12_STANDARD_PIXEL_COMPONENT_COUNT = (128);

    [NativeTypeName("#define D3D12_STANDARD_PIXEL_ELEMENT_COUNT ( 32 )")]
    public const int D3D12_STANDARD_PIXEL_ELEMENT_COUNT = (32);

    [NativeTypeName("#define D3D12_STANDARD_VECTOR_SIZE ( 4 )")]
    public const int D3D12_STANDARD_VECTOR_SIZE = (4);

    [NativeTypeName("#define D3D12_STANDARD_VERTEX_ELEMENT_COUNT ( 32 )")]
    public const int D3D12_STANDARD_VERTEX_ELEMENT_COUNT = (32);

    [NativeTypeName("#define D3D12_STANDARD_VERTEX_TOTAL_COMPONENT_COUNT ( 64 )")]
    public const int D3D12_STANDARD_VERTEX_TOTAL_COMPONENT_COUNT = (64);

    [NativeTypeName("#define D3D12_SUBPIXEL_FRACTIONAL_BIT_COUNT ( 8 )")]
    public const int D3D12_SUBPIXEL_FRACTIONAL_BIT_COUNT = (8);

    [NativeTypeName("#define D3D12_SUBTEXEL_FRACTIONAL_BIT_COUNT ( 8 )")]
    public const int D3D12_SUBTEXEL_FRACTIONAL_BIT_COUNT = (8);

    [NativeTypeName("#define D3D12_SYSTEM_RESERVED_REGISTER_SPACE_VALUES_END ( 0xffffffff )")]
    public const uint D3D12_SYSTEM_RESERVED_REGISTER_SPACE_VALUES_END = (0xffffffff);

    [NativeTypeName("#define D3D12_SYSTEM_RESERVED_REGISTER_SPACE_VALUES_START ( 0xfffffff0 )")]
    public const uint D3D12_SYSTEM_RESERVED_REGISTER_SPACE_VALUES_START = (0xfffffff0);

    [NativeTypeName("#define D3D12_TESSELLATOR_MAX_EVEN_TESSELLATION_FACTOR ( 64 )")]
    public const int D3D12_TESSELLATOR_MAX_EVEN_TESSELLATION_FACTOR = (64);

    [NativeTypeName("#define D3D12_TESSELLATOR_MAX_ISOLINE_DENSITY_TESSELLATION_FACTOR ( 64 )")]
    public const int D3D12_TESSELLATOR_MAX_ISOLINE_DENSITY_TESSELLATION_FACTOR = (64);

    [NativeTypeName("#define D3D12_TESSELLATOR_MAX_ODD_TESSELLATION_FACTOR ( 63 )")]
    public const int D3D12_TESSELLATOR_MAX_ODD_TESSELLATION_FACTOR = (63);

    [NativeTypeName("#define D3D12_TESSELLATOR_MAX_TESSELLATION_FACTOR ( 64 )")]
    public const int D3D12_TESSELLATOR_MAX_TESSELLATION_FACTOR = (64);

    [NativeTypeName("#define D3D12_TESSELLATOR_MIN_EVEN_TESSELLATION_FACTOR ( 2 )")]
    public const int D3D12_TESSELLATOR_MIN_EVEN_TESSELLATION_FACTOR = (2);

    [NativeTypeName("#define D3D12_TESSELLATOR_MIN_ISOLINE_DENSITY_TESSELLATION_FACTOR ( 1 )")]
    public const int D3D12_TESSELLATOR_MIN_ISOLINE_DENSITY_TESSELLATION_FACTOR = (1);

    [NativeTypeName("#define D3D12_TESSELLATOR_MIN_ODD_TESSELLATION_FACTOR ( 1 )")]
    public const int D3D12_TESSELLATOR_MIN_ODD_TESSELLATION_FACTOR = (1);

    [NativeTypeName("#define D3D12_TEXEL_ADDRESS_RANGE_BIT_COUNT ( 16 )")]
    public const int D3D12_TEXEL_ADDRESS_RANGE_BIT_COUNT = (16);

    [NativeTypeName("#define D3D12_TEXTURE_DATA_PITCH_ALIGNMENT ( 256 )")]
    public const int D3D12_TEXTURE_DATA_PITCH_ALIGNMENT = (256);

    [NativeTypeName("#define D3D12_TEXTURE_DATA_PLACEMENT_ALIGNMENT ( 512 )")]
    public const int D3D12_TEXTURE_DATA_PLACEMENT_ALIGNMENT = (512);

    [NativeTypeName("#define D3D12_TIGHT_ALIGNMENT_MAX_COMMITTED_BUFFER_ALIGNMENT ( 4096 )")]
    public const int D3D12_TIGHT_ALIGNMENT_MAX_COMMITTED_BUFFER_ALIGNMENT = (4096);

    [NativeTypeName("#define D3D12_TIGHT_ALIGNMENT_MAX_PLACED_BUFFER_ALIGNMENT ( 256 )")]
    public const int D3D12_TIGHT_ALIGNMENT_MAX_PLACED_BUFFER_ALIGNMENT = (256);

    [NativeTypeName("#define D3D12_TIGHT_ALIGNMENT_MIN_COMMITTED_RESOURCE_ALIGNMENT ( 4096 )")]
    public const int D3D12_TIGHT_ALIGNMENT_MIN_COMMITTED_RESOURCE_ALIGNMENT = (4096);

    [NativeTypeName("#define D3D12_TIGHT_ALIGNMENT_MIN_PLACED_RESOURCE_ALIGNMENT ( 8 )")]
    public const int D3D12_TIGHT_ALIGNMENT_MIN_PLACED_RESOURCE_ALIGNMENT = (8);

    [NativeTypeName("#define D3D12_TILED_RESOURCE_TILE_SIZE_IN_BYTES ( 65536 )")]
    public const int D3D12_TILED_RESOURCE_TILE_SIZE_IN_BYTES = (65536);

    [NativeTypeName("#define D3D12_TRACKED_WORKLOAD_MAX_INSTANCES ( 32 )")]
    public const int D3D12_TRACKED_WORKLOAD_MAX_INSTANCES = (32);

    [NativeTypeName("#define D3D12_UAV_COUNTER_PLACEMENT_ALIGNMENT ( 4096 )")]
    public const int D3D12_UAV_COUNTER_PLACEMENT_ALIGNMENT = (4096);

    [NativeTypeName("#define D3D12_UAV_SLOT_COUNT ( 64 )")]
    public const int D3D12_UAV_SLOT_COUNT = (64);

    [NativeTypeName("#define D3D12_UNBOUND_MEMORY_ACCESS_RESULT ( 0 )")]
    public const int D3D12_UNBOUND_MEMORY_ACCESS_RESULT = (0);

    [NativeTypeName("#define D3D12_VIDEO_DECODE_MAX_ARGUMENTS ( 10 )")]
    public const int D3D12_VIDEO_DECODE_MAX_ARGUMENTS = (10);

    [NativeTypeName("#define D3D12_VIDEO_DECODE_MAX_HISTOGRAM_COMPONENTS ( 4 )")]
    public const int D3D12_VIDEO_DECODE_MAX_HISTOGRAM_COMPONENTS = (4);

    [NativeTypeName("#define D3D12_VIDEO_DECODE_MIN_BITSTREAM_OFFSET_ALIGNMENT ( 256 )")]
    public const int D3D12_VIDEO_DECODE_MIN_BITSTREAM_OFFSET_ALIGNMENT = (256);

    [NativeTypeName("#define D3D12_VIDEO_DECODE_MIN_HISTOGRAM_OFFSET_ALIGNMENT ( 256 )")]
    public const int D3D12_VIDEO_DECODE_MIN_HISTOGRAM_OFFSET_ALIGNMENT = (256);

    [NativeTypeName("#define D3D12_VIDEO_DECODE_STATUS_MACROBLOCKS_AFFECTED_UNKNOWN ( 0xffffffff )")]
    public const uint D3D12_VIDEO_DECODE_STATUS_MACROBLOCKS_AFFECTED_UNKNOWN = (0xffffffff);

    [NativeTypeName("#define D3D12_VIDEO_ENCODER_AV1_INVALID_DPB_RESOURCE_INDEX ( 0xff )")]
    public const int D3D12_VIDEO_ENCODER_AV1_INVALID_DPB_RESOURCE_INDEX = (0xff);

    [NativeTypeName("#define D3D12_VIDEO_ENCODER_AV1_MAX_TILE_COLS ( 64 )")]
    public const int D3D12_VIDEO_ENCODER_AV1_MAX_TILE_COLS = (64);

    [NativeTypeName("#define D3D12_VIDEO_ENCODER_AV1_MAX_TILE_ROWS ( 64 )")]
    public const int D3D12_VIDEO_ENCODER_AV1_MAX_TILE_ROWS = (64);

    [NativeTypeName("#define D3D12_VIDEO_ENCODER_AV1_SUPERRES_DENOM_MIN ( 9 )")]
    public const int D3D12_VIDEO_ENCODER_AV1_SUPERRES_DENOM_MIN = (9);

    [NativeTypeName("#define D3D12_VIDEO_ENCODER_AV1_SUPERRES_NUM ( 8 )")]
    public const int D3D12_VIDEO_ENCODER_AV1_SUPERRES_NUM = (8);

    [NativeTypeName("#define D3D12_VIDEO_PROCESS_MAX_FILTERS ( 32 )")]
    public const int D3D12_VIDEO_PROCESS_MAX_FILTERS = (32);

    [NativeTypeName("#define D3D12_VIDEO_PROCESS_STEREO_VIEWS ( 2 )")]
    public const int D3D12_VIDEO_PROCESS_STEREO_VIEWS = (2);

    [NativeTypeName("#define D3D12_VIEWPORT_AND_SCISSORRECT_MAX_INDEX ( 15 )")]
    public const int D3D12_VIEWPORT_AND_SCISSORRECT_MAX_INDEX = (15);

    [NativeTypeName("#define D3D12_VIEWPORT_AND_SCISSORRECT_OBJECT_COUNT_PER_PIPELINE ( 16 )")]
    public const int D3D12_VIEWPORT_AND_SCISSORRECT_OBJECT_COUNT_PER_PIPELINE = (16);

    [NativeTypeName("#define D3D12_VIEWPORT_BOUNDS_MAX ( 32767 )")]
    public const int D3D12_VIEWPORT_BOUNDS_MAX = (32767);

    [NativeTypeName("#define D3D12_VIEWPORT_BOUNDS_MIN ( -32768 )")]
    public const int D3D12_VIEWPORT_BOUNDS_MIN = (-32768);

    [NativeTypeName("#define D3D12_VS_INPUT_REGISTER_COMPONENTS ( 4 )")]
    public const int D3D12_VS_INPUT_REGISTER_COMPONENTS = (4);

    [NativeTypeName("#define D3D12_VS_INPUT_REGISTER_COMPONENT_BIT_COUNT ( 32 )")]
    public const int D3D12_VS_INPUT_REGISTER_COMPONENT_BIT_COUNT = (32);

    [NativeTypeName("#define D3D12_VS_INPUT_REGISTER_COUNT ( 32 )")]
    public const int D3D12_VS_INPUT_REGISTER_COUNT = (32);

    [NativeTypeName("#define D3D12_VS_INPUT_REGISTER_READS_PER_INST ( 2 )")]
    public const int D3D12_VS_INPUT_REGISTER_READS_PER_INST = (2);

    [NativeTypeName("#define D3D12_VS_INPUT_REGISTER_READ_PORTS ( 1 )")]
    public const int D3D12_VS_INPUT_REGISTER_READ_PORTS = (1);

    [NativeTypeName("#define D3D12_VS_OUTPUT_REGISTER_COMPONENTS ( 4 )")]
    public const int D3D12_VS_OUTPUT_REGISTER_COMPONENTS = (4);

    [NativeTypeName("#define D3D12_VS_OUTPUT_REGISTER_COMPONENT_BIT_COUNT ( 32 )")]
    public const int D3D12_VS_OUTPUT_REGISTER_COMPONENT_BIT_COUNT = (32);

    [NativeTypeName("#define D3D12_VS_OUTPUT_REGISTER_COUNT ( 32 )")]
    public const int D3D12_VS_OUTPUT_REGISTER_COUNT = (32);

    [NativeTypeName("#define D3D12_WHQL_CONTEXT_COUNT_FOR_RESOURCE_LIMIT ( 10 )")]
    public const int D3D12_WHQL_CONTEXT_COUNT_FOR_RESOURCE_LIMIT = (10);

    [NativeTypeName("#define D3D12_WHQL_DRAWINDEXED_INDEX_COUNT_2_TO_EXP ( 25 )")]
    public const int D3D12_WHQL_DRAWINDEXED_INDEX_COUNT_2_TO_EXP = (25);

    [NativeTypeName("#define D3D12_WHQL_DRAW_VERTEX_COUNT_2_TO_EXP ( 25 )")]
    public const int D3D12_WHQL_DRAW_VERTEX_COUNT_2_TO_EXP = (25);

    [NativeTypeName("#define D3D12_WORK_GRAPHS_BACKING_MEMORY_ALIGNMENT_IN_BYTES ( 8 )")]
    public const int D3D12_WORK_GRAPHS_BACKING_MEMORY_ALIGNMENT_IN_BYTES = (8);

    [NativeTypeName("#define D3D12_WORK_GRAPHS_DISPATCH_MAX_THREAD_GROUPS_PER_GRID ( 16777215 )")]
    public const int D3D12_WORK_GRAPHS_DISPATCH_MAX_THREAD_GROUPS_PER_GRID = (16777215);

    [NativeTypeName("#define D3D12_WORK_GRAPHS_MAX_NODE_DEPTH ( 32 )")]
    public const int D3D12_WORK_GRAPHS_MAX_NODE_DEPTH = (32);

    [NativeTypeName("#define D3D12_SHADER_COMPONENT_MAPPING_MASK 0x7")]
    public const int D3D12_SHADER_COMPONENT_MAPPING_MASK = 0x7;

    [NativeTypeName("#define D3D12_SHADER_COMPONENT_MAPPING_SHIFT 3")]
    public const int D3D12_SHADER_COMPONENT_MAPPING_SHIFT = 3;

    [NativeTypeName("#define D3D12_SHADER_COMPONENT_MAPPING_ALWAYS_SET_BIT_AVOIDING_ZEROMEM_MISTAKES (1<<(D3D12_SHADER_COMPONENT_MAPPING_SHIFT*4))")]
    public const int D3D12_SHADER_COMPONENT_MAPPING_ALWAYS_SET_BIT_AVOIDING_ZEROMEM_MISTAKES = (1 << (3 * 4));

    [NativeTypeName("#define D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING D3D12_ENCODE_SHADER_4_COMPONENT_MAPPING(0,1,2,3)")]
    public const int D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING = ((((0) & 0x7) | (((1) & 0x7) << 3) | (((2) & 0x7) << (3 * 2)) | (((3) & 0x7) << (3 * 3)) | (1 << (3 * 4))));

    [NativeTypeName("#define D3D12_FILTER_REDUCTION_TYPE_MASK ( 0x3 )")]
    public const int D3D12_FILTER_REDUCTION_TYPE_MASK = (0x3);

    [NativeTypeName("#define D3D12_FILTER_REDUCTION_TYPE_SHIFT ( 7 )")]
    public const int D3D12_FILTER_REDUCTION_TYPE_SHIFT = (7);

    [NativeTypeName("#define D3D12_FILTER_TYPE_MASK ( 0x3 )")]
    public const int D3D12_FILTER_TYPE_MASK = (0x3);

    [NativeTypeName("#define D3D12_MIN_FILTER_SHIFT ( 4 )")]
    public const int D3D12_MIN_FILTER_SHIFT = (4);

    [NativeTypeName("#define D3D12_MAG_FILTER_SHIFT ( 2 )")]
    public const int D3D12_MAG_FILTER_SHIFT = (2);

    [NativeTypeName("#define D3D12_MIP_FILTER_SHIFT ( 0 )")]
    public const int D3D12_MIP_FILTER_SHIFT = (0);

    [NativeTypeName("#define D3D12_ANISOTROPIC_FILTERING_BIT ( 0x40 )")]
    public const int D3D12_ANISOTROPIC_FILTERING_BIT = (0x40);

    [NativeTypeName("#define D3D12_VERSION_PART_MAJOR 3")]
    public const int D3D12_VERSION_PART_MAJOR = 3;

    [NativeTypeName("#define D3D12_VERSION_PART_MINOR 2")]
    public const int D3D12_VERSION_PART_MINOR = 2;

    [NativeTypeName("#define D3D12_VERSION_PART_BUILD 1")]
    public const int D3D12_VERSION_PART_BUILD = 1;

    [NativeTypeName("#define D3D12_VERSION_PART_REVISION 0")]
    public const int D3D12_VERSION_PART_REVISION = 0;

    [NativeTypeName("#define D3D12_INFO_QUEUE_DEFAULT_MESSAGE_COUNT_LIMIT 1024")]
    public const int D3D12_INFO_QUEUE_DEFAULT_MESSAGE_COUNT_LIMIT = 1024;

    [NativeTypeName("#define D3D12_SHADING_RATE_X_AXIS_SHIFT 2")]
    public const int D3D12_SHADING_RATE_X_AXIS_SHIFT = 2;

    [NativeTypeName("#define D3D12_SHADING_RATE_VALID_MASK 3")]
    public const int D3D12_SHADING_RATE_VALID_MASK = 3;

    [NativeTypeName("const GUID")]
    public static readonly Guid WKPDID_D3DAutoDebugObjectNameW = new Guid(0xd4902e36, 0x757a, 0x4942, 0x95, 0x94, 0xb6, 0x76, 0x9a, 0xfa, 0x43, 0xcd);

    [NativeTypeName("const GUID")]
    public static readonly Guid DXGI_DEBUG_D3D12 = new Guid(0xcf59a98c, 0xa950, 0x4326, 0x91, 0xef, 0x9b, 0xba, 0xa1, 0x7b, 0xfd, 0x95);

    [DllImport("d3d12", CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    public static extern HRESULT D3D12SDKLayersGetInterface([NativeTypeName("const IID &")] Guid* rclsid, [NativeTypeName("const IID &")] Guid* riid, void** ppvInterface);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12Debug = new Guid(0x344488b7, 0x6846, 0x474b, 0xb9, 0x89, 0xf0, 0x27, 0x44, 0x82, 0x45, 0xe0);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12Debug1 = new Guid(0xaffaa4ca, 0x63fe, 0x4d8e, 0xb8, 0xad, 0x15, 0x90, 0x00, 0xaf, 0x43, 0x04);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12Debug2 = new Guid(0x93a665c4, 0xa3b2, 0x4e5d, 0xb6, 0x92, 0xa2, 0x6a, 0xe1, 0x4e, 0x33, 0x74);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12Debug3 = new Guid(0x5cf4e58f, 0xf671, 0x4ff1, 0xa5, 0x42, 0x36, 0x86, 0xe3, 0xd1, 0x53, 0xd1);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12Debug4 = new Guid(0x014b816e, 0x9ec5, 0x4a2f, 0xa8, 0x45, 0xff, 0xbe, 0x44, 0x1c, 0xe1, 0x3a);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12Debug5 = new Guid(0x548d6b12, 0x09fa, 0x40e0, 0x90, 0x69, 0x5d, 0xcd, 0x58, 0x9a, 0x52, 0xc9);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12Debug6 = new Guid(0x82a816d6, 0x5d01, 0x4157, 0x97, 0xd0, 0x49, 0x75, 0x46, 0x3f, 0xd1, 0xed);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12Debug7 = new Guid(0x31d409f9, 0x22b4, 0x409d, 0x9c, 0x44, 0xf7, 0x2d, 0xa8, 0xa2, 0xba, 0x61);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12DebugDevice1 = new Guid(0xa9b71770, 0xd099, 0x4a65, 0xa6, 0x98, 0x3d, 0xee, 0x10, 0x02, 0x0f, 0x88);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12DebugDevice = new Guid(0x3febd6dd, 0x4973, 0x4787, 0x81, 0x94, 0xe4, 0x5f, 0x9e, 0x28, 0x92, 0x3e);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12DebugDevice2 = new Guid(0x60eccbc1, 0x378d, 0x4df1, 0x89, 0x4c, 0xf8, 0xac, 0x5c, 0xe4, 0xd7, 0xdd);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12DebugCommandQueue = new Guid(0x09e0bf36, 0x54ac, 0x484f, 0x88, 0x47, 0x4b, 0xae, 0xea, 0xb6, 0x05, 0x3a);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12DebugCommandQueue1 = new Guid(0x16be35a2, 0xbfd6, 0x49f2, 0xbc, 0xae, 0xea, 0xae, 0x4a, 0xff, 0x86, 0x2d);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12DebugCommandList1 = new Guid(0x102ca951, 0x311b, 0x4b01, 0xb1, 0x1f, 0xec, 0xb8, 0x3e, 0x06, 0x1b, 0x37);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12DebugCommandList = new Guid(0x09e0bf36, 0x54ac, 0x484f, 0x88, 0x47, 0x4b, 0xae, 0xea, 0xb6, 0x05, 0x3f);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12DebugCommandList2 = new Guid(0xaeb575cf, 0x4e06, 0x48be, 0xba, 0x3b, 0xc4, 0x50, 0xfc, 0x96, 0x65, 0x2e);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12DebugCommandList3 = new Guid(0x197d5e15, 0x4d37, 0x4d34, 0xaf, 0x78, 0x72, 0x4c, 0xd7, 0x0f, 0xdb, 0x1f);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12SharingContract = new Guid(0x0adf7d52, 0x929c, 0x4e61, 0xad, 0xdb, 0xff, 0xed, 0x30, 0xde, 0x66, 0xef);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12ManualWriteTrackingResource = new Guid(0x86ca3b85, 0x49ad, 0x4b6e, 0xae, 0xd5, 0xed, 0xdb, 0x18, 0x54, 0x0f, 0x41);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12InfoQueue = new Guid(0x0742a90b, 0xc387, 0x483f, 0xb9, 0x46, 0x30, 0xa7, 0xe4, 0xe6, 0x14, 0x58);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12InfoQueue1 = new Guid(0x2852dd88, 0xb484, 0x4c0c, 0xb6, 0xb1, 0x67, 0x16, 0x85, 0x00, 0xe6, 0x00);

    [DllImport("d3d12", CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    public static extern HRESULT D3D12CreateDevice(IUnknown* pAdapter, D3D_FEATURE_LEVEL MinimumFeatureLevel, [NativeTypeName("const IID &")] Guid* riid, void** ppDevice);

    [DllImport("d3d12", CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    public static extern HRESULT D3D12GetDebugInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvDebug);

    [DllImport("d3d12", CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    public static extern HRESULT D3D12EnableExperimentalFeatures(uint NumFeatures, [NativeTypeName("const IID *")] Guid* pIIDs, void* pConfigurationStructs, uint* pConfigurationStructSizes);

    [NativeTypeName("const UUID")]
    public static readonly Guid D3D12ExperimentalShaderModels = new Guid(0x76f5573e, 0xf13a, 0x40f5, 0xb2, 0x97, 0x81, 0xce, 0x9e, 0x18, 0x93, 0x3f);

    [NativeTypeName("const UUID")]
    public static readonly Guid D3D12GPUUploadHeapsOnUnsupportedOS = new Guid(0x45dc51f3, 0x767f, 0x4588, 0xb2, 0x06, 0x0b, 0xaa, 0x2b, 0x16, 0xfb, 0xae);

    [NativeTypeName("const GUID")]
    public static readonly Guid CLSID_D3D12Debug = new Guid(0xf2352aeb, 0xdd84, 0x49fe, 0xb9, 0x7b, 0xa9, 0xdc, 0xfd, 0xcc, 0x1b, 0x4f);

    [NativeTypeName("const GUID")]
    public static readonly Guid CLSID_D3D12Tools = new Guid(0xe38216b1, 0x3c8c, 0x4833, 0xaa, 0x09, 0x0a, 0x06, 0xb6, 0x5d, 0x96, 0xc8);

    [NativeTypeName("const GUID")]
    public static readonly Guid CLSID_D3D12DeviceRemovedExtendedData = new Guid(0x4a75bbc4, 0x9ff4, 0x4ad8, 0x9f, 0x18, 0xab, 0xae, 0x84, 0xdc, 0x5f, 0xf2);

    [NativeTypeName("const GUID")]
    public static readonly Guid CLSID_D3D12SDKConfiguration = new Guid(0x7cda6aca, 0xa03e, 0x49c8, 0x94, 0x58, 0x03, 0x34, 0xd2, 0x0e, 0x07, 0xce);

    [NativeTypeName("const GUID")]
    public static readonly Guid CLSID_D3D12DeviceFactory = new Guid(0x114863bf, 0xc386, 0x4aee, 0xb3, 0x9d, 0x8f, 0x0b, 0xbb, 0x06, 0x29, 0x55);

    [NativeTypeName("const GUID")]
    public static readonly Guid CLSID_D3D12DSRDeviceFactory = new Guid(0xbb6dd27e, 0x94a9, 0x41a6, 0x9f, 0x1b, 0x13, 0x37, 0x72, 0x17, 0x24, 0x28);

    [NativeTypeName("const GUID")]
    public static readonly Guid CLSID_D3D12StateObjectFactory = new Guid(0x54e1c9f3, 0x1303, 0x4112, 0xbf, 0x8e, 0x7b, 0xf2, 0xbb, 0x60, 0x6a, 0x73);

    [NativeTypeName("const GUID")]
    public static readonly Guid CLSID_D3D12RuntimeValidationControl = new Guid(0xe5b53e74, 0x3fca, 0x47b4, 0x88, 0xb9, 0xa8, 0xb4, 0x1e, 0xf8, 0xfb, 0x73);

    [NativeTypeName("const GUID")]
    public static readonly Guid CLSID_D3D12ApplicationIdentity = new Guid(0x08d8e1e8, 0x75a6, 0x42a7, 0xbf, 0x3a, 0xd0, 0x5f, 0xe5, 0x29, 0xc4, 0x7c);

    [NativeTypeName("const GUID")]
    public static readonly Guid CLSID_D3D12AsyncCommandsFallback = new Guid(0xef4992f0, 0x0fae, 0x424e, 0xb4, 0x84, 0x7b, 0xcd, 0x8e, 0x56, 0x9e, 0x3e);

    [NativeTypeName("const GUID")]
    public static readonly Guid CLSID_D3D12InfoQueue = new Guid(0x4a87fbe2, 0x9e5b, 0x4c5d, 0xa8, 0x21, 0x8e, 0x06, 0xf8, 0x33, 0x71, 0x9a);

    [NativeTypeName("const GUID")]
    public static readonly Guid CLSID_D3D12StateObjectDatabaseCapture = new Guid(0x2b6c3e2f, 0xeb8e, 0x4db9, 0x8c, 0xf4, 0x1e, 0x04, 0x7e, 0x67, 0x1b, 0x7f);

    [DllImport("d3d12", CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    public static extern HRESULT D3D12GetInterface([NativeTypeName("const IID &")] Guid* rclsid, [NativeTypeName("const IID &")] Guid* riid, void** ppvDebug);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12Object = new Guid(0xc4fec28f, 0x7966, 0x4e95, 0x9f, 0x94, 0xf4, 0x31, 0xcb, 0x56, 0xc3, 0xb8);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12DeviceChild = new Guid(0x905db94b, 0xa00c, 0x4140, 0x9d, 0xf5, 0x2b, 0x64, 0xca, 0x9e, 0xa3, 0x57);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12RootSignature = new Guid(0xc54a6b66, 0x72df, 0x4ee8, 0x8b, 0xe5, 0xa9, 0x46, 0xa1, 0x42, 0x92, 0x14);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12RootSignature1 = new Guid(0xc390bd7d, 0x9142, 0x4a95, 0xb0, 0x72, 0x6d, 0x34, 0x39, 0xad, 0xe5, 0xc4);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12RootSignatureDeserializer = new Guid(0x34AB647B, 0x3CC8, 0x46AC, 0x84, 0x1B, 0xC0, 0x96, 0x56, 0x45, 0xC0, 0x46);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12VersionedRootSignatureDeserializer = new Guid(0x7F91CE67, 0x090C, 0x4BB7, 0xB7, 0x8E, 0xED, 0x8F, 0xF2, 0xE3, 0x1D, 0xA0);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12Pageable = new Guid(0x63ee58fb, 0x1268, 0x4835, 0x86, 0xda, 0xf0, 0x08, 0xce, 0x62, 0xf0, 0xd6);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12Heap = new Guid(0x6b3b2502, 0x6e51, 0x45b3, 0x90, 0xee, 0x98, 0x84, 0x26, 0x5e, 0x8d, 0xf3);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12Resource = new Guid(0x696442be, 0xa72e, 0x4059, 0xbc, 0x79, 0x5b, 0x5c, 0x98, 0x04, 0x0f, 0xad);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12CommandAllocator = new Guid(0x6102dee4, 0xaf59, 0x4b09, 0xb9, 0x99, 0xb4, 0x4d, 0x73, 0xf0, 0x9b, 0x24);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12Fence = new Guid(0x0a753dcf, 0xc4d8, 0x4b91, 0xad, 0xf6, 0xbe, 0x5a, 0x60, 0xd9, 0x5a, 0x76);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12Fence1 = new Guid(0x433685fe, 0xe22b, 0x4ca0, 0xa8, 0xdb, 0xb5, 0xb4, 0xf4, 0xdd, 0x0e, 0x4a);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12PipelineState = new Guid(0x765a30f3, 0xf624, 0x4c6f, 0xa8, 0x28, 0xac, 0xe9, 0x48, 0x62, 0x24, 0x45);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12PipelineState1 = new Guid(0x5646804c, 0x9638, 0x48f7, 0x91, 0x82, 0xb3, 0xee, 0x5a, 0x6b, 0x60, 0xfb);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12DescriptorHeap = new Guid(0x8efb471d, 0x616c, 0x4f49, 0x90, 0xf7, 0x12, 0x7b, 0xb7, 0x63, 0xfa, 0x51);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12QueryHeap = new Guid(0x0d9658ae, 0xed45, 0x469e, 0xa6, 0x1d, 0x97, 0x0e, 0xc5, 0x83, 0xca, 0xb4);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12CommandSignature = new Guid(0xc36a797c, 0xec80, 0x4f0a, 0x89, 0x85, 0xa7, 0xb2, 0x47, 0x50, 0x82, 0xd1);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12CommandList = new Guid(0x7116d91c, 0xe7e4, 0x47ce, 0xb8, 0xc6, 0xec, 0x81, 0x68, 0xf4, 0x37, 0xe5);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12GraphicsCommandList = new Guid(0x5b160d0f, 0xac1b, 0x4185, 0x8b, 0xa8, 0xb3, 0xae, 0x42, 0xa5, 0xa4, 0x55);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12GraphicsCommandList1 = new Guid(0x553103fb, 0x1fe7, 0x4557, 0xbb, 0x38, 0x94, 0x6d, 0x7d, 0x0e, 0x7c, 0xa7);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12GraphicsCommandList2 = new Guid(0x38C3E585, 0xFF17, 0x412C, 0x91, 0x50, 0x4F, 0xC6, 0xF9, 0xD7, 0x2A, 0x28);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12CommandQueue = new Guid(0x0ec870a6, 0x5d7e, 0x4c22, 0x8c, 0xfc, 0x5b, 0xaa, 0xe0, 0x76, 0x16, 0xed);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12CommandQueue1 = new Guid(0x3a3c3165, 0x0ee7, 0x4b8e, 0xa0, 0xaf, 0x63, 0x56, 0xb4, 0xc3, 0xbb, 0xb9);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12Device = new Guid(0x189819f1, 0x1db6, 0x4b57, 0xbe, 0x54, 0x18, 0x21, 0x33, 0x9b, 0x85, 0xf7);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12PipelineLibrary = new Guid(0xc64226a8, 0x9201, 0x46af, 0xb4, 0xcc, 0x53, 0xfb, 0x9f, 0xf7, 0x41, 0x4f);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12PipelineLibrary1 = new Guid(0x80eabf42, 0x2568, 0x4e5e, 0xbd, 0x82, 0xc3, 0x7f, 0x86, 0x96, 0x1d, 0xc3);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12Device1 = new Guid(0x77acce80, 0x638e, 0x4e65, 0x88, 0x95, 0xc1, 0xf2, 0x33, 0x86, 0x86, 0x3e);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12Device2 = new Guid(0x30baa41e, 0xb15b, 0x475c, 0xa0, 0xbb, 0x1a, 0xf5, 0xc5, 0xb6, 0x43, 0x28);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12Device3 = new Guid(0x81dadc15, 0x2bad, 0x4392, 0x93, 0xc5, 0x10, 0x13, 0x45, 0xc4, 0xaa, 0x98);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12ProtectedSession = new Guid(0xA1533D18, 0x0AC1, 0x4084, 0x85, 0xB9, 0x89, 0xA9, 0x61, 0x16, 0x80, 0x6B);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12ProtectedResourceSession = new Guid(0x6CD696F4, 0xF289, 0x40CC, 0x80, 0x91, 0x5A, 0x6C, 0x0A, 0x09, 0x9C, 0x3D);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12Device4 = new Guid(0xe865df17, 0xa9ee, 0x46f9, 0xa4, 0x63, 0x30, 0x98, 0x31, 0x5a, 0xa2, 0xe5);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12LifetimeOwner = new Guid(0xe667af9f, 0xcd56, 0x4f46, 0x83, 0xce, 0x03, 0x2e, 0x59, 0x5d, 0x70, 0xa8);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12SwapChainAssistant = new Guid(0xf1df64b6, 0x57fd, 0x49cd, 0x88, 0x07, 0xc0, 0xeb, 0x88, 0xb4, 0x5c, 0x8f);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12LifetimeTracker = new Guid(0x3fd03d36, 0x4eb1, 0x424a, 0xa5, 0x82, 0x49, 0x4e, 0xcb, 0x8b, 0xa8, 0x13);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12StateObject = new Guid(0x47016943, 0xfca8, 0x4594, 0x93, 0xea, 0xaf, 0x25, 0x8b, 0x55, 0x34, 0x6d);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12StateObjectProperties = new Guid(0xde5fa827, 0x9bf9, 0x4f26, 0x89, 0xff, 0xd7, 0xf5, 0x6f, 0xde, 0x38, 0x60);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12StateObjectProperties1 = new Guid(0x460caac7, 0x1d24, 0x446a, 0xa1, 0x84, 0xca, 0x67, 0xdb, 0x49, 0x41, 0x38);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12StateObjectProperties2 = new Guid(0xd5e82917, 0xf0f1, 0x44cf, 0xae, 0x5e, 0xce, 0x22, 0x2d, 0xd0, 0xb8, 0x84);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12WorkGraphProperties = new Guid(0x065acf71, 0xf863, 0x4b89, 0x82, 0xf4, 0x02, 0xe4, 0xd5, 0x88, 0x67, 0x57);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12Device5 = new Guid(0x8b4f173b, 0x2fea, 0x4b80, 0x8f, 0x58, 0x43, 0x07, 0x19, 0x1a, 0xb9, 0x5d);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12DeviceRemovedExtendedDataSettings = new Guid(0x82BC481C, 0x6B9B, 0x4030, 0xAE, 0xDB, 0x7E, 0xE3, 0xD1, 0xDF, 0x1E, 0x63);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12DeviceRemovedExtendedDataSettings1 = new Guid(0xDBD5AE51, 0x3317, 0x4F0A, 0xAD, 0xF9, 0x1D, 0x7C, 0xED, 0xCA, 0xAE, 0x0B);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12DeviceRemovedExtendedDataSettings2 = new Guid(0x61552388, 0x01ab, 0x4008, 0xa4, 0x36, 0x83, 0xdb, 0x18, 0x95, 0x66, 0xea);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12DeviceRemovedExtendedData = new Guid(0x98931D33, 0x5AE8, 0x4791, 0xAA, 0x3C, 0x1A, 0x73, 0xA2, 0x93, 0x4E, 0x71);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12DeviceRemovedExtendedData1 = new Guid(0x9727A022, 0xCF1D, 0x4DDA, 0x9E, 0xBA, 0xEF, 0xFA, 0x65, 0x3F, 0xC5, 0x06);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12DeviceRemovedExtendedData2 = new Guid(0x67FC5816, 0xE4CA, 0x4915, 0xBF, 0x18, 0x42, 0x54, 0x12, 0x72, 0xDA, 0x54);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12Device6 = new Guid(0xc70b221b, 0x40e4, 0x4a17, 0x89, 0xaf, 0x02, 0x5a, 0x07, 0x27, 0xa6, 0xdc);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12ProtectedResourceSession1 = new Guid(0xD6F12DD6, 0x76FB, 0x406E, 0x89, 0x61, 0x42, 0x96, 0xEE, 0xFC, 0x04, 0x09);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12Device7 = new Guid(0x5c014b53, 0x68a1, 0x4b9b, 0x8b, 0xd1, 0xdd, 0x60, 0x46, 0xb9, 0x35, 0x8b);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12Device8 = new Guid(0x9218E6BB, 0xF944, 0x4F7E, 0xA7, 0x5C, 0xB1, 0xB2, 0xC7, 0xB7, 0x01, 0xF3);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12Resource1 = new Guid(0x9D5E227A, 0x4430, 0x4161, 0x88, 0xB3, 0x3E, 0xCA, 0x6B, 0xB1, 0x6E, 0x19);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12Resource2 = new Guid(0xBE36EC3B, 0xEA85, 0x4AEB, 0xA4, 0x5A, 0xE9, 0xD7, 0x64, 0x04, 0xA4, 0x95);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12Resource3 = new Guid(0xb5e58a68, 0x46f3, 0x4ece, 0xa2, 0x78, 0x22, 0x46, 0xc1, 0xeb, 0x40, 0x93);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12Heap1 = new Guid(0x572F7389, 0x2168, 0x49E3, 0x96, 0x93, 0xD6, 0xDF, 0x58, 0x71, 0xBF, 0x6D);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12GraphicsCommandList3 = new Guid(0x6FDA83A7, 0xB84C, 0x4E38, 0x9A, 0xC8, 0xC7, 0xBD, 0x22, 0x01, 0x6B, 0x3D);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12MetaCommand = new Guid(0xDBB84C27, 0x36CE, 0x4FC9, 0xB8, 0x01, 0xF0, 0x48, 0xC4, 0x6A, 0xC5, 0x70);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12GraphicsCommandList4 = new Guid(0x8754318e, 0xd3a9, 0x4541, 0x98, 0xcf, 0x64, 0x5b, 0x50, 0xdc, 0x48, 0x74);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12ShaderCacheSession = new Guid(0x28e2495d, 0x0f64, 0x4ae4, 0xa6, 0xec, 0x12, 0x92, 0x55, 0xdc, 0x49, 0xa8);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12Device9 = new Guid(0x4c80e962, 0xf032, 0x4f60, 0xbc, 0x9e, 0xeb, 0xc2, 0xcf, 0xa1, 0xd8, 0x3c);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12Device10 = new Guid(0x517f8718, 0xaa66, 0x49f9, 0xb0, 0x2b, 0xa7, 0xab, 0x89, 0xc0, 0x60, 0x31);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12Device11 = new Guid(0x5405c344, 0xd457, 0x444e, 0xb4, 0xdd, 0x23, 0x66, 0xe4, 0x5a, 0xee, 0x39);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12Device12 = new Guid(0x5af5c532, 0x4c91, 0x4cd0, 0xb5, 0x41, 0x15, 0xa4, 0x05, 0x39, 0x5f, 0xc5);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12Device13 = new Guid(0x14eecffc, 0x4df8, 0x40f7, 0xa1, 0x18, 0x5c, 0x81, 0x6f, 0x45, 0x69, 0x5e);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12Device14 = new Guid(0x5f6e592d, 0xd895, 0x44c2, 0x8e, 0x4a, 0x88, 0xad, 0x49, 0x26, 0xd3, 0x23);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12Device15 = new Guid(0x76cff76f, 0x1e9b, 0x4450, 0x8c, 0xdc, 0x34, 0xf1, 0xaf, 0x78, 0x8e, 0x5b);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12StateObjectDatabase = new Guid(0xc56060b7, 0xb5fc, 0x4135, 0x98, 0xe0, 0xa1, 0xe9, 0x99, 0x7e, 0xac, 0xe0);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12Device16 = new Guid(0xb34a50dd, 0xe102, 0x428f, 0x82, 0xa0, 0x82, 0x29, 0x4a, 0x27, 0xbb, 0xe6);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12ResourceOpaqueTextureLayout = new Guid(0xa7c3f1e8, 0x2b94, 0x4d06, 0x9f, 0x5a, 0xe8, 0xd1, 0xc4, 0x0b, 0x72, 0x63);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12ShaderCacheSession1 = new Guid(0x1c5789ff, 0x9203, 0x490f, 0xb9, 0x71, 0x98, 0x4a, 0xcf, 0x1e, 0xb5, 0xfe);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12VirtualizationGuestDevice = new Guid(0xbc66d368, 0x7373, 0x4943, 0x87, 0x57, 0xfc, 0x87, 0xdc, 0x79, 0xe4, 0x76);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12Tools = new Guid(0x7071e1f0, 0xe84b, 0x4b33, 0x97, 0x4f, 0x12, 0xfa, 0x49, 0xde, 0x65, 0xc5);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12Tools1 = new Guid(0xe4fbc019, 0xdd3c, 0x43e1, 0x8f, 0x32, 0x7f, 0x64, 0x95, 0x75, 0xf0, 0xa0);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12Tools2 = new Guid(0x01d393c5, 0xc9b0, 0x42a1, 0x95, 0x8c, 0xc2, 0x6b, 0x02, 0xd4, 0xd0, 0x97);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12RuntimeValidationControl = new Guid(0xc706c811, 0x3663, 0x4bf1, 0x91, 0xb9, 0x1e, 0x8a, 0x7c, 0x11, 0x4a, 0xb9);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12PageableTools = new Guid(0x8f1359db, 0xd8d1, 0x42f9, 0xb5, 0xcf, 0x79, 0xf4, 0xcb, 0xad, 0x0d, 0x3d);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12DeviceTools = new Guid(0x2ea68e9c, 0x19c3, 0x4e47, 0xa1, 0x09, 0x6c, 0xda, 0xdf, 0xf0, 0xac, 0xa9);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12DeviceTools1 = new Guid(0xe30e9fc7, 0xe641, 0x4d6e, 0x8a, 0x81, 0x9d, 0xd9, 0x20, 0x6e, 0xc4, 0x7a);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12DeviceTools2 = new Guid(0x0edb4324, 0x20a7, 0x45ed, 0x80, 0x16, 0xc3, 0x17, 0xa0, 0x73, 0xbe, 0x33);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12SDKConfiguration = new Guid(0xe9eb5314, 0x33aa, 0x42b2, 0xa7, 0x18, 0xd7, 0x7f, 0x58, 0xb1, 0xf1, 0xc7);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12SDKConfiguration1 = new Guid(0x8aaf9303, 0xad25, 0x48b9, 0x9a, 0x57, 0xd9, 0xc3, 0x7e, 0x00, 0x9d, 0x9f);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12DeviceFactory = new Guid(0x61f307d3, 0xd34e, 0x4e7c, 0x83, 0x74, 0x3b, 0xa4, 0xde, 0x23, 0xcc, 0xcb);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12DeviceConfiguration = new Guid(0x78dbf87b, 0xf766, 0x422b, 0xa6, 0x1c, 0xc8, 0xc4, 0x46, 0xbd, 0xb9, 0xad);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12DeviceConfiguration1 = new Guid(0xed342442, 0x6343, 0x4e16, 0xbb, 0x82, 0xa3, 0xa5, 0x77, 0x87, 0x4e, 0x56);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12StateObjectDatabaseFactory = new Guid(0xf5b066f0, 0x648a, 0x4611, 0xbd, 0x41, 0x27, 0xfd, 0x09, 0x48, 0xb9, 0xeb);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12StateObjectDatabase1 = new Guid(0xc700e43f, 0x47a6, 0x4ec6, 0xad, 0x83, 0xf2, 0x75, 0x40, 0x02, 0x31, 0xaf);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12StateObjectDatabaseFactory1 = new Guid(0x68a6585d, 0x0ac7, 0x4fa4, 0xba, 0x33, 0xe3, 0xbd, 0x44, 0x2f, 0x28, 0x33);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12ApplicationIdentity = new Guid(0x82dc6c85, 0x727b, 0x4a8d, 0x91, 0x69, 0xdb, 0x6c, 0xe3, 0xe9, 0x75, 0xa0);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12AsyncCommandsFallback = new Guid(0xfbddf9e8, 0x33a3, 0x488c, 0x97, 0x75, 0x1a, 0x97, 0x0d, 0xc5, 0x48, 0x62);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12StateObjectDatabaseCapture = new Guid(0xd67447bc, 0x4d6c, 0x4ca5, 0xa2, 0xa8, 0xde, 0xee, 0xa4, 0xc1, 0x45, 0xc4);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12GraphicsCommandList5 = new Guid(0x55050859, 0x4024, 0x474c, 0x87, 0xf5, 0x64, 0x72, 0xea, 0xee, 0x44, 0xea);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12GraphicsCommandList6 = new Guid(0xc3827890, 0xe548, 0x4cfa, 0x96, 0xcf, 0x56, 0x89, 0xa9, 0x37, 0x0f, 0x80);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12GraphicsCommandList7 = new Guid(0xdd171223, 0x8b61, 0x4769, 0x90, 0xe3, 0x16, 0x0c, 0xcd, 0xe4, 0xe2, 0xc1);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12GraphicsCommandList8 = new Guid(0xee936ef9, 0x599d, 0x4d28, 0x93, 0x8e, 0x23, 0xc4, 0xad, 0x05, 0xce, 0x51);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12GraphicsCommandList9 = new Guid(0x34ed2808, 0xffe6, 0x4c2b, 0xb1, 0x1a, 0xca, 0xbd, 0x2b, 0x0c, 0x59, 0xe1);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12GraphicsCommandList10 = new Guid(0x7013c015, 0xd161, 0x4b63, 0xa0, 0x8c, 0x23, 0x85, 0x52, 0xdd, 0x8a, 0xcc);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12GraphicsCommandList11 = new Guid(0x823aee5b, 0xc932, 0x4b15, 0xb4, 0xc4, 0x4a, 0x28, 0xe7, 0xc3, 0x4d, 0xea);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12DeviceDumpFile = new Guid(0xdb82bfed, 0x784d, 0x40ea, 0xae, 0xbf, 0x08, 0xbc, 0xd7, 0xf0, 0xa9, 0xe4);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12GraphicsCommandList12 = new Guid(0x9524ea5e, 0xc1b0, 0x4fe5, 0xa2, 0x03, 0xa7, 0x5a, 0xbc, 0x5d, 0x8e, 0x3a);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12DSRDeviceFactory = new Guid(0xf343d1a0, 0xafe3, 0x439f, 0xb1, 0x3d, 0xcd, 0x87, 0xa4, 0x3b, 0x70, 0xca);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12GBVDiagnostics = new Guid(0x597985ab, 0x9b75, 0x4dbb, 0xbe, 0x23, 0x07, 0x61, 0x19, 0x5b, 0xeb, 0xee);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_ID3D12DeviceStatistics = new Guid(0x3d5ca1a8, 0xa39e, 0x4619, 0x95, 0xe0, 0xf9, 0xb0, 0xa4, 0x03, 0x40, 0xf5);

    public static readonly Guid IID_ID3DDestructionNotifier = new Guid(0xA06EB39A, 0x50DA, 0x425B, 0x8C, 0x31, 0x4E, 0xEC, 0xD6, 0xC2, 0x70, 0xF3);
}
