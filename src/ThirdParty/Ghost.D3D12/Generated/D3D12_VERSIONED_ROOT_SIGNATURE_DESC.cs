using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.D3D12;

public partial struct D3D12_VERSIONED_ROOT_SIGNATURE_DESC
{
    public D3D_ROOT_SIGNATURE_VERSION Version;

    [NativeTypeName("__AnonymousRecord_d3d12_L4671_C5")]
    public _Anonymous_e__Union Anonymous;

    [UnscopedRef]
    public ref D3D12_ROOT_SIGNATURE_DESC Desc_1_0
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.Desc_1_0;
        }
    }

    [UnscopedRef]
    public ref D3D12_ROOT_SIGNATURE_DESC1 Desc_1_1
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.Desc_1_1;
        }
    }

    [UnscopedRef]
    public ref D3D12_ROOT_SIGNATURE_DESC2 Desc_1_2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.Desc_1_2;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public partial struct _Anonymous_e__Union
    {
        [FieldOffset(0)]
        public D3D12_ROOT_SIGNATURE_DESC Desc_1_0;

        [FieldOffset(0)]
        public D3D12_ROOT_SIGNATURE_DESC1 Desc_1_1;

        [FieldOffset(0)]
        public D3D12_ROOT_SIGNATURE_DESC2 Desc_1_2;
    }
}
