using System.Runtime.CompilerServices;

namespace Ghost.D3D12;

public partial struct D3D12_RAYTRACING_OPACITY_MICROMAP_DESC
{
    public uint ByteOffset;

    public uint _bitfield;

    [NativeTypeName("uint : 16")]
    public uint SubdivisionLevel
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get
        {
            return _bitfield & 0xFFFFu;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            _bitfield = (_bitfield & ~0xFFFFu) | (value & 0xFFFFu);
        }
    }

    [NativeTypeName("D3D12_RAYTRACING_OPACITY_MICROMAP_FORMAT : 16")]
    public D3D12_RAYTRACING_OPACITY_MICROMAP_FORMAT Format
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get
        {
            return (D3D12_RAYTRACING_OPACITY_MICROMAP_FORMAT)((_bitfield << 0) >> 16);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            _bitfield = (_bitfield & ~(0xFFFFu << 16)) | (((uint)(value) & 0xFFFF) << 16);
        }
    }
}
