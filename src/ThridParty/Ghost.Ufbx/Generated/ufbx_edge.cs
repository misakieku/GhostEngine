using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    public partial struct ufbx_edge
    {
        [NativeTypeName("__AnonymousRecord_ufbx_L1096_C2")]
        public _Anonymous_e__Union Anonymous;

        [UnscopedRef]
        public ref uint a
        {
            get
            {
                return ref Anonymous.Anonymous.a;
            }
        }

        [UnscopedRef]
        public ref uint b
        {
            get
            {
                return ref Anonymous.Anonymous.b;
            }
        }

        [UnscopedRef]
        public Span<uint> indices
        {
            get
            {
                return Anonymous.indices;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_ufbx_L1097_C3")]
            public _Anonymous_e__Struct Anonymous;

            [FieldOffset(0)]
            [NativeTypeName("uint32_t[2]")]
            public _indices_e__FixedBuffer indices;

            public partial struct _Anonymous_e__Struct
            {
                [NativeTypeName("uint32_t")]
                public uint a;

                [NativeTypeName("uint32_t")]
                public uint b;
            }

            [InlineArray(2)]
            public partial struct _indices_e__FixedBuffer
            {
                public uint e0;
            }
        }
    }
}
