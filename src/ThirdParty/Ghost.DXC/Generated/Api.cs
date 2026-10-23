using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.DXC;

public static unsafe partial class Api
{
    /// <include file='Api.xml' path='doc/member[@name="Api.DxcCreateInstance"]/*' />
    [DllImport("dxcompiler", CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    [return: NativeTypeName("HRESULT")]
    public static extern int DxcCreateInstance([NativeTypeName("const IID &")] Guid* rclsid, [NativeTypeName("const IID &")] Guid* riid, [NativeTypeName("LPVOID *")] void** ppv);

    /// <include file='Api.xml' path='doc/member[@name="Api.DxcCreateInstance2"]/*' />
    [DllImport("dxcompiler", CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    [return: NativeTypeName("HRESULT")]
    public static extern int DxcCreateInstance2(IMalloc* pMalloc, [NativeTypeName("const IID &")] Guid* rclsid, [NativeTypeName("const IID &")] Guid* riid, [NativeTypeName("LPVOID *")] void** ppv);

    [NativeTypeName("const UINT32")]
    public const uint DxcValidatorFlags_Default = 0;

    [NativeTypeName("const UINT32")]
    public const uint DxcValidatorFlags_InPlaceEdit = 1;

    [NativeTypeName("const UINT32")]
    public const uint DxcValidatorFlags_RootSignatureOnly = 2;

    [NativeTypeName("const UINT32")]
    public const uint DxcValidatorFlags_ModuleOnly = 4;

    [NativeTypeName("const UINT32")]
    public const uint DxcValidatorFlags_ValidMask = 0x7;

    [NativeTypeName("const UINT32")]
    public const uint DxcVersionInfoFlags_None = 0;

    [NativeTypeName("const UINT32")]
    public const uint DxcVersionInfoFlags_Debug = 1;

    [NativeTypeName("const UINT32")]
    public const uint DxcVersionInfoFlags_Internal = 2;

    [NativeTypeName("const CLSID")]
    public static ref readonly Guid CLSID_DxcCompiler
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x93, 0x2D, 0xE2, 0x73,
                0xCE, 0xE6,
                0xF3, 0x47,
                0xB5,
                0xBF,
                0xF0,
                0x66,
                0x4F,
                0x39,
                0xC1,
                0xB0
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    [NativeTypeName("const GUID")]
    public static ref readonly Guid CLSID_DxcLinker
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x87, 0x80, 0x6A, 0xEF,
                0xEA, 0xB0,
                0x56, 0x4D,
                0x9E,
                0x45,
                0xD0,
                0x7E,
                0x1A,
                0x8B,
                0x78,
                0x06
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    [NativeTypeName("const CLSID")]
    public static ref readonly Guid CLSID_DxcDiaDataSource
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x73, 0x6B, 0x1F, 0xCD,
                0xB0, 0x2A,
                0x4D, 0x48,
                0x8E,
                0xDC,
                0xEB,
                0xE7,
                0xA4,
                0x3C,
                0xA0,
                0x9F
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    [NativeTypeName("const CLSID")]
    public static ref readonly Guid CLSID_DxcCompilerArgs
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x82, 0xAE, 0x56, 0x3E,
                0x4D, 0x22,
                0x0F, 0x47,
                0xA1,
                0xA1,
                0xFE,
                0x30,
                0x16,
                0xEE,
                0x9F,
                0x9D
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    [NativeTypeName("const GUID")]
    public static ref readonly Guid CLSID_DxcLibrary
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0xAF, 0xD6, 0x45, 0x62,
                0xE0, 0x66,
                0xFD, 0x48,
                0x80,
                0xB4,
                0x4D,
                0x27,
                0x17,
                0x96,
                0x74,
                0x8C
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    [NativeTypeName("const GUID")]
    public static ref readonly Guid CLSID_DxcUtils => ref CLSID_DxcLibrary;

    [NativeTypeName("const GUID")]
    public static ref readonly Guid CLSID_DxcValidator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x15, 0xE2, 0xA3, 0x8C,
                0x28, 0xF7,
                0xF3, 0x4C,
                0x8C,
                0xDD,
                0x88,
                0xAF,
                0x91,
                0x75,
                0x87,
                0xA1
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    [NativeTypeName("const GUID")]
    public static ref readonly Guid CLSID_DxcAssembler
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x68, 0xDB, 0x28, 0xD7,
                0x03, 0xF9,
                0x80, 0x4F,
                0x94,
                0xCD,
                0xDC,
                0xCF,
                0x76,
                0xEC,
                0x71,
                0x51
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    [NativeTypeName("const GUID")]
    public static ref readonly Guid CLSID_DxcContainerReflection
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x89, 0x44, 0xF5, 0xB9,
                0xB8, 0x55,
                0x0C, 0x40,
                0xBA,
                0x3A,
                0x16,
                0x75,
                0xE4,
                0x72,
                0x8B,
                0x91
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    [NativeTypeName("const GUID")]
    public static ref readonly Guid CLSID_DxcOptimizer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x9F, 0xD7, 0x2C, 0xAE,
                0x22, 0xCC,
                0x3F, 0x45,
                0x9B,
                0x6B,
                0xB1,
                0x24,
                0xE7,
                0xA5,
                0x20,
                0x4C
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    [NativeTypeName("const GUID")]
    public static ref readonly Guid CLSID_DxcContainerBuilder
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x94, 0x42, 0x13, 0x94,
                0x1F, 0x41,
                0x74, 0x45,
                0xB4,
                0xD0,
                0x87,
                0x41,
                0xE2,
                0x52,
                0x40,
                0xD2
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    [NativeTypeName("const GUID")]
    public static ref readonly Guid CLSID_DxcPdbUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0xFB, 0x1D, 0x62, 0x54,
                0xCE, 0xF2,
                0x7E, 0x45,
                0xAE,
                0x8C,
                0xEC,
                0x35,
                0x5F,
                0xAE,
                0xEC,
                0x7C
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    [NativeTypeName("const DWORD")]
    public const uint EXCEPTION_LOAD_LIBRARY_FAILED = (0xc0000000U | (38 << 16) | (0xff00U | (0x00U & 0xffU)));

    [NativeTypeName("const DWORD")]
    public const uint EXCEPTION_NO_HMODULE = (0xc0000000U | (38 << 16) | (0xff00U | (0x01U & 0xffU)));

    [NativeTypeName("const DWORD")]
    public const uint EXCEPTION_GET_PROC_FAILED = (0xc0000000U | (38 << 16) | (0xff00U | (0x02U & 0xffU)));

    [NativeTypeName("const CLSID")]
    public static ref readonly Guid CLSID_DxcIntelliSense
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x3C, 0x83, 0x47, 0x30,
                0xC0, 0xD1,
                0x8E, 0x4B,
                0x9D,
                0x40,
                0x10,
                0x28,
                0x78,
                0x60,
                0x59,
                0x85
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    [NativeTypeName("const CLSID")]
    public static ref readonly Guid CLSID_DxcPixDxilDebugger
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x22, 0xB6, 0x12, 0xA7,
                0xF7, 0x5A,
                0x77, 0x4C,
                0xA9,
                0x65,
                0xC8,
                0x3A,
                0xC1,
                0xA5,
                0xD8,
                0xBC
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    [NativeTypeName("#define DXC_SEVERITY_ERROR 1")]
    public const int DXC_SEVERITY_ERROR = 1;

    [NativeTypeName("#define FACILITY_ERRNO (0x96)")]
    public const int FACILITY_ERRNO = (0x96);

    [NativeTypeName("#define FACILITY_DXC (0xAA)")]
    public const int FACILITY_DXC = (0xAA);

    [NativeTypeName("#define DXC_S_OK 0")]
    public const int DXC_S_OK = 0;

    [NativeTypeName("#define DXC_E_OVERLAPPING_SEMANTICS DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x0001))")]
    public const int DXC_E_OVERLAPPING_SEMANTICS = unchecked((int)(((uint)(1) << 31) | ((uint)((0xAA)) << 16) | (0x0001)));

    [NativeTypeName("#define DXC_E_MULTIPLE_DEPTH_SEMANTICS DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x0002))")]
    public const int DXC_E_MULTIPLE_DEPTH_SEMANTICS = unchecked((int)(((uint)(1) << 31) | ((uint)((0xAA)) << 16) | (0x0002)));

    [NativeTypeName("#define DXC_E_INPUT_FILE_TOO_LARGE DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x0003))")]
    public const int DXC_E_INPUT_FILE_TOO_LARGE = unchecked((int)(((uint)(1) << 31) | ((uint)((0xAA)) << 16) | (0x0003)));

    [NativeTypeName("#define DXC_E_INCORRECT_DXBC DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x0004))")]
    public const int DXC_E_INCORRECT_DXBC = unchecked((int)(((uint)(1) << 31) | ((uint)((0xAA)) << 16) | (0x0004)));

    [NativeTypeName("#define DXC_E_ERROR_PARSING_DXBC_BYTECODE DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x0005))")]
    public const int DXC_E_ERROR_PARSING_DXBC_BYTECODE = unchecked((int)(((uint)(1) << 31) | ((uint)((0xAA)) << 16) | (0x0005)));

    [NativeTypeName("#define DXC_E_DATA_TOO_LARGE DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x0006))")]
    public const int DXC_E_DATA_TOO_LARGE = unchecked((int)(((uint)(1) << 31) | ((uint)((0xAA)) << 16) | (0x0006)));

    [NativeTypeName("#define DXC_E_INCOMPATIBLE_CONVERTER_OPTIONS DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x0007))")]
    public const int DXC_E_INCOMPATIBLE_CONVERTER_OPTIONS = unchecked((int)(((uint)(1) << 31) | ((uint)((0xAA)) << 16) | (0x0007)));

    [NativeTypeName("#define DXC_E_IRREDUCIBLE_CFG DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x0008))")]
    public const int DXC_E_IRREDUCIBLE_CFG = unchecked((int)(((uint)(1) << 31) | ((uint)((0xAA)) << 16) | (0x0008)));

    [NativeTypeName("#define DXC_E_IR_VERIFICATION_FAILED DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x0009))")]
    public const int DXC_E_IR_VERIFICATION_FAILED = unchecked((int)(((uint)(1) << 31) | ((uint)((0xAA)) << 16) | (0x0009)));

    [NativeTypeName("#define DXC_E_SCOPE_NESTED_FAILED DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x000A))")]
    public const int DXC_E_SCOPE_NESTED_FAILED = unchecked((int)(((uint)(1) << 31) | ((uint)((0xAA)) << 16) | (0x000A)));

    [NativeTypeName("#define DXC_E_NOT_SUPPORTED DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x000B))")]
    public const int DXC_E_NOT_SUPPORTED = unchecked((int)(((uint)(1) << 31) | ((uint)((0xAA)) << 16) | (0x000B)));

    [NativeTypeName("#define DXC_E_STRING_ENCODING_FAILED DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x000C))")]
    public const int DXC_E_STRING_ENCODING_FAILED = unchecked((int)(((uint)(1) << 31) | ((uint)((0xAA)) << 16) | (0x000C)));

    [NativeTypeName("#define DXC_E_CONTAINER_INVALID DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x000D))")]
    public const int DXC_E_CONTAINER_INVALID = unchecked((int)(((uint)(1) << 31) | ((uint)((0xAA)) << 16) | (0x000D)));

    [NativeTypeName("#define DXC_E_CONTAINER_MISSING_DXIL DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x000E))")]
    public const int DXC_E_CONTAINER_MISSING_DXIL = unchecked((int)(((uint)(1) << 31) | ((uint)((0xAA)) << 16) | (0x000E)));

    [NativeTypeName("#define DXC_E_INCORRECT_DXIL_METADATA DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x000F))")]
    public const int DXC_E_INCORRECT_DXIL_METADATA = unchecked((int)(((uint)(1) << 31) | ((uint)((0xAA)) << 16) | (0x000F)));

    [NativeTypeName("#define DXC_E_INCORRECT_DDI_SIGNATURE DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x0010))")]
    public const int DXC_E_INCORRECT_DDI_SIGNATURE = unchecked((int)(((uint)(1) << 31) | ((uint)((0xAA)) << 16) | (0x0010)));

    [NativeTypeName("#define DXC_E_DUPLICATE_PART DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x0011))")]
    public const int DXC_E_DUPLICATE_PART = unchecked((int)(((uint)(1) << 31) | ((uint)((0xAA)) << 16) | (0x0011)));

    [NativeTypeName("#define DXC_E_MISSING_PART DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x0012))")]
    public const int DXC_E_MISSING_PART = unchecked((int)(((uint)(1) << 31) | ((uint)((0xAA)) << 16) | (0x0012)));

    [NativeTypeName("#define DXC_E_MALFORMED_CONTAINER DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x0013))")]
    public const int DXC_E_MALFORMED_CONTAINER = unchecked((int)(((uint)(1) << 31) | ((uint)((0xAA)) << 16) | (0x0013)));

    [NativeTypeName("#define DXC_E_INCORRECT_ROOT_SIGNATURE DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x0014))")]
    public const int DXC_E_INCORRECT_ROOT_SIGNATURE = unchecked((int)(((uint)(1) << 31) | ((uint)((0xAA)) << 16) | (0x0014)));

    [NativeTypeName("#define DXC_E_CONTAINER_MISSING_DEBUG DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x0015))")]
    public const int DXC_E_CONTAINER_MISSING_DEBUG = unchecked((int)(((uint)(1) << 31) | ((uint)((0xAA)) << 16) | (0x0015)));

    [NativeTypeName("#define DXC_E_MACRO_EXPANSION_FAILURE DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x0016))")]
    public const int DXC_E_MACRO_EXPANSION_FAILURE = unchecked((int)(((uint)(1) << 31) | ((uint)((0xAA)) << 16) | (0x0016)));

    [NativeTypeName("#define DXC_E_OPTIMIZATION_FAILED DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x0017))")]
    public const int DXC_E_OPTIMIZATION_FAILED = unchecked((int)(((uint)(1) << 31) | ((uint)((0xAA)) << 16) | (0x0017)));

    [NativeTypeName("#define DXC_E_GENERAL_INTERNAL_ERROR DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x0018))")]
    public const int DXC_E_GENERAL_INTERNAL_ERROR = unchecked((int)(((uint)(1) << 31) | ((uint)((0xAA)) << 16) | (0x0018)));

    [NativeTypeName("#define DXC_E_ABORT_COMPILATION_ERROR DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x0019))")]
    public const int DXC_E_ABORT_COMPILATION_ERROR = unchecked((int)(((uint)(1) << 31) | ((uint)((0xAA)) << 16) | (0x0019)));

    [NativeTypeName("#define DXC_E_EXTENSION_ERROR DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x001A))")]
    public const int DXC_E_EXTENSION_ERROR = unchecked((int)(((uint)(1) << 31) | ((uint)((0xAA)) << 16) | (0x001A)));

    [NativeTypeName("#define DXC_E_LLVM_FATAL_ERROR DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x001B))")]
    public const int DXC_E_LLVM_FATAL_ERROR = unchecked((int)(((uint)(1) << 31) | ((uint)((0xAA)) << 16) | (0x001B)));

    [NativeTypeName("#define DXC_E_LLVM_UNREACHABLE DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x001C))")]
    public const int DXC_E_LLVM_UNREACHABLE = unchecked((int)(((uint)(1) << 31) | ((uint)((0xAA)) << 16) | (0x001C)));

    [NativeTypeName("#define DXC_E_LLVM_CAST_ERROR DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x001D))")]
    public const int DXC_E_LLVM_CAST_ERROR = unchecked((int)(((uint)(1) << 31) | ((uint)((0xAA)) << 16) | (0x001D)));

    [NativeTypeName("#define DXC_E_VALIDATOR_MISSING DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x001E))")]
    public const int DXC_E_VALIDATOR_MISSING = unchecked((int)(((uint)(1) << 31) | ((uint)((0xAA)) << 16) | (0x001E)));

    [NativeTypeName("#define DXC_E_INCORRECT_PROGRAM_VERSION DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x001F))")]
    public const int DXC_E_INCORRECT_PROGRAM_VERSION = unchecked((int)(((uint)(1) << 31) | ((uint)((0xAA)) << 16) | (0x001F)));

    [NativeTypeName("#define DXC_CP_UTF8 65001")]
    public const int DXC_CP_UTF8 = 65001;

    [NativeTypeName("#define DXC_CP_UTF16 1200")]
    public const int DXC_CP_UTF16 = 1200;

    [NativeTypeName("#define DXC_CP_UTF32 12000")]
    public const int DXC_CP_UTF32 = 12000;

    [NativeTypeName("#define DXC_CP_ACP 0")]
    public const int DXC_CP_ACP = 0;

    [NativeTypeName("#define DXC_CP_WIDE DXC_CP_UTF16")]
    public const int DXC_CP_WIDE = 1200;

    [NativeTypeName("#define DXC_HASHFLAG_INCLUDES_SOURCE 1")]
    public const int DXC_HASHFLAG_INCLUDES_SOURCE = 1;

    [NativeTypeName("#define DXC_PART_PDB DXC_FOURCC('I', 'L', 'D', 'B')")]
    public const uint DXC_PART_PDB = ((byte)('I') | (uint)((byte)('L')) << 8 | (uint)((byte)('D')) << 16 | (uint)((byte)('B')) << 24);

    [NativeTypeName("#define DXC_PART_PDB_NAME DXC_FOURCC('I', 'L', 'D', 'N')")]
    public const uint DXC_PART_PDB_NAME = ((byte)('I') | (uint)((byte)('L')) << 8 | (uint)((byte)('D')) << 16 | (uint)((byte)('N')) << 24);

    [NativeTypeName("#define DXC_PART_PRIVATE_DATA DXC_FOURCC('P', 'R', 'I', 'V')")]
    public const uint DXC_PART_PRIVATE_DATA = ((byte)('P') | (uint)((byte)('R')) << 8 | (uint)((byte)('I')) << 16 | (uint)((byte)('V')) << 24);

    [NativeTypeName("#define DXC_PART_ROOT_SIGNATURE DXC_FOURCC('R', 'T', 'S', '0')")]
    public const uint DXC_PART_ROOT_SIGNATURE = ((byte)('R') | (uint)((byte)('T')) << 8 | (uint)((byte)('S')) << 16 | (uint)((byte)('0')) << 24);

    [NativeTypeName("#define DXC_PART_DXIL DXC_FOURCC('D', 'X', 'I', 'L')")]
    public const uint DXC_PART_DXIL = ((byte)('D') | (uint)((byte)('X')) << 8 | (uint)((byte)('I')) << 16 | (uint)((byte)('L')) << 24);

    [NativeTypeName("#define DXC_PART_REFLECTION_DATA DXC_FOURCC('S', 'T', 'A', 'T')")]
    public const uint DXC_PART_REFLECTION_DATA = ((byte)('S') | (uint)((byte)('T')) << 8 | (uint)((byte)('A')) << 16 | (uint)((byte)('T')) << 24);

    [NativeTypeName("#define DXC_PART_SHADER_HASH DXC_FOURCC('H', 'A', 'S', 'H')")]
    public const uint DXC_PART_SHADER_HASH = ((byte)('H') | (uint)((byte)('A')) << 8 | (uint)((byte)('S')) << 16 | (uint)((byte)('H')) << 24);

    [NativeTypeName("#define DXC_PART_INPUT_SIGNATURE DXC_FOURCC('I', 'S', 'G', '1')")]
    public const uint DXC_PART_INPUT_SIGNATURE = ((byte)('I') | (uint)((byte)('S')) << 8 | (uint)((byte)('G')) << 16 | (uint)((byte)('1')) << 24);

    [NativeTypeName("#define DXC_PART_OUTPUT_SIGNATURE DXC_FOURCC('O', 'S', 'G', '1')")]
    public const uint DXC_PART_OUTPUT_SIGNATURE = ((byte)('O') | (uint)((byte)('S')) << 8 | (uint)((byte)('G')) << 16 | (uint)((byte)('1')) << 24);

    [NativeTypeName("#define DXC_PART_PATCH_CONSTANT_SIGNATURE DXC_FOURCC('P', 'S', 'G', '1')")]
    public const uint DXC_PART_PATCH_CONSTANT_SIGNATURE = ((byte)('P') | (uint)((byte)('S')) << 8 | (uint)((byte)('G')) << 16 | (uint)((byte)('1')) << 24);

    [NativeTypeName("#define DXC_ARG_DEBUG L\"-Zi\"")]
    public const string DXC_ARG_DEBUG = "-Zi";

    [NativeTypeName("#define DXC_ARG_SKIP_VALIDATION L\"-Vd\"")]
    public const string DXC_ARG_SKIP_VALIDATION = "-Vd";

    [NativeTypeName("#define DXC_ARG_SKIP_OPTIMIZATIONS L\"-Od\"")]
    public const string DXC_ARG_SKIP_OPTIMIZATIONS = "-Od";

    [NativeTypeName("#define DXC_ARG_PACK_MATRIX_ROW_MAJOR L\"-Zpr\"")]
    public const string DXC_ARG_PACK_MATRIX_ROW_MAJOR = "-Zpr";

    [NativeTypeName("#define DXC_ARG_PACK_MATRIX_COLUMN_MAJOR L\"-Zpc\"")]
    public const string DXC_ARG_PACK_MATRIX_COLUMN_MAJOR = "-Zpc";

    [NativeTypeName("#define DXC_ARG_AVOID_FLOW_CONTROL L\"-Gfa\"")]
    public const string DXC_ARG_AVOID_FLOW_CONTROL = "-Gfa";

    [NativeTypeName("#define DXC_ARG_PREFER_FLOW_CONTROL L\"-Gfp\"")]
    public const string DXC_ARG_PREFER_FLOW_CONTROL = "-Gfp";

    [NativeTypeName("#define DXC_ARG_ENABLE_STRICTNESS L\"-Ges\"")]
    public const string DXC_ARG_ENABLE_STRICTNESS = "-Ges";

    [NativeTypeName("#define DXC_ARG_ENABLE_BACKWARDS_COMPATIBILITY L\"-Gec\"")]
    public const string DXC_ARG_ENABLE_BACKWARDS_COMPATIBILITY = "-Gec";

    [NativeTypeName("#define DXC_ARG_IEEE_STRICTNESS L\"-Gis\"")]
    public const string DXC_ARG_IEEE_STRICTNESS = "-Gis";

    [NativeTypeName("#define DXC_ARG_OPTIMIZATION_LEVEL0 L\"-O0\"")]
    public const string DXC_ARG_OPTIMIZATION_LEVEL0 = "-O0";

    [NativeTypeName("#define DXC_ARG_OPTIMIZATION_LEVEL1 L\"-O1\"")]
    public const string DXC_ARG_OPTIMIZATION_LEVEL1 = "-O1";

    [NativeTypeName("#define DXC_ARG_OPTIMIZATION_LEVEL2 L\"-O2\"")]
    public const string DXC_ARG_OPTIMIZATION_LEVEL2 = "-O2";

    [NativeTypeName("#define DXC_ARG_OPTIMIZATION_LEVEL3 L\"-O3\"")]
    public const string DXC_ARG_OPTIMIZATION_LEVEL3 = "-O3";

    [NativeTypeName("#define DXC_ARG_WARNINGS_ARE_ERRORS L\"-WX\"")]
    public const string DXC_ARG_WARNINGS_ARE_ERRORS = "-WX";

    [NativeTypeName("#define DXC_ARG_RESOURCES_MAY_ALIAS L\"-res_may_alias\"")]
    public const string DXC_ARG_RESOURCES_MAY_ALIAS = "-res_may_alias";

    [NativeTypeName("#define DXC_ARG_ALL_RESOURCES_BOUND L\"-all_resources_bound\"")]
    public const string DXC_ARG_ALL_RESOURCES_BOUND = "-all_resources_bound";

    [NativeTypeName("#define DXC_ARG_DEBUG_NAME_FOR_SOURCE L\"-Zss\"")]
    public const string DXC_ARG_DEBUG_NAME_FOR_SOURCE = "-Zss";

    [NativeTypeName("#define DXC_ARG_DEBUG_NAME_FOR_BINARY L\"-Zsb\"")]
    public const string DXC_ARG_DEBUG_NAME_FOR_BINARY = "-Zsb";

    [NativeTypeName("#define DXC_EXTRA_OUTPUT_NAME_STDOUT L\"*stdout*\"")]
    public const string DXC_EXTRA_OUTPUT_NAME_STDOUT = "*stdout*";

    [NativeTypeName("#define DXC_EXTRA_OUTPUT_NAME_STDERR L\"*stderr*\"")]
    public const string DXC_EXTRA_OUTPUT_NAME_STDERR = "*stderr*";

    public static ref readonly Guid IID_IDxcBlob
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x08, 0xFB, 0xA5, 0x8B,
                0x95, 0x51,
                0xE2, 0x40,
                0xAC,
                0x58,
                0x0D,
                0x98,
                0x9C,
                0x3A,
                0x01,
                0x02
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcBlobEncoding
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x24, 0xD4, 0x41, 0x72,
                0x46, 0x26,
                0x91, 0x41,
                0x97,
                0xC0,
                0x98,
                0xE9,
                0x6E,
                0x42,
                0xFC,
                0x68
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcBlobWide
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0xAB, 0x4E, 0xF8, 0xA3,
                0xAA, 0x0F,
                0x7E, 0x49,
                0xA3,
                0x9C,
                0xEE,
                0x6E,
                0xD6,
                0x0B,
                0x2D,
                0x84
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcBlobUtf8
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0xC9, 0x36, 0xA6, 0x3D,
                0x71, 0xBA,
                0x24, 0x40,
                0xA3,
                0x01,
                0x30,
                0xCB,
                0xF1,
                0x25,
                0x30,
                0x5B
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcIncludeHandler
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x7D, 0xFC, 0x61, 0x7F,
                0x0D, 0x95,
                0x7F, 0x46,
                0xB3,
                0xE3,
                0x3C,
                0x02,
                0xFB,
                0x49,
                0x18,
                0x7C
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcCompilerArgs
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x2A, 0xFE, 0xEF, 0x73,
                0xDC, 0x70,
                0xF8, 0x45,
                0x96,
                0x90,
                0xEF,
                0xF6,
                0x4C,
                0x02,
                0x42,
                0x9D
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcLibrary
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0xC7, 0x4D, 0x20, 0xE5,
                0x8C, 0xD1,
                0x3C, 0x4C,
                0xBD,
                0xFB,
                0x85,
                0x16,
                0x73,
                0x98,
                0x0F,
                0xE7
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcOperationResult
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x4A, 0x48, 0xDB, 0xCE,
                0xE9, 0xD4,
                0x5A, 0x44,
                0xB9,
                0x91,
                0xCA,
                0x21,
                0xCA,
                0x15,
                0x7D,
                0xC2
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcCompiler
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0xF3, 0x0B, 0x21, 0x8C,
                0x1F, 0x01,
                0x22, 0x44,
                0x8D,
                0x70,
                0x6F,
                0x9A,
                0xCB,
                0x8D,
                0xB6,
                0x17
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcCompiler2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0xD9, 0xA9, 0x05, 0xA0,
                0xBB, 0xB8,
                0x94, 0x45,
                0xB5,
                0xC9,
                0x0E,
                0x63,
                0x3B,
                0xEC,
                0x4D,
                0x37
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcLinker
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x2A, 0xBE, 0xB5, 0xF1,
                0xDD, 0x62,
                0x27, 0x43,
                0xA1,
                0xC2,
                0x42,
                0xAC,
                0x1E,
                0x1E,
                0x78,
                0xE6
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0xCB, 0xC4, 0x05, 0x46,
                0x19, 0x20,
                0x2A, 0x49,
                0xAD,
                0xA4,
                0x65,
                0xF2,
                0x0B,
                0xB7,
                0xD6,
                0x7F
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcResult
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0xDA, 0x6C, 0x34, 0x58,
                0xE7, 0xDD,
                0x97, 0x44,
                0x94,
                0x61,
                0x6F,
                0x87,
                0xAF,
                0x5E,
                0x06,
                0x59
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcExtraOutputs
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0xA2, 0x37, 0x9B, 0x31,
                0xC2, 0xA5,
                0x4A, 0x49,
                0xA5,
                0xDE,
                0x48,
                0x01,
                0xB2,
                0xFA,
                0xF9,
                0x89
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcCompiler3
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x87, 0x46, 0x8B, 0x22,
                0x6A, 0x5A,
                0x30, 0x47,
                0x90,
                0x0C,
                0x97,
                0x02,
                0xB2,
                0x20,
                0x3F,
                0x54
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcValidator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0xD2, 0x2B, 0xE8, 0xA6,
                0xD7, 0x1F,
                0x26, 0x48,
                0x98,
                0x11,
                0x28,
                0x57,
                0xE7,
                0x97,
                0xF4,
                0x9A
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcValidator2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0xD1, 0x1F, 0x8E, 0x45,
                0xB2, 0xB1,
                0x50, 0x47,
                0xA6,
                0xE1,
                0x9C,
                0x10,
                0xF0,
                0x3B,
                0xED,
                0x92
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcContainerBuilder
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x50, 0x1F, 0x4B, 0x33,
                0x92, 0x22,
                0x35, 0x4B,
                0x99,
                0xA1,
                0x25,
                0x58,
                0x8D,
                0x8C,
                0x17,
                0xFE
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcAssembler
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x26, 0x7A, 0x1F, 0x09,
                0x1F, 0x1C,
                0x48, 0x49,
                0x90,
                0x4B,
                0xE6,
                0xE3,
                0xA8,
                0xA7,
                0x71,
                0xD5
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcContainerReflection
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x26, 0x1B, 0xC2, 0xD2,
                0x50, 0x83,
                0xDC, 0x4B,
                0x97,
                0x6A,
                0x33,
                0x1C,
                0xE6,
                0xF4,
                0xC5,
                0x4C
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcOptimizerPass
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x9F, 0xD7, 0x2C, 0xAE,
                0x22, 0xCC,
                0x3F, 0x45,
                0x9B,
                0x6B,
                0xB1,
                0x24,
                0xE7,
                0xA5,
                0x20,
                0x4C
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcOptimizer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x2E, 0x0E, 0x74, 0x25,
                0xBA, 0x9C,
                0x1B, 0x40,
                0x91,
                0x19,
                0x4F,
                0xB4,
                0x2F,
                0x39,
                0xF2,
                0x70
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcVersionInfo
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x50, 0x5B, 0x4F, 0xB0,
                0x59, 0x20,
                0x12, 0x4F,
                0xA8,
                0xFF,
                0xA1,
                0xE0,
                0xCD,
                0xE1,
                0xCC,
                0x7E
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcVersionInfo2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0xC4, 0x04, 0x69, 0xFB,
                0xF0, 0x42,
                0x62, 0x4B,
                0x9C,
                0x46,
                0x98,
                0x3A,
                0xF7,
                0xDA,
                0x7C,
                0x83
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcVersionInfo3
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x43, 0xE8, 0x13, 0x5E,
                0x25, 0x9D,
                0x3C, 0x47,
                0x9A,
                0xD2,
                0x03,
                0xB2,
                0xD0,
                0xB4,
                0x4B,
                0x1E
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcPdbUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x7E, 0x64, 0xC9, 0xE6,
                0x6A, 0x9D,
                0x3B, 0x4C,
                0xB9,
                0x4C,
                0x52,
                0x4B,
                0x5A,
                0x6C,
                0x34,
                0x3D
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcPdbUtils2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x38, 0xD9, 0x15, 0x43,
                0x69, 0xF3,
                0x93, 0x4F,
                0x95,
                0xA2,
                0x25,
                0x20,
                0x17,
                0xCC,
                0x38,
                0x07
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcCursor
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x85, 0xB9, 0x67, 0x14,
                0x8D, 0x28,
                0x2A, 0x4D,
                0x80,
                0xC1,
                0xEF,
                0x89,
                0xC4,
                0x2C,
                0x40,
                0xBC
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcDiagnostic
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x34, 0xB2, 0x76, 0x4F,
                0x59, 0x36,
                0x33, 0x4D,
                0x99,
                0xB0,
                0x3B,
                0x0D,
                0xB9,
                0x94,
                0xB5,
                0x64
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcFile
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x9E, 0xCA, 0x2F, 0xBB,
                0x78, 0x14,
                0xBA, 0x47,
                0xB0,
                0x8C,
                0x2C,
                0x50,
                0x2A,
                0xDA,
                0x48,
                0x95
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcInclusion
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x65, 0x4D, 0x36, 0x0C,
                0x44, 0xDF,
                0x12, 0x44,
                0x88,
                0x8E,
                0x4E,
                0x55,
                0x2F,
                0xC5,
                0xE3,
                0xD6
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcIntelliSense
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x13, 0x95, 0xF9, 0xB1,
                0xD6, 0x46,
                0x12, 0x41,
                0x81,
                0x69,
                0xDD,
                0x0D,
                0x60,
                0x53,
                0xF1,
                0x7D
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcIndex
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0xA0, 0x24, 0x78, 0x93,
                0x5A, 0x7F,
                0x15, 0x48,
                0x9B,
                0xA7,
                0x7F,
                0xC0,
                0x42,
                0x4F,
                0x41,
                0x73
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcSourceLocation
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x1C, 0xDF, 0x7D, 0x8E,
                0xD3, 0xD7,
                0x69, 0x4D,
                0xB2,
                0x86,
                0x85,
                0xFC,
                0xCB,
                0xA1,
                0xE0,
                0xCF
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcSourceRange
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x36, 0x9B, 0x35, 0xF1,
                0x3F, 0xA5,
                0x81, 0x4E,
                0xB5,
                0x14,
                0xB6,
                0xB8,
                0x41,
                0x22,
                0xA1,
                0x3F
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcToken
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0xFF, 0xB9, 0x90, 0x7F,
                0x75, 0xA2,
                0x32, 0x49,
                0x97,
                0xD8,
                0x3C,
                0xFD,
                0x23,
                0x44,
                0x82,
                0xA2
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcTranslationUnit
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0xE0, 0xDE, 0x77, 0x96,
                0xE5, 0xC0,
                0xA1, 0x46,
                0x8B,
                0x40,
                0x3D,
                0xB3,
                0x16,
                0x8B,
                0xE6,
                0x3D
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcType
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0xFD, 0x12, 0xC9, 0x2E,
                0x44, 0xB1,
                0x15, 0x4A,
                0xAD,
                0x0D,
                0x1C,
                0x54,
                0x39,
                0xC8,
                0x1E,
                0x46
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcUnsavedFile
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x98, 0x0F, 0xC0, 0x8E,
                0xD0, 0x07,
                0x60, 0x4E,
                0x9D,
                0x7C,
                0x5A,
                0x50,
                0xB5,
                0xB0,
                0x01,
                0x7F
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcCodeCompleteResults
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x6A, 0x46, 0x06, 0x1E,
                0x8B, 0xFD,
                0xF3, 0x45,
                0xA7,
                0x8F,
                0x8A,
                0x3F,
                0x76,
                0xEB,
                0xB5,
                0x52
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcCompletionResult
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x88, 0x05, 0x3C, 0x94,
                0xD0, 0x22,
                0x84, 0x47,
                0x86,
                0xFC,
                0x70,
                0x1F,
                0x80,
                0x2A,
                0xC2,
                0xB6
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcCompletionString
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x0F, 0x1E, 0xB5, 0x06,
                0x05, 0xA6,
                0x69, 0x4C,
                0xA1,
                0x10,
                0xCD,
                0x6E,
                0x14,
                0xB5,
                0x8E,
                0xEC
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcPixType
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x13, 0x8C, 0x9D, 0x19,
                0x12, 0xD3,
                0x97, 0x41,
                0xA2,
                0xC1,
                0x07,
                0xA5,
                0x32,
                0x99,
                0x97,
                0x27
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcPixConstType
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x8B, 0x2C, 0xDF, 0xD9,
                0x73, 0x27,
                0x6D, 0x46,
                0x9B,
                0xC2,
                0xD8,
                0x48,
                0xD8,
                0x49,
                0x6B,
                0xF6
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcPixTypedefType
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0xC0, 0xA9, 0xFC, 0x7B,
                0xD0, 0x1E,
                0x9C, 0x42,
                0x9D,
                0xC2,
                0xC7,
                0x55,
                0x97,
                0xD8,
                0x21,
                0xD2
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcPixScalarType
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x52, 0x16, 0x6E, 0x24,
                0x2A, 0xED,
                0xFC, 0x4F,
                0xA9,
                0x49,
                0x43,
                0xBF,
                0x63,
                0x75,
                0x0E,
                0xE5
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcPixArrayType
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0xD3, 0xD9, 0xA0, 0x9B,
                0x7B, 0x45,
                0x6F, 0x42,
                0x80,
                0x19,
                0x9F,
                0x38,
                0x49,
                0x98,
                0x2A,
                0xA2
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcPixStructField0
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x08, 0x7D, 0x70, 0x6C,
                0x95, 0x79,
                0x84, 0x4A,
                0xBA,
                0xE5,
                0xE6,
                0xD8,
                0x29,
                0x1F,
                0x3B,
                0x78
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcPixStructField
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x7C, 0x59, 0x45, 0xDE,
                0x69, 0x58,
                0x97, 0x4F,
                0xA7,
                0x7B,
                0xD6,
                0x65,
                0x0B,
                0x9A,
                0x16,
                0xCF
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcPixStructType
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x44, 0x8C, 0xC0, 0x24,
                0x4B, 0x68,
                0x1C, 0x4B,
                0xB4,
                0x1B,
                0xF8,
                0x77,
                0x23,
                0x83,
                0xD0,
                0x74
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcPixStructType2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x0C, 0xF4, 0x09, 0x74,
                0xCB, 0xDC,
                0xAA, 0x41,
                0xBB,
                0x42,
                0x1C,
                0x95,
                0xBB,
                0xF7,
                0x56,
                0x2F
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcPixDxilStorage
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0xF5, 0x22, 0xD5, 0x74,
                0xC4, 0x16,
                0xCB, 0x40,
                0x86,
                0x7B,
                0x4B,
                0x41,
                0x49,
                0xE3,
                0xDB,
                0x0E
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcPixVariable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x30, 0x4B, 0x95, 0x2F,
                0xA7, 0x61,
                0x48, 0x43,
                0x95,
                0xB1,
                0x2D,
                0xB3,
                0x56,
                0xA7,
                0x5C,
                0xDE
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcPixDxilLiveVariables
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x2F, 0x30, 0x9D, 0xC5,
                0xA2, 0x34,
                0xE5, 0x4F,
                0x96,
                0x46,
                0x32,
                0xCE,
                0x7A,
                0x52,
                0xD0,
                0x3F
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcPixDxilInstructionOffsets
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x5E, 0xF8, 0x71, 0xEB,
                0x42, 0x85,
                0xB5, 0x44,
                0x87,
                0xDA,
                0x9D,
                0x76,
                0x04,
                0x5A,
                0x19,
                0x10
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcPixDxilSourceLocations
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x3D, 0x83, 0x1C, 0x76,
                0xB8, 0xE7,
                0x24, 0x46,
                0x80,
                0xF8,
                0x3A,
                0x3F,
                0xB4,
                0x14,
                0x63,
                0x42
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcPixDxilDebugInfo
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x8E, 0x63, 0x75, 0xB8,
                0x8A, 0x10,
                0x90, 0x4D,
                0xA5,
                0x3A,
                0x68,
                0xD6,
                0x37,
                0x73,
                0xCB,
                0x38
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcPixCompilationInfo
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x95, 0x6C, 0xB1, 0x61,
                0x99, 0x87,
                0xD8, 0x4E,
                0xBD,
                0xB0,
                0x3B,
                0x6C,
                0x08,
                0xA1,
                0x41,
                0xB4
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static ref readonly Guid IID_IDxcPixDxilDebugInfoFactory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x0D, 0x04, 0x2A, 0x9C,
                0x68, 0x80,
                0xEC, 0x44,
                0x8C,
                0x68,
                0x8B,
                0xFE,
                0xF1,
                0xB4,
                0x37,
                0x89
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());
            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }
}
