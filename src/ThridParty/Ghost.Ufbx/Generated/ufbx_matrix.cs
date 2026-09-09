using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    /// <include file='ufbx_matrix.xml' path='doc/member[@name="ufbx_matrix"]/*' />
    public partial struct ufbx_matrix
    {
        /// <include file='ufbx_matrix.xml' path='doc/member[@name="ufbx_matrix.Anonymous"]/*' />
        [NativeTypeName("__AnonymousRecord_ufbx_L368_C2")]
        public _Anonymous_e__Union Anonymous;

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.m00"]/*' />
        [UnscopedRef]
        public ref float m00
        {
            get
            {
                return ref Anonymous.Anonymous.m00;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.m10"]/*' />
        [UnscopedRef]
        public ref float m10
        {
            get
            {
                return ref Anonymous.Anonymous.m10;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.m20"]/*' />
        [UnscopedRef]
        public ref float m20
        {
            get
            {
                return ref Anonymous.Anonymous.m20;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.m01"]/*' />
        [UnscopedRef]
        public ref float m01
        {
            get
            {
                return ref Anonymous.Anonymous.m01;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.m11"]/*' />
        [UnscopedRef]
        public ref float m11
        {
            get
            {
                return ref Anonymous.Anonymous.m11;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.m21"]/*' />
        [UnscopedRef]
        public ref float m21
        {
            get
            {
                return ref Anonymous.Anonymous.m21;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.m02"]/*' />
        [UnscopedRef]
        public ref float m02
        {
            get
            {
                return ref Anonymous.Anonymous.m02;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.m12"]/*' />
        [UnscopedRef]
        public ref float m12
        {
            get
            {
                return ref Anonymous.Anonymous.m12;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.m22"]/*' />
        [UnscopedRef]
        public ref float m22
        {
            get
            {
                return ref Anonymous.Anonymous.m22;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.m03"]/*' />
        [UnscopedRef]
        public ref float m03
        {
            get
            {
                return ref Anonymous.Anonymous.m03;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.m13"]/*' />
        [UnscopedRef]
        public ref float m13
        {
            get
            {
                return ref Anonymous.Anonymous.m13;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.m23"]/*' />
        [UnscopedRef]
        public ref float m23
        {
            get
            {
                return ref Anonymous.Anonymous.m23;
            }
        }

        /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union.cols"]/*' />
        [UnscopedRef]
        public Span<ufbx_vec3> cols
        {
            get
            {
                return Anonymous.cols;
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
            [NativeTypeName("__AnonymousRecord_ufbx_L369_C3")]
            public _Anonymous_e__Struct Anonymous;

            /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union.cols"]/*' />
            [FieldOffset(0)]
            [NativeTypeName("ufbx_vec3[4]")]
            public _cols_e__FixedBuffer cols;

            /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union.v"]/*' />
            [FieldOffset(0)]
            [NativeTypeName("ufbx_real[12]")]
            public _v_e__FixedBuffer v;

            /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct"]/*' />
            public partial struct _Anonymous_e__Struct
            {
                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.m00"]/*' />
                [NativeTypeName("ufbx_real")]
                public float m00;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.m10"]/*' />
                [NativeTypeName("ufbx_real")]
                public float m10;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.m20"]/*' />
                [NativeTypeName("ufbx_real")]
                public float m20;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.m01"]/*' />
                [NativeTypeName("ufbx_real")]
                public float m01;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.m11"]/*' />
                [NativeTypeName("ufbx_real")]
                public float m11;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.m21"]/*' />
                [NativeTypeName("ufbx_real")]
                public float m21;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.m02"]/*' />
                [NativeTypeName("ufbx_real")]
                public float m02;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.m12"]/*' />
                [NativeTypeName("ufbx_real")]
                public float m12;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.m22"]/*' />
                [NativeTypeName("ufbx_real")]
                public float m22;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.m03"]/*' />
                [NativeTypeName("ufbx_real")]
                public float m03;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.m13"]/*' />
                [NativeTypeName("ufbx_real")]
                public float m13;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.m23"]/*' />
                [NativeTypeName("ufbx_real")]
                public float m23;
            }

            /// <include file='_cols_e__FixedBuffer.xml' path='doc/member[@name="_cols_e__FixedBuffer"]/*' />
            [InlineArray(4)]
            public partial struct _cols_e__FixedBuffer
            {
                public ufbx_vec3 e0;
            }

            /// <include file='_v_e__FixedBuffer.xml' path='doc/member[@name="_v_e__FixedBuffer"]/*' />
            [InlineArray(12)]
            public partial struct _v_e__FixedBuffer
            {
                public float e0;
            }
        }
    }
}
