using System.Runtime.CompilerServices;

namespace Ghost.DXGI;

public partial struct DXGI_HDR_METADATA_HDR10
{
    [NativeTypeName("UINT16[2]")]
    public _RedPrimary_e__FixedBuffer RedPrimary;

    [NativeTypeName("UINT16[2]")]
    public _GreenPrimary_e__FixedBuffer GreenPrimary;

    [NativeTypeName("UINT16[2]")]
    public _BluePrimary_e__FixedBuffer BluePrimary;

    [NativeTypeName("UINT16[2]")]
    public _WhitePoint_e__FixedBuffer WhitePoint;

    public uint MaxMasteringLuminance;

    public uint MinMasteringLuminance;

    [NativeTypeName("UINT16")]
    public ushort MaxContentLightLevel;

    [NativeTypeName("UINT16")]
    public ushort MaxFrameAverageLightLevel;

    [InlineArray(2)]
    public partial struct _RedPrimary_e__FixedBuffer
    {
        public ushort e0;
    }

    [InlineArray(2)]
    public partial struct _GreenPrimary_e__FixedBuffer
    {
        public ushort e0;
    }

    [InlineArray(2)]
    public partial struct _BluePrimary_e__FixedBuffer
    {
        public ushort e0;
    }

    [InlineArray(2)]
    public partial struct _WhitePoint_e__FixedBuffer
    {
        public ushort e0;
    }
}
