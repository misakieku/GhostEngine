using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.D3D12;

public partial struct D3D12_RAYTRACING_GEOMETRY_DESC
{
    public D3D12_RAYTRACING_GEOMETRY_TYPE Type;

    public D3D12_RAYTRACING_GEOMETRY_FLAGS Flags;

    [NativeTypeName("__AnonymousRecord_d3d12_L15951_C5")]
    public _Anonymous_e__Union Anonymous;

    [UnscopedRef]
    public ref D3D12_RAYTRACING_GEOMETRY_TRIANGLES_DESC Triangles
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.Triangles;
        }
    }

    [UnscopedRef]
    public ref D3D12_RAYTRACING_GEOMETRY_AABBS_DESC AABBs
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.AABBs;
        }
    }

    [UnscopedRef]
    public ref D3D12_RAYTRACING_GEOMETRY_OMM_TRIANGLES_DESC OmmTriangles
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.OmmTriangles;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public partial struct _Anonymous_e__Union
    {
        [FieldOffset(0)]
        public D3D12_RAYTRACING_GEOMETRY_TRIANGLES_DESC Triangles;

        [FieldOffset(0)]
        public D3D12_RAYTRACING_GEOMETRY_AABBS_DESC AABBs;

        [FieldOffset(0)]
        public D3D12_RAYTRACING_GEOMETRY_OMM_TRIANGLES_DESC OmmTriangles;
    }
}
