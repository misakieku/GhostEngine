using System.Runtime.CompilerServices;

namespace Ghost.D3D12;

public partial struct D3D12_PROGRAM_IDENTIFIER
{
    [NativeTypeName("UINT64[4]")]
    public _OpaqueData_e__FixedBuffer OpaqueData;

    [InlineArray(4)]
    public partial struct _OpaqueData_e__FixedBuffer
    {
        public ulong e0;
    }
}
