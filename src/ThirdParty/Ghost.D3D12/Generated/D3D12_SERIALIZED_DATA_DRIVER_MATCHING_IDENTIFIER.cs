using System.Runtime.CompilerServices;

namespace Ghost.D3D12;

public partial struct D3D12_SERIALIZED_DATA_DRIVER_MATCHING_IDENTIFIER
{
    public Guid DriverOpaqueGUID;

    [NativeTypeName("BYTE[16]")]
    public _DriverOpaqueVersioningData_e__FixedBuffer DriverOpaqueVersioningData;

    [InlineArray(16)]
    public partial struct _DriverOpaqueVersioningData_e__FixedBuffer
    {
        public byte e0;
    }
}
