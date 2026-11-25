using System.Runtime.CompilerServices;

namespace Ghost.DXGI;

public partial struct DXGI_JPEG_AC_HUFFMAN_TABLE
{
    [NativeTypeName("BYTE[16]")]
    public _CodeCounts_e__FixedBuffer CodeCounts;

    [NativeTypeName("BYTE[162]")]
    public _CodeValues_e__FixedBuffer CodeValues;

    [InlineArray(16)]
    public partial struct _CodeCounts_e__FixedBuffer
    {
        public byte e0;
    }

    [InlineArray(162)]
    public partial struct _CodeValues_e__FixedBuffer
    {
        public byte e0;
    }
}
