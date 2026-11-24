using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.D3D12;

public partial struct D3D12_SET_PROGRAM_DESC
{
    public D3D12_PROGRAM_TYPE Type;

    [NativeTypeName("__AnonymousRecord_d3d12_L22669_C5")]
    public _Anonymous_e__Union Anonymous;

    [UnscopedRef]
    public ref D3D12_SET_GENERIC_PIPELINE_DESC GenericPipeline
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.GenericPipeline;
        }
    }

    [UnscopedRef]
    public ref D3D12_SET_RAYTRACING_PIPELINE_DESC RaytracingPipeline
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.RaytracingPipeline;
        }
    }

    [UnscopedRef]
    public ref D3D12_SET_WORK_GRAPH_DESC WorkGraph
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.WorkGraph;
        }
    }

    [UnscopedRef]
    public ref D3D12_SET_MLIR_PROGRAM_DESC MlirProgram
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.MlirProgram;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public partial struct _Anonymous_e__Union
    {
        [FieldOffset(0)]
        public D3D12_SET_GENERIC_PIPELINE_DESC GenericPipeline;

        [FieldOffset(0)]
        public D3D12_SET_RAYTRACING_PIPELINE_DESC RaytracingPipeline;

        [FieldOffset(0)]
        public D3D12_SET_WORK_GRAPH_DESC WorkGraph;

        [FieldOffset(0)]
        public D3D12_SET_MLIR_PROGRAM_DESC MlirProgram;
    }
}
