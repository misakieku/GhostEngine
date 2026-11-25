using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.D3D12;

public partial struct D3D12_DEPTH_STENCIL_VIEW_DESC
{
    public DXGI_FORMAT Format;

    public D3D12_DSV_DIMENSION ViewDimension;

    public D3D12_DSV_FLAGS Flags;

    [NativeTypeName("__AnonymousRecord_d3d12_L4391_C5")]
    public _Anonymous_e__Union Anonymous;

    [UnscopedRef]
    public ref D3D12_TEX1D_DSV Texture1D
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.Texture1D;
        }
    }

    [UnscopedRef]
    public ref D3D12_TEX1D_ARRAY_DSV Texture1DArray
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.Texture1DArray;
        }
    }

    [UnscopedRef]
    public ref D3D12_TEX2D_DSV Texture2D
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.Texture2D;
        }
    }

    [UnscopedRef]
    public ref D3D12_TEX2D_ARRAY_DSV Texture2DArray
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.Texture2DArray;
        }
    }

    [UnscopedRef]
    public ref D3D12_TEX2DMS_DSV Texture2DMS
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.Texture2DMS;
        }
    }

    [UnscopedRef]
    public ref D3D12_TEX2DMS_ARRAY_DSV Texture2DMSArray
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.Texture2DMSArray;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public partial struct _Anonymous_e__Union
    {
        [FieldOffset(0)]
        public D3D12_TEX1D_DSV Texture1D;

        [FieldOffset(0)]
        public D3D12_TEX1D_ARRAY_DSV Texture1DArray;

        [FieldOffset(0)]
        public D3D12_TEX2D_DSV Texture2D;

        [FieldOffset(0)]
        public D3D12_TEX2D_ARRAY_DSV Texture2DArray;

        [FieldOffset(0)]
        public D3D12_TEX2DMS_DSV Texture2DMS;

        [FieldOffset(0)]
        public D3D12_TEX2DMS_ARRAY_DSV Texture2DMSArray;
    }
}
