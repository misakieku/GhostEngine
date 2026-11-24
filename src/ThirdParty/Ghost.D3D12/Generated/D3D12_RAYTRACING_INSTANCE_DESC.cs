using System.Runtime.CompilerServices;

namespace Ghost.D3D12;

public partial struct D3D12_RAYTRACING_INSTANCE_DESC
{
    [NativeTypeName("FLOAT[3][4]")]
    public _Transform_e__FixedBuffer Transform;

    public uint _bitfield1;

    [NativeTypeName("uint : 24")]
    public uint InstanceID
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get
        {
            return _bitfield1 & 0xFFFFFFu;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            _bitfield1 = (_bitfield1 & ~0xFFFFFFu) | (value & 0xFFFFFFu);
        }
    }

    [NativeTypeName("uint : 8")]
    public uint InstanceMask
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get
        {
            return (_bitfield1 >> 24) & 0xFFu;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            _bitfield1 = (_bitfield1 & ~(0xFFu << 24)) | ((value & 0xFFu) << 24);
        }
    }

    public uint _bitfield2;

    [NativeTypeName("uint : 24")]
    public uint InstanceContributionToHitGroupIndex
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get
        {
            return _bitfield2 & 0xFFFFFFu;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            _bitfield2 = (_bitfield2 & ~0xFFFFFFu) | (value & 0xFFFFFFu);
        }
    }

    [NativeTypeName("uint : 8")]
    public uint Flags
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get
        {
            return (_bitfield2 >> 24) & 0xFFu;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            _bitfield2 = (_bitfield2 & ~(0xFFu << 24)) | ((value & 0xFFu) << 24);
        }
    }

    [NativeTypeName("D3D12_GPU_VIRTUAL_ADDRESS")]
    public ulong AccelerationStructure;

    [InlineArray(3 * 4)]
    public partial struct _Transform_e__FixedBuffer
    {
        public float e0_0;
    }
}
