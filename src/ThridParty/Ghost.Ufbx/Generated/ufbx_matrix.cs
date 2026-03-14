using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    public partial struct ufbx_matrix
    {
        [NativeTypeName("__AnonymousRecord_ufbx_L368_C2")]
        public _Anonymous_e__Union Anonymous;

        [UnscopedRef]
        public ref float m00
        {
            get
            {
                return ref Anonymous.Anonymous.m00;
            }
        }

        [UnscopedRef]
        public ref float m10
        {
            get
            {
                return ref Anonymous.Anonymous.m10;
            }
        }

        [UnscopedRef]
        public ref float m20
        {
            get
            {
                return ref Anonymous.Anonymous.m20;
            }
        }

        [UnscopedRef]
        public ref float m01
        {
            get
            {
                return ref Anonymous.Anonymous.m01;
            }
        }

        [UnscopedRef]
        public ref float m11
        {
            get
            {
                return ref Anonymous.Anonymous.m11;
            }
        }

        [UnscopedRef]
        public ref float m21
        {
            get
            {
                return ref Anonymous.Anonymous.m21;
            }
        }

        [UnscopedRef]
        public ref float m02
        {
            get
            {
                return ref Anonymous.Anonymous.m02;
            }
        }

        [UnscopedRef]
        public ref float m12
        {
            get
            {
                return ref Anonymous.Anonymous.m12;
            }
        }

        [UnscopedRef]
        public ref float m22
        {
            get
            {
                return ref Anonymous.Anonymous.m22;
            }
        }

        [UnscopedRef]
        public ref float m03
        {
            get
            {
                return ref Anonymous.Anonymous.m03;
            }
        }

        [UnscopedRef]
        public ref float m13
        {
            get
            {
                return ref Anonymous.Anonymous.m13;
            }
        }

        [UnscopedRef]
        public ref float m23
        {
            get
            {
                return ref Anonymous.Anonymous.m23;
            }
        }

        [UnscopedRef]
        public Span<System.Numerics.Vector3> cols
        {
            get
            {
                return Anonymous.cols;
            }
        }

        [UnscopedRef]
        public Span<float> v
        {
            get
            {
                return Anonymous.v;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_ufbx_L369_C3")]
            public _Anonymous_e__Struct Anonymous;

            [FieldOffset(0)]
            [NativeTypeName("ufbx_vec3[4]")]
            public _cols_e__FixedBuffer cols;

            [FieldOffset(0)]
            [NativeTypeName("ufbx_real[12]")]
            public _v_e__FixedBuffer v;

            public partial struct _Anonymous_e__Struct
            {
                [NativeTypeName("ufbx_real")]
                public float m00;

                [NativeTypeName("ufbx_real")]
                public float m10;

                [NativeTypeName("ufbx_real")]
                public float m20;

                [NativeTypeName("ufbx_real")]
                public float m01;

                [NativeTypeName("ufbx_real")]
                public float m11;

                [NativeTypeName("ufbx_real")]
                public float m21;

                [NativeTypeName("ufbx_real")]
                public float m02;

                [NativeTypeName("ufbx_real")]
                public float m12;

                [NativeTypeName("ufbx_real")]
                public float m22;

                [NativeTypeName("ufbx_real")]
                public float m03;

                [NativeTypeName("ufbx_real")]
                public float m13;

                [NativeTypeName("ufbx_real")]
                public float m23;
            }

            [InlineArray(4)]
            public partial struct _cols_e__FixedBuffer
            {
                public System.Numerics.Vector3 e0;
            }

            [InlineArray(12)]
            public partial struct _v_e__FixedBuffer
            {
                public float e0;
            }
        }
    }
}
