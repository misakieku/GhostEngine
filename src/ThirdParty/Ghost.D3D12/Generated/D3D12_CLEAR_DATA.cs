using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.D3D12;

public partial struct D3D12_CLEAR_DATA
{
    [NativeTypeName("__AnonymousRecord_d3d12_L42815_C5")]
    public _Anonymous_e__Union Anonymous;

    [UnscopedRef]
    public Span<float> Floats
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return Anonymous.Floats;
        }
    }

    [UnscopedRef]
    public Span<uint> Uints
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return Anonymous.Uints;
        }
    }

    [UnscopedRef]
    public Span<int> Sints
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return Anonymous.Sints;
        }
    }

    [UnscopedRef]
    public Span<byte> Bytes
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return Anonymous.Bytes;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public partial struct _Anonymous_e__Union
    {
        [FieldOffset(0)]
        [NativeTypeName("FLOAT[4]")]
        public _Floats_e__FixedBuffer Floats;

        [FieldOffset(0)]
        [NativeTypeName("UINT[4]")]
        public _Uints_e__FixedBuffer Uints;

        [FieldOffset(0)]
        [NativeTypeName("INT[4]")]
        public _Sints_e__FixedBuffer Sints;

        [FieldOffset(0)]
        [NativeTypeName("BYTE[16]")]
        public _Bytes_e__FixedBuffer Bytes;

        [InlineArray(4)]
        public partial struct _Floats_e__FixedBuffer
        {
            public float e0;
        }

        [InlineArray(4)]
        public partial struct _Uints_e__FixedBuffer
        {
            public uint e0;
        }

        [InlineArray(4)]
        public partial struct _Sints_e__FixedBuffer
        {
            public int e0;
        }

        [InlineArray(16)]
        public partial struct _Bytes_e__FixedBuffer
        {
            public byte e0;
        }
    }
}
