using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.D3D12;

public partial struct D3D12_ROOT_PARAMETER1
{
    public D3D12_ROOT_PARAMETER_TYPE ParameterType;

    [NativeTypeName("__AnonymousRecord_d3d12_L4641_C5")]
    public _Anonymous_e__Union Anonymous;

    public D3D12_SHADER_VISIBILITY ShaderVisibility;

    [UnscopedRef]
    public ref D3D12_ROOT_DESCRIPTOR_TABLE1 DescriptorTable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.DescriptorTable;
        }
    }

    [UnscopedRef]
    public ref D3D12_ROOT_CONSTANTS Constants
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.Constants;
        }
    }

    [UnscopedRef]
    public ref D3D12_ROOT_DESCRIPTOR1 Descriptor
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.Descriptor;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public partial struct _Anonymous_e__Union
    {
        [FieldOffset(0)]
        public D3D12_ROOT_DESCRIPTOR_TABLE1 DescriptorTable;

        [FieldOffset(0)]
        public D3D12_ROOT_CONSTANTS Constants;

        [FieldOffset(0)]
        public D3D12_ROOT_DESCRIPTOR1 Descriptor;
    }
}
