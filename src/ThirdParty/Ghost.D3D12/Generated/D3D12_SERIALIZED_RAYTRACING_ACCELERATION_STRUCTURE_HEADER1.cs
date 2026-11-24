using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.D3D12;

public partial struct D3D12_SERIALIZED_RAYTRACING_ACCELERATION_STRUCTURE_HEADER1
{
    public D3D12_SERIALIZED_DATA_DRIVER_MATCHING_IDENTIFIER DriverMatchingIdentifier;

    [NativeTypeName("UINT64")]
    public ulong SerializedSizeInBytesIncludingHeader;

    [NativeTypeName("UINT64")]
    public ulong DeserializedSizeInBytes;

    [NativeTypeName("__AnonymousRecord_d3d12_L15912_C5")]
    public _Anonymous_e__Union Anonymous;

    public D3D12_SERIALIZED_RAYTRACING_ACCELERATION_STRUCTURE_HEADER_POSTAMBLE_TYPE HeaderPostambleType;

    [UnscopedRef]
    public ref uint NumBottomLevelAccelerationStructurePointersAfterHeader
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.NumBottomLevelAccelerationStructurePointersAfterHeader;
        }
    }

    [UnscopedRef]
    public ref uint NumBlocks
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.NumBlocks;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public partial struct _Anonymous_e__Union
    {
        [FieldOffset(0)]
        public uint NumBottomLevelAccelerationStructurePointersAfterHeader;

        [FieldOffset(0)]
        public uint NumBlocks;
    }
}
