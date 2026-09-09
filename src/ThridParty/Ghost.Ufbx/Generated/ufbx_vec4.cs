using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    /// <include file='ufbx_vec4.xml' path='doc/member[@name="ufbx_vec4"]/*' />
    public partial struct ufbx_vec4
    {
        /// <include file='ufbx_vec4.xml' path='doc/member[@name="ufbx_vec4.Anonymous"]/*' />
        [NativeTypeName("__AnonymousRecord_ufbx_L319_C2")]
        public _Anonymous_e__Union Anonymous;

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.x"]/*' />
        [UnscopedRef]
        public ref float x
        {
            get
            {
                return ref Anonymous.Anonymous.x;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.y"]/*' />
        [UnscopedRef]
        public ref float y
        {
            get
            {
                return ref Anonymous.Anonymous.y;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.z"]/*' />
        [UnscopedRef]
        public ref float z
        {
            get
            {
                return ref Anonymous.Anonymous.z;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.w"]/*' />
        [UnscopedRef]
        public ref float w
        {
            get
            {
                return ref Anonymous.Anonymous.w;
            }
        }

        /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union.v"]/*' />
        [UnscopedRef]
        public Span<float> v
        {
            get
            {
                return Anonymous.v;
            }
        }

        /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union"]/*' />
        [StructLayout(LayoutKind.Explicit)]
        public partial struct _Anonymous_e__Union
        {
            /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union.Anonymous"]/*' />
            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_ufbx_L320_C3")]
            public _Anonymous_e__Struct Anonymous;

            /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union.v"]/*' />
            [FieldOffset(0)]
            [NativeTypeName("ufbx_real[4]")]
            public _v_e__FixedBuffer v;

            /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct"]/*' />
            public partial struct _Anonymous_e__Struct
            {
                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.x"]/*' />
                [NativeTypeName("ufbx_real")]
                public float x;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.y"]/*' />
                [NativeTypeName("ufbx_real")]
                public float y;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.z"]/*' />
                [NativeTypeName("ufbx_real")]
                public float z;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.w"]/*' />
                [NativeTypeName("ufbx_real")]
                public float w;
            }

            /// <include file='_v_e__FixedBuffer.xml' path='doc/member[@name="_v_e__FixedBuffer"]/*' />
            [InlineArray(4)]
            public partial struct _v_e__FixedBuffer
            {
                public float e0;
            }
        }
    }
}
