using System;
using System.Runtime.InteropServices;

namespace Ghost.DXGI;

public static unsafe partial class DXGI
{
    [DllImport("dxgi", CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    public static extern HRESULT CreateDXGIFactory([NativeTypeName("const IID &")] Guid* riid, void** ppFactory);

    [DllImport("dxgi", CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    public static extern HRESULT CreateDXGIFactory1([NativeTypeName("const IID &")] Guid* riid, void** ppFactory);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGIObject = new Guid(0xaec22fb8, 0x76f3, 0x4639, 0x9b, 0xe0, 0x28, 0xeb, 0x43, 0xa6, 0x7a, 0x2e);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGIDeviceSubObject = new Guid(0x3d3e0379, 0xf9de, 0x4d58, 0xbb, 0x6c, 0x18, 0xd6, 0x29, 0x92, 0xf1, 0xa6);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGIResource = new Guid(0x035f3ab4, 0x482e, 0x4e50, 0xb4, 0x1f, 0x8a, 0x7f, 0x8b, 0xd8, 0x96, 0x0b);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGIKeyedMutex = new Guid(0x9d8e1289, 0xd7b3, 0x465f, 0x81, 0x26, 0x25, 0x0e, 0x34, 0x9a, 0xf8, 0x5d);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGISurface = new Guid(0xcafcb56c, 0x6ac3, 0x4889, 0xbf, 0x47, 0x9e, 0x23, 0xbb, 0xd2, 0x60, 0xec);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGISurface1 = new Guid(0x4AE63092, 0x6327, 0x4c1b, 0x80, 0xAE, 0xBF, 0xE1, 0x2E, 0xA3, 0x2B, 0x86);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGIAdapter = new Guid(0x2411e7e1, 0x12ac, 0x4ccf, 0xbd, 0x14, 0x97, 0x98, 0xe8, 0x53, 0x4d, 0xc0);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGIOutput = new Guid(0xae02eedb, 0xc735, 0x4690, 0x8d, 0x52, 0x5a, 0x8d, 0xc2, 0x02, 0x13, 0xaa);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGISwapChain = new Guid(0x310d36a0, 0xd2e7, 0x4c0a, 0xaa, 0x04, 0x6a, 0x9d, 0x23, 0xb8, 0x88, 0x6a);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGIFactory = new Guid(0x7b7166ec, 0x21c7, 0x44ae, 0xb2, 0x1a, 0xc9, 0xae, 0x32, 0x1a, 0xe3, 0x69);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGIDevice = new Guid(0x54ec77fa, 0x1377, 0x44e6, 0x8c, 0x32, 0x88, 0xfd, 0x5f, 0x44, 0xc8, 0x4c);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGIFactory1 = new Guid(0x770aae78, 0xf26f, 0x4dba, 0xa8, 0x29, 0x25, 0x3c, 0x83, 0xd1, 0xb3, 0x87);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGIAdapter1 = new Guid(0x29038f61, 0x3839, 0x4626, 0x91, 0xfd, 0x08, 0x68, 0x79, 0x01, 0x1a, 0x05);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGIDevice1 = new Guid(0x77db970f, 0x6276, 0x48ba, 0xba, 0x28, 0x07, 0x01, 0x43, 0xb4, 0x39, 0x2c);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGIDisplayControl = new Guid(0xea9dbf1a, 0xc88e, 0x4486, 0x85, 0x4a, 0x98, 0xaa, 0x01, 0x38, 0xf3, 0x0c);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGIOutputDuplication = new Guid(0x191cfac3, 0xa341, 0x470d, 0xb2, 0x6e, 0xa8, 0x64, 0xf4, 0x28, 0x31, 0x9c);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGISurface2 = new Guid(0xaba496dd, 0xb617, 0x4cb8, 0xa8, 0x66, 0xbc, 0x44, 0xd7, 0xeb, 0x1f, 0xa2);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGIResource1 = new Guid(0x30961379, 0x4609, 0x4a41, 0x99, 0x8e, 0x54, 0xfe, 0x56, 0x7e, 0xe0, 0xc1);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGIDevice2 = new Guid(0x05008617, 0xfbfd, 0x4051, 0xa7, 0x90, 0x14, 0x48, 0x84, 0xb4, 0xf6, 0xa9);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGISwapChain1 = new Guid(0x790a45f7, 0x0d42, 0x4876, 0x98, 0x3a, 0x0a, 0x55, 0xcf, 0xe6, 0xf4, 0xaa);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGIFactory2 = new Guid(0x50c83a1c, 0xe072, 0x4c48, 0x87, 0xb0, 0x36, 0x30, 0xfa, 0x36, 0xa6, 0xd0);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGIAdapter2 = new Guid(0x0AA1AE0A, 0xFA0E, 0x4B84, 0x86, 0x44, 0xE0, 0x5F, 0xF8, 0xE5, 0xAC, 0xB5);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGIOutput1 = new Guid(0x00cddea8, 0x939b, 0x4b83, 0xa3, 0x40, 0xa6, 0x85, 0x22, 0x66, 0x66, 0xcc);

    [DllImport("dxgi", CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    public static extern HRESULT CreateDXGIFactory2(uint Flags, [NativeTypeName("const IID &")] Guid* riid, void** ppFactory);

    [DllImport("dxgi", CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    public static extern HRESULT DXGIGetDebugInterface1(uint Flags, [NativeTypeName("const IID &")] Guid* riid, void** pDebug);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGIDevice3 = new Guid(0x6007896c, 0x3244, 0x4afd, 0xbf, 0x18, 0xa6, 0xd3, 0xbe, 0xda, 0x50, 0x23);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGISwapChain2 = new Guid(0xa8be2ac4, 0x199f, 0x4946, 0xb3, 0x31, 0x79, 0x59, 0x9f, 0xb9, 0x8d, 0xe7);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGIOutput2 = new Guid(0x595e39d1, 0x2724, 0x4663, 0x99, 0xb1, 0xda, 0x96, 0x9d, 0xe2, 0x83, 0x64);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGIFactory3 = new Guid(0x25483823, 0xcd46, 0x4c7d, 0x86, 0xca, 0x47, 0xaa, 0x95, 0xb8, 0x37, 0xbd);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGIDecodeSwapChain = new Guid(0x2633066b, 0x4514, 0x4c7a, 0x8f, 0xd8, 0x12, 0xea, 0x98, 0x05, 0x9d, 0x18);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGIFactoryMedia = new Guid(0x41e7d1f2, 0xa591, 0x4f7b, 0xa2, 0xe5, 0xfa, 0x9c, 0x84, 0x3e, 0x1c, 0x12);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGISwapChainMedia = new Guid(0xdd95b90b, 0xf05f, 0x4f6a, 0xbd, 0x65, 0x25, 0xbf, 0xb2, 0x64, 0xbd, 0x84);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGIOutput3 = new Guid(0x8a6bb301, 0x7e7e, 0x41F4, 0xa8, 0xe0, 0x5b, 0x32, 0xf7, 0xf9, 0x9b, 0x18);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGISwapChain3 = new Guid(0x94d99bdb, 0xf1f8, 0x4ab0, 0xb2, 0x36, 0x7d, 0xa0, 0x17, 0x0e, 0xda, 0xb1);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGIOutput4 = new Guid(0xdc7dca35, 0x2196, 0x414d, 0x9F, 0x53, 0x61, 0x78, 0x84, 0x03, 0x2a, 0x60);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGIFactory4 = new Guid(0x1bc6ea02, 0xef36, 0x464f, 0xbf, 0x0c, 0x21, 0xca, 0x39, 0xe5, 0x16, 0x8a);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGIAdapter3 = new Guid(0x645967A4, 0x1392, 0x4310, 0xA7, 0x98, 0x80, 0x53, 0xCE, 0x3E, 0x93, 0xFD);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGIOutput5 = new Guid(0x80A07424, 0xAB52, 0x42EB, 0x83, 0x3C, 0x0C, 0x42, 0xFD, 0x28, 0x2D, 0x98);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGISwapChain4 = new Guid(0x3D585D5A, 0xBD4A, 0x489E, 0xB1, 0xF4, 0x3D, 0xBC, 0xB6, 0x45, 0x2F, 0xFB);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGIDevice4 = new Guid(0x95B4F95F, 0xD8DA, 0x4CA4, 0x9E, 0xE6, 0x3B, 0x76, 0xD5, 0x96, 0x8A, 0x10);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGIFactory5 = new Guid(0x7632e1f5, 0xee65, 0x4dca, 0x87, 0xfd, 0x84, 0xcd, 0x75, 0xf8, 0x83, 0x8d);

    [DllImport("dxgi", CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    public static extern HRESULT DXGIDeclareAdapterRemovalSupport();

    [DllImport("dxgi", CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    public static extern HRESULT DXGIDisableVBlankVirtualization();

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGIAdapter4 = new Guid(0x3c8d99d1, 0x4fbf, 0x4181, 0xa8, 0x2c, 0xaf, 0x66, 0xbf, 0x7b, 0xd2, 0x4e);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGIOutput6 = new Guid(0x068346e8, 0xaaec, 0x4b84, 0xad, 0xd7, 0x13, 0x7f, 0x51, 0x3f, 0x77, 0xa1);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGIFactory6 = new Guid(0xc1b6694f, 0xff09, 0x44a9, 0xb0, 0x3c, 0x77, 0x90, 0x0a, 0x0a, 0x1d, 0x17);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGIFactory7 = new Guid(0xa4966eed, 0x76db, 0x44da, 0x84, 0xc1, 0xee, 0x9a, 0x7a, 0xfb, 0x20, 0xa8);

    [NativeTypeName("const GUID")]
    public static readonly Guid DXGI_DEBUG_ALL = new Guid(0xe48ae283, 0xda80, 0x490b, 0x87, 0xe6, 0x43, 0xe9, 0xa9, 0xcf, 0xda, 0x8);

    [NativeTypeName("const GUID")]
    public static readonly Guid DXGI_DEBUG_DX = new Guid(0x35cdd7fc, 0x13b2, 0x421d, 0xa5, 0xd7, 0x7e, 0x44, 0x51, 0x28, 0x7d, 0x64);

    [NativeTypeName("const GUID")]
    public static readonly Guid DXGI_DEBUG_DXGI = new Guid(0x25cddaa4, 0xb1c6, 0x47e1, 0xac, 0x3e, 0x98, 0x87, 0x5b, 0x5a, 0x2e, 0x2a);

    [NativeTypeName("const GUID")]
    public static readonly Guid DXGI_DEBUG_APP = new Guid(0x6cd6e01, 0x4219, 0x4ebd, 0x87, 0x9, 0x27, 0xed, 0x23, 0x36, 0xc, 0x62);

    [DllImport("dxgidebug", CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    public static extern HRESULT DXGIGetDebugInterface([NativeTypeName("const IID &")] Guid* riid, void** ppDebug);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGIInfoQueue = new Guid(0xD67441C7, 0x672A, 0x476f, 0x9E, 0x82, 0xCD, 0x55, 0xB4, 0x49, 0x49, 0xCE);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGIDebug = new Guid(0x119E7452, 0xDE9E, 0x40fe, 0x88, 0x06, 0x88, 0xF9, 0x0C, 0x12, 0xB4, 0x41);

    [NativeTypeName("const GUID")]
    public static readonly Guid IID_IDXGIDebug1 = new Guid(0xc5a05f0c, 0x16f2, 0x4adf, 0x9f, 0x4d, 0xa8, 0xc4, 0xd5, 0x8a, 0xc5, 0x50);

    [NativeTypeName("#define __REQUIRED_RPCNDR_H_VERSION__ 501")]
    public const int __REQUIRED_RPCNDR_H_VERSION__ = 501;

    [NativeTypeName("#define __REQUIRED_RPCSAL_H_VERSION__ 100")]
    public const int __REQUIRED_RPCSAL_H_VERSION__ = 100;

    [NativeTypeName("#define DXGI_STANDARD_MULTISAMPLE_QUALITY_PATTERN 0xffffffff")]
    public const uint DXGI_STANDARD_MULTISAMPLE_QUALITY_PATTERN = 0xffffffff;

    [NativeTypeName("#define DXGI_CENTER_MULTISAMPLE_QUALITY_PATTERN 0xfffffffe")]
    public const uint DXGI_CENTER_MULTISAMPLE_QUALITY_PATTERN = 0xfffffffe;

    [NativeTypeName("#define DXGI_FORMAT_DEFINED 1")]
    public const int DXGI_FORMAT_DEFINED = 1;

    [NativeTypeName("#define _FACDXGI 0x87a")]
    public const int _FACDXGI = 0x87a;

    [NativeTypeName("#define DXGI_CPU_ACCESS_NONE ( 0 )")]
    public const int DXGI_CPU_ACCESS_NONE = (0);

    [NativeTypeName("#define DXGI_CPU_ACCESS_DYNAMIC ( 1 )")]
    public const int DXGI_CPU_ACCESS_DYNAMIC = (1);

    [NativeTypeName("#define DXGI_CPU_ACCESS_READ_WRITE ( 2 )")]
    public const int DXGI_CPU_ACCESS_READ_WRITE = (2);

    [NativeTypeName("#define DXGI_CPU_ACCESS_SCRATCH ( 3 )")]
    public const int DXGI_CPU_ACCESS_SCRATCH = (3);

    [NativeTypeName("#define DXGI_CPU_ACCESS_FIELD 15")]
    public const int DXGI_CPU_ACCESS_FIELD = 15;

    [NativeTypeName("#define DXGI_USAGE_SHADER_INPUT 0x00000010UL")]
    public const uint DXGI_USAGE_SHADER_INPUT = 0x00000010U;

    [NativeTypeName("#define DXGI_USAGE_RENDER_TARGET_OUTPUT 0x00000020UL")]
    public const uint DXGI_USAGE_RENDER_TARGET_OUTPUT = 0x00000020U;

    [NativeTypeName("#define DXGI_USAGE_BACK_BUFFER 0x00000040UL")]
    public const uint DXGI_USAGE_BACK_BUFFER = 0x00000040U;

    [NativeTypeName("#define DXGI_USAGE_SHARED 0x00000080UL")]
    public const uint DXGI_USAGE_SHARED = 0x00000080U;

    [NativeTypeName("#define DXGI_USAGE_READ_ONLY 0x00000100UL")]
    public const uint DXGI_USAGE_READ_ONLY = 0x00000100U;

    [NativeTypeName("#define DXGI_USAGE_DISCARD_ON_PRESENT 0x00000200UL")]
    public const uint DXGI_USAGE_DISCARD_ON_PRESENT = 0x00000200U;

    [NativeTypeName("#define DXGI_USAGE_UNORDERED_ACCESS 0x00000400UL")]
    public const uint DXGI_USAGE_UNORDERED_ACCESS = 0x00000400U;

    [NativeTypeName("#define DXGI_RESOURCE_PRIORITY_MINIMUM ( 0x28000000 )")]
    public const int DXGI_RESOURCE_PRIORITY_MINIMUM = (0x28000000);

    [NativeTypeName("#define DXGI_RESOURCE_PRIORITY_LOW ( 0x50000000 )")]
    public const int DXGI_RESOURCE_PRIORITY_LOW = (0x50000000);

    [NativeTypeName("#define DXGI_RESOURCE_PRIORITY_NORMAL ( 0x78000000 )")]
    public const int DXGI_RESOURCE_PRIORITY_NORMAL = (0x78000000);

    [NativeTypeName("#define DXGI_RESOURCE_PRIORITY_HIGH ( 0xa0000000 )")]
    public const uint DXGI_RESOURCE_PRIORITY_HIGH = (0xa0000000);

    [NativeTypeName("#define DXGI_RESOURCE_PRIORITY_MAXIMUM ( 0xc8000000 )")]
    public const uint DXGI_RESOURCE_PRIORITY_MAXIMUM = (0xc8000000);

    [NativeTypeName("#define DXGI_MAP_READ ( 1UL )")]
    public const uint DXGI_MAP_READ = (1U);

    [NativeTypeName("#define DXGI_MAP_WRITE ( 2UL )")]
    public const uint DXGI_MAP_WRITE = (2U);

    [NativeTypeName("#define DXGI_MAP_DISCARD ( 4UL )")]
    public const uint DXGI_MAP_DISCARD = (4U);

    [NativeTypeName("#define DXGI_ENUM_MODES_INTERLACED ( 1UL )")]
    public const uint DXGI_ENUM_MODES_INTERLACED = (1U);

    [NativeTypeName("#define DXGI_ENUM_MODES_SCALING ( 2UL )")]
    public const uint DXGI_ENUM_MODES_SCALING = (2U);

    [NativeTypeName("#define DXGI_MAX_SWAP_CHAIN_BUFFERS ( 16 )")]
    public const int DXGI_MAX_SWAP_CHAIN_BUFFERS = (16);

    [NativeTypeName("#define DXGI_PRESENT_TEST 0x00000001UL")]
    public const uint DXGI_PRESENT_TEST = 0x00000001U;

    [NativeTypeName("#define DXGI_PRESENT_DO_NOT_SEQUENCE 0x00000002UL")]
    public const uint DXGI_PRESENT_DO_NOT_SEQUENCE = 0x00000002U;

    [NativeTypeName("#define DXGI_PRESENT_RESTART 0x00000004UL")]
    public const uint DXGI_PRESENT_RESTART = 0x00000004U;

    [NativeTypeName("#define DXGI_PRESENT_DO_NOT_WAIT 0x00000008UL")]
    public const uint DXGI_PRESENT_DO_NOT_WAIT = 0x00000008U;

    [NativeTypeName("#define DXGI_PRESENT_STEREO_PREFER_RIGHT 0x00000010UL")]
    public const uint DXGI_PRESENT_STEREO_PREFER_RIGHT = 0x00000010U;

    [NativeTypeName("#define DXGI_PRESENT_STEREO_TEMPORARY_MONO 0x00000020UL")]
    public const uint DXGI_PRESENT_STEREO_TEMPORARY_MONO = 0x00000020U;

    [NativeTypeName("#define DXGI_PRESENT_RESTRICT_TO_OUTPUT 0x00000040UL")]
    public const uint DXGI_PRESENT_RESTRICT_TO_OUTPUT = 0x00000040U;

    [NativeTypeName("#define DXGI_PRESENT_USE_DURATION 0x00000100UL")]
    public const uint DXGI_PRESENT_USE_DURATION = 0x00000100U;

    [NativeTypeName("#define DXGI_PRESENT_ALLOW_TEARING 0x00000200UL")]
    public const uint DXGI_PRESENT_ALLOW_TEARING = 0x00000200U;

    [NativeTypeName("#define DXGI_MWA_NO_WINDOW_CHANGES ( 1 << 0 )")]
    public const int DXGI_MWA_NO_WINDOW_CHANGES = (1 << 0);

    [NativeTypeName("#define DXGI_MWA_NO_ALT_ENTER ( 1 << 1 )")]
    public const int DXGI_MWA_NO_ALT_ENTER = (1 << 1);

    [NativeTypeName("#define DXGI_MWA_NO_PRINT_SCREEN ( 1 << 2 )")]
    public const int DXGI_MWA_NO_PRINT_SCREEN = (1 << 2);

    [NativeTypeName("#define DXGI_MWA_VALID ( 0x7 )")]
    public const int DXGI_MWA_VALID = (0x7);

    [NativeTypeName("#define DXGI_ENUM_MODES_STEREO ( 4UL )")]
    public const uint DXGI_ENUM_MODES_STEREO = (4U);

    [NativeTypeName("#define DXGI_ENUM_MODES_DISABLED_STEREO ( 8UL )")]
    public const uint DXGI_ENUM_MODES_DISABLED_STEREO = (8U);

    [NativeTypeName("#define DXGI_SHARED_RESOURCE_READ ( 0x80000000L )")]
    public const uint DXGI_SHARED_RESOURCE_READ = (0x80000000);

    [NativeTypeName("#define DXGI_SHARED_RESOURCE_WRITE ( 1 )")]
    public const int DXGI_SHARED_RESOURCE_WRITE = (1);

    [NativeTypeName("#define DXGI_CREATE_FACTORY_DEBUG 0x1")]
    public const int DXGI_CREATE_FACTORY_DEBUG = 0x1;

    [NativeTypeName("#define DXGI_DEBUG_BINARY_VERSION ( 1 )")]
    public const int DXGI_DEBUG_BINARY_VERSION = (1);

    [NativeTypeName("#define DXGI_INFO_QUEUE_MESSAGE_ID_STRING_FROM_APPLICATION 0")]
    public const int DXGI_INFO_QUEUE_MESSAGE_ID_STRING_FROM_APPLICATION = 0;

    [NativeTypeName("#define DXGI_INFO_QUEUE_DEFAULT_MESSAGE_COUNT_LIMIT 1024")]
    public const int DXGI_INFO_QUEUE_DEFAULT_MESSAGE_COUNT_LIMIT = 1024;
}
