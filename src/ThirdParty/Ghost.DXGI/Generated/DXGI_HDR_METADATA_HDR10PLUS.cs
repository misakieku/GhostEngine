using System.Runtime.CompilerServices;

namespace Ghost.DXGI;

public partial struct DXGI_HDR_METADATA_HDR10PLUS
{
    [NativeTypeName("BYTE[72]")]
    public _Data_e__FixedBuffer Data;

    [InlineArray(72)]
    public partial struct _Data_e__FixedBuffer
    {
        public byte e0;
    }
}
