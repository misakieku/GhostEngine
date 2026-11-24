using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.D3D12;

public partial struct D3D12_RAYTRACING_ACCELERATION_STRUCTURE_POSTBUILD_INFO_SERIALIZATION_DESC
{
    [NativeTypeName("UINT64")]
    public ulong SerializedSizeInBytes;

    [NativeTypeName("__AnonymousRecord_d3d12_L15861_C5")]
    public _Anonymous_e__Union Anonymous;

    [UnscopedRef]
    public ref ulong NumBottomLevelAccelerationStructurePointers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.NumBottomLevelAccelerationStructurePointers;
        }
    }

    [UnscopedRef]
    public ref ulong NumBottomLevelAccelerationStructureHeaderAndPointerListPairs
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.NumBottomLevelAccelerationStructureHeaderAndPointerListPairs;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public partial struct _Anonymous_e__Union
    {
        [FieldOffset(0)]
        [NativeTypeName("UINT64")]
        public ulong NumBottomLevelAccelerationStructurePointers;

        [FieldOffset(0)]
        [NativeTypeName("UINT64")]
        public ulong NumBottomLevelAccelerationStructureHeaderAndPointerListPairs;
    }
}
