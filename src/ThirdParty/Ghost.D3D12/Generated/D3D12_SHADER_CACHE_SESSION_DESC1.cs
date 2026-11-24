using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.D3D12;

public partial struct D3D12_SHADER_CACHE_SESSION_DESC1
{
    public D3D12_SHADER_CACHE_MODE Mode;

    [NativeTypeName("__AnonymousRecord_d3d12_L31570_C36")]
    public _Anonymous_e__Union Anonymous;

    [UnscopedRef]
    public ref D3D12_SHADER_CACHE_SESSION_MEMORY_DESC Memory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.Memory;
        }
    }

    [UnscopedRef]
    public ref D3D12_SHADER_CACHE_SESSION_DISK_DESC Disk
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.Disk;
        }
    }

    [UnscopedRef]
    public ref D3D12_SHADER_CACHE_SESSION_REGISTERED_DESC Registered
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.Registered;
        }
    }

    [UnscopedRef]
    public ref D3D12_SHADER_CACHE_SESSION_FILE_DESC File
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.File;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public partial struct _Anonymous_e__Union
    {
        [FieldOffset(0)]
        public D3D12_SHADER_CACHE_SESSION_MEMORY_DESC Memory;

        [FieldOffset(0)]
        public D3D12_SHADER_CACHE_SESSION_DISK_DESC Disk;

        [FieldOffset(0)]
        public D3D12_SHADER_CACHE_SESSION_REGISTERED_DESC Registered;

        [FieldOffset(0)]
        public D3D12_SHADER_CACHE_SESSION_FILE_DESC File;
    }
}
