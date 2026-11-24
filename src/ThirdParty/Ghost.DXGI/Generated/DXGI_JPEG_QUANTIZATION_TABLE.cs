using System.Runtime.CompilerServices;

namespace Ghost.DXGI;

public partial struct DXGI_JPEG_QUANTIZATION_TABLE
{
    [NativeTypeName("BYTE[64]")]
    public _Elements_e__FixedBuffer Elements;

    [InlineArray(64)]
    public partial struct _Elements_e__FixedBuffer
    {
        public byte e0;
    }
}
