using System.Runtime.CompilerServices;

namespace Ghost.DXGI;

public partial struct DXGI_JPEG_DC_HUFFMAN_TABLE
{
    [NativeTypeName("BYTE[12]")]
    public _CodeCounts_e__FixedBuffer CodeCounts;

    [NativeTypeName("BYTE[12]")]
    public _CodeValues_e__FixedBuffer CodeValues;

    [InlineArray(12)]
    public partial struct _CodeCounts_e__FixedBuffer
    {
        public byte e0;
    }

    [InlineArray(12)]
    public partial struct _CodeValues_e__FixedBuffer
    {
        public byte e0;
    }
}
