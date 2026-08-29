using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    /// <include file='ufbx_edge.xml' path='doc/member[@name="ufbx_edge"]/*' />
    public partial struct ufbx_edge
    {
        /// <include file='ufbx_edge.xml' path='doc/member[@name="ufbx_edge.Anonymous"]/*' />
        [NativeTypeName("__AnonymousRecord_ufbx_L1100_C2")]
        public _Anonymous_e__Union Anonymous;

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.a"]/*' />
        [UnscopedRef]
        public ref uint a
        {
            get
            {
                return ref Anonymous.Anonymous.a;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.b"]/*' />
        [UnscopedRef]
        public ref uint b
        {
            get
            {
                return ref Anonymous.Anonymous.b;
            }
        }

        /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union.indices"]/*' />
        [UnscopedRef]
        public Span<uint> indices
        {
            get
            {
                return Anonymous.indices;
            }
        }

        /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union"]/*' />
        [StructLayout(LayoutKind.Explicit)]
        public partial struct _Anonymous_e__Union
        {
            /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union.Anonymous"]/*' />
            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_ufbx_L1101_C3")]
            public _Anonymous_e__Struct Anonymous;

            /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union.indices"]/*' />
            [FieldOffset(0)]
            [NativeTypeName("uint32_t[2]")]
            public _indices_e__FixedBuffer indices;

            /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct"]/*' />
            public partial struct _Anonymous_e__Struct
            {
                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.a"]/*' />
                [NativeTypeName("uint32_t")]
                public uint a;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.b"]/*' />
                [NativeTypeName("uint32_t")]
                public uint b;
            }

            /// <include file='_indices_e__FixedBuffer.xml' path='doc/member[@name="_indices_e__FixedBuffer"]/*' />
            [InlineArray(2)]
            public partial struct _indices_e__FixedBuffer
            {
                public uint e0;
            }
        }
    }
}
