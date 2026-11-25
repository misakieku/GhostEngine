using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.D3D12;

public partial struct D3D12_RENDER_PASS_BEGINNING_ACCESS
{
    public D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE Type;

    [NativeTypeName("__AnonymousRecord_d3d12_L22376_C5")]
    public _Anonymous_e__Union Anonymous;

    [UnscopedRef]
    public ref D3D12_RENDER_PASS_BEGINNING_ACCESS_CLEAR_PARAMETERS Clear
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.Clear;
        }
    }

    [UnscopedRef]
    public ref D3D12_RENDER_PASS_BEGINNING_ACCESS_PRESERVE_LOCAL_PARAMETERS PreserveLocal
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.PreserveLocal;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public partial struct _Anonymous_e__Union
    {
        [FieldOffset(0)]
        public D3D12_RENDER_PASS_BEGINNING_ACCESS_CLEAR_PARAMETERS Clear;

        [FieldOffset(0)]
        public D3D12_RENDER_PASS_BEGINNING_ACCESS_PRESERVE_LOCAL_PARAMETERS PreserveLocal;
    }
}
