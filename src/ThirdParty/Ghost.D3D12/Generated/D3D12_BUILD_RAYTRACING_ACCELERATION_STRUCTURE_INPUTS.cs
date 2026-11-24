using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.D3D12;

public unsafe partial struct D3D12_BUILD_RAYTRACING_ACCELERATION_STRUCTURE_INPUTS
{
    public D3D12_RAYTRACING_ACCELERATION_STRUCTURE_TYPE Type;

    public D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAGS Flags;

    public uint NumDescs;

    public D3D12_ELEMENTS_LAYOUT DescsLayout;

    [NativeTypeName("__AnonymousRecord_d3d12_L15980_C5")]
    public _Anonymous_e__Union Anonymous;

    [UnscopedRef]
    public ref ulong InstanceDescs
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.InstanceDescs;
        }
    }

    [UnscopedRef]
    public ref D3D12_RAYTRACING_GEOMETRY_DESC* pGeometryDescs
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.pGeometryDescs;
        }
    }

    [UnscopedRef]
    public ref D3D12_RAYTRACING_GEOMETRY_DESC** ppGeometryDescs
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.ppGeometryDescs;
        }
    }

    [UnscopedRef]
    public ref D3D12_RAYTRACING_OPACITY_MICROMAP_ARRAY_DESC* pOpacityMicromapArrayDesc
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.pOpacityMicromapArrayDesc;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe partial struct _Anonymous_e__Union
    {
        [FieldOffset(0)]
        [NativeTypeName("D3D12_GPU_VIRTUAL_ADDRESS")]
        public ulong InstanceDescs;

        [FieldOffset(0)]
        [NativeTypeName("const D3D12_RAYTRACING_GEOMETRY_DESC *")]
        public D3D12_RAYTRACING_GEOMETRY_DESC* pGeometryDescs;

        [FieldOffset(0)]
        [NativeTypeName("const D3D12_RAYTRACING_GEOMETRY_DESC *const *")]
        public D3D12_RAYTRACING_GEOMETRY_DESC** ppGeometryDescs;

        [FieldOffset(0)]
        [NativeTypeName("const D3D12_RAYTRACING_OPACITY_MICROMAP_ARRAY_DESC *")]
        public D3D12_RAYTRACING_OPACITY_MICROMAP_ARRAY_DESC* pOpacityMicromapArrayDesc;
    }
}
