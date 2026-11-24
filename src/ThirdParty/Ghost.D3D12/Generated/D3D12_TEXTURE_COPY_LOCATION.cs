using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.D3D12;

public unsafe partial struct D3D12_TEXTURE_COPY_LOCATION
{
    public ID3D12Resource* pResource;

    public D3D12_TEXTURE_COPY_TYPE Type;

    [NativeTypeName("__AnonymousRecord_d3d12_L3772_C5")]
    public _Anonymous_e__Union Anonymous;

    [UnscopedRef]
    public ref D3D12_PLACED_SUBRESOURCE_FOOTPRINT PlacedFootprint
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.PlacedFootprint;
        }
    }

    [UnscopedRef]
    public ref uint SubresourceIndex
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.SubresourceIndex;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public partial struct _Anonymous_e__Union
    {
        [FieldOffset(0)]
        public D3D12_PLACED_SUBRESOURCE_FOOTPRINT PlacedFootprint;

        [FieldOffset(0)]
        public uint SubresourceIndex;
    }
}
