using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.D3D12;

public unsafe partial struct D3D12_TEXTURE_COPY_DESC
{
    public D3D12_TEXTURE_COPY_DESC_TYPE Type;

    public uint NumRegions;

    [NativeTypeName("const D3D12_TEXTURE_COPY_REGION *")]
    public D3D12_TEXTURE_COPY_REGION* pCopyRegions;

    [NativeTypeName("__AnonymousRecord_d3d12_L42861_C5")]
    public _Anonymous_e__Union Anonymous;

    [UnscopedRef]
    public ref D3D12_TEXTURE_TEXTURE_COPY_DESC TextureTextureCopy
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.TextureTextureCopy;
        }
    }

    [UnscopedRef]
    public ref D3D12_BUFFER_TEXTURE_COPY_DESC BufferTextureCopy
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.BufferTextureCopy;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public partial struct _Anonymous_e__Union
    {
        [FieldOffset(0)]
        public D3D12_TEXTURE_TEXTURE_COPY_DESC TextureTextureCopy;

        [FieldOffset(0)]
        public D3D12_BUFFER_TEXTURE_COPY_DESC BufferTextureCopy;
    }
}
