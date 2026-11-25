using System.Runtime.CompilerServices;

namespace Ghost.DXGI;

public partial struct DXGI_ADAPTER_DESC1
{
    [NativeTypeName("WCHAR[128]")]
    public _Description_e__FixedBuffer Description;

    public uint VendorId;

    public uint DeviceId;

    public uint SubSysId;

    public uint Revision;

    [NativeTypeName("SIZE_T")]
    public nuint DedicatedVideoMemory;

    [NativeTypeName("SIZE_T")]
    public nuint DedicatedSystemMemory;

    [NativeTypeName("SIZE_T")]
    public nuint SharedSystemMemory;

    public LUID AdapterLuid;

    public uint Flags;

    [InlineArray(128)]
    public partial struct _Description_e__FixedBuffer
    {
        public char e0;
    }
}
