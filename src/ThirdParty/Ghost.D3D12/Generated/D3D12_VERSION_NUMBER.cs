using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.D3D12;

[StructLayout(LayoutKind.Explicit)]
public partial struct D3D12_VERSION_NUMBER
{
    [FieldOffset(0)]
    [NativeTypeName("UINT64")]
    public ulong Version;

    [FieldOffset(0)]
    [NativeTypeName("struct D3D12_VERSION_NUMBER_COMPONENTS")]
    public D3D12_VERSION_NUMBER_COMPONENTS Components;

    [FieldOffset(0)]
    [NativeTypeName("UINT16[4]")]
    public _VersionParts_e__FixedBuffer VersionParts;

    public partial struct D3D12_VERSION_NUMBER_COMPONENTS
    {
        [NativeTypeName("UINT16")]
        public ushort Revision;

        [NativeTypeName("UINT16")]
        public ushort Build;

        [NativeTypeName("UINT16")]
        public ushort Minor;

        [NativeTypeName("UINT16")]
        public ushort Major;
    }

    [InlineArray(4)]
    public partial struct _VersionParts_e__FixedBuffer
    {
        public ushort e0;
    }
}
