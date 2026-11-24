using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.D3D12;

public partial struct D3D12_DISPATCH_GRAPH_DESC
{
    public D3D12_DISPATCH_MODE Mode;

    [NativeTypeName("__AnonymousRecord_d3d12_L22760_C5")]
    public _Anonymous_e__Union Anonymous;

    [UnscopedRef]
    public ref D3D12_NODE_CPU_INPUT NodeCPUInput
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.NodeCPUInput;
        }
    }

    [UnscopedRef]
    public ref ulong NodeGPUInput
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.NodeGPUInput;
        }
    }

    [UnscopedRef]
    public ref D3D12_MULTI_NODE_CPU_INPUT MultiNodeCPUInput
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.MultiNodeCPUInput;
        }
    }

    [UnscopedRef]
    public ref ulong MultiNodeGPUInput
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.MultiNodeGPUInput;
        }
    }

    [UnscopedRef]
    public ref D3D12_MLIR_PROGRAM_BINDINGS MlirProgramBindings
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.MlirProgramBindings;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public partial struct _Anonymous_e__Union
    {
        [FieldOffset(0)]
        public D3D12_NODE_CPU_INPUT NodeCPUInput;

        [FieldOffset(0)]
        [NativeTypeName("D3D12_GPU_VIRTUAL_ADDRESS")]
        public ulong NodeGPUInput;

        [FieldOffset(0)]
        public D3D12_MULTI_NODE_CPU_INPUT MultiNodeCPUInput;

        [FieldOffset(0)]
        [NativeTypeName("D3D12_GPU_VIRTUAL_ADDRESS")]
        public ulong MultiNodeGPUInput;

        [FieldOffset(0)]
        public D3D12_MLIR_PROGRAM_BINDINGS MlirProgramBindings;
    }
}
