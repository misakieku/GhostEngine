using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    public partial struct ufbx_vec4
    {
        [NativeTypeName("__AnonymousRecord_ufbx_L319_C2")]
        public _Anonymous_e__Union Anonymous;

        [UnscopedRef]
        public ref float x
        {
            get
            {
                return ref Anonymous.Anonymous.x;
            }
        }

        [UnscopedRef]
        public ref float y
        {
            get
            {
                return ref Anonymous.Anonymous.y;
            }
        }

        [UnscopedRef]
        public ref float z
        {
            get
            {
                return ref Anonymous.Anonymous.z;
            }
        }

        [UnscopedRef]
        public ref float w
        {
            get
            {
                return ref Anonymous.Anonymous.w;
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
            [NativeTypeName("__AnonymousRecord_ufbx_L320_C3")]
            public _Anonymous_e__Struct Anonymous;

            [FieldOffset(0)]
            [NativeTypeName("ufbx_real[4]")]
            public _v_e__FixedBuffer v;

            public partial struct _Anonymous_e__Struct
            {
                [NativeTypeName("ufbx_real")]
                public float x;

                [NativeTypeName("ufbx_real")]
                public float y;

                [NativeTypeName("ufbx_real")]
                public float z;

                [NativeTypeName("ufbx_real")]
                public float w;
            }

            [InlineArray(4)]
            public partial struct _v_e__FixedBuffer
            {
                public float e0;
            }
        }
    }
}
