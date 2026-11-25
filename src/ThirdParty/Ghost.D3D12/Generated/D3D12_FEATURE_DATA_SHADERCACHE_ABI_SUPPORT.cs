using System.Runtime.CompilerServices;

namespace Ghost.D3D12;

public partial struct D3D12_FEATURE_DATA_SHADERCACHE_ABI_SUPPORT
{
    [NativeTypeName("WCHAR[128]")]
    public _szAdapterFamily_e__FixedBuffer szAdapterFamily;

    [NativeTypeName("UINT64")]
    public ulong MinimumABISupportVersion;

    [NativeTypeName("UINT64")]
    public ulong MaximumABISupportVersion;

    public D3D12_VERSION_NUMBER CompilerVersion;

    public D3D12_VERSION_NUMBER ApplicationProfileVersion;

    [InlineArray(128)]
    public partial struct _szAdapterFamily_e__FixedBuffer
    {
        public char e0;
    }
}
