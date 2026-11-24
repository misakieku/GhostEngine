using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.D3D12;

public partial struct D3D12_RESOURCE_BARRIER
{
    public D3D12_RESOURCE_BARRIER_TYPE Type;

    public D3D12_RESOURCE_BARRIER_FLAGS Flags;

    [NativeTypeName("__AnonymousRecord_d3d12_L3738_C5")]
    public _Anonymous_e__Union Anonymous;

    [UnscopedRef]
    public ref D3D12_RESOURCE_TRANSITION_BARRIER Transition
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.Transition;
        }
    }

    [UnscopedRef]
    public ref D3D12_RESOURCE_ALIASING_BARRIER Aliasing
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.Aliasing;
        }
    }

    [UnscopedRef]
    public ref D3D12_RESOURCE_UAV_BARRIER UAV
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.UAV;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public partial struct _Anonymous_e__Union
    {
        [FieldOffset(0)]
        public D3D12_RESOURCE_TRANSITION_BARRIER Transition;

        [FieldOffset(0)]
        public D3D12_RESOURCE_ALIASING_BARRIER Aliasing;

        [FieldOffset(0)]
        public D3D12_RESOURCE_UAV_BARRIER UAV;
    }
}
