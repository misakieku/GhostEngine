using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.D3D12;

public partial struct D3D12_VERSIONED_DEVICE_REMOVED_EXTENDED_DATA
{
    public D3D12_DRED_VERSION Version;

    [NativeTypeName("__AnonymousRecord_d3d12_L17224_C5")]
    public _Anonymous_e__Union Anonymous;

    [UnscopedRef]
    public ref D3D12_DEVICE_REMOVED_EXTENDED_DATA Dred_1_0
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.Dred_1_0;
        }
    }

    [UnscopedRef]
    public ref D3D12_DEVICE_REMOVED_EXTENDED_DATA1 Dred_1_1
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.Dred_1_1;
        }
    }

    [UnscopedRef]
    public ref D3D12_DEVICE_REMOVED_EXTENDED_DATA2 Dred_1_2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.Dred_1_2;
        }
    }

    [UnscopedRef]
    public ref D3D12_DEVICE_REMOVED_EXTENDED_DATA3 Dred_1_3
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.Dred_1_3;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public partial struct _Anonymous_e__Union
    {
        [FieldOffset(0)]
        public D3D12_DEVICE_REMOVED_EXTENDED_DATA Dred_1_0;

        [FieldOffset(0)]
        public D3D12_DEVICE_REMOVED_EXTENDED_DATA1 Dred_1_1;

        [FieldOffset(0)]
        public D3D12_DEVICE_REMOVED_EXTENDED_DATA2 Dred_1_2;

        [FieldOffset(0)]
        public D3D12_DEVICE_REMOVED_EXTENDED_DATA3 Dred_1_3;
    }
}
