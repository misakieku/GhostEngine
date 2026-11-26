using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.D3D12;

public partial struct D3D12_CLEAR_VALUE
{
    public DXGI_FORMAT Format;

    [NativeTypeName("__AnonymousRecord_d3d12_L3560_C5")]
    public _Anonymous_e__Union Anonymous;

    [UnscopedRef]
    public Span<float> Color
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return Anonymous.Color;
        }
    }

    [UnscopedRef]
    public ref D3D12_DEPTH_STENCIL_VALUE DepthStencil
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.DepthStencil;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public partial struct _Anonymous_e__Union
    {
        [FieldOffset(0)]
        [NativeTypeName("FLOAT[4]")]
        public _Color_e__FixedBuffer Color;

        [FieldOffset(0)]
        public D3D12_DEPTH_STENCIL_VALUE DepthStencil;

        [InlineArray(4)]
        public partial struct _Color_e__FixedBuffer
        {
            public float e0;
        }
    }
}
