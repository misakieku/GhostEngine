using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.D3D12;

public partial struct D3D12_MLIR_PROGRAM_BINDING
{
    public uint BindPointIndex;

    public D3D12_MLIR_PROGRAM_BINDING_FLAGS Flags;

    public D3D12_MLIR_PROGRAM_BINDING_TYPE Type;

    [NativeTypeName("__AnonymousRecord_d3d12_L22743_C5")]
    public _Anonymous_e__Union Anonymous;

    [UnscopedRef]
    public ref D3D12_GPU_VIRTUAL_ADDRESS_RANGE GpuVirtualAddressBinding
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.GpuVirtualAddressBinding;
        }
    }

    [UnscopedRef]
    public ref D3D12_GPU_DESCRIPTOR_HANDLE GpuDescriptorBinding
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.GpuDescriptorBinding;
        }
    }

    [UnscopedRef]
    public ref D3D12_MLIR_PROGRAM_CPU_BINDING CpuBinding
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.CpuBinding;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public partial struct _Anonymous_e__Union
    {
        [FieldOffset(0)]
        public D3D12_GPU_VIRTUAL_ADDRESS_RANGE GpuVirtualAddressBinding;

        [FieldOffset(0)]
        public D3D12_GPU_DESCRIPTOR_HANDLE GpuDescriptorBinding;

        [FieldOffset(0)]
        public D3D12_MLIR_PROGRAM_CPU_BINDING CpuBinding;
    }
}
