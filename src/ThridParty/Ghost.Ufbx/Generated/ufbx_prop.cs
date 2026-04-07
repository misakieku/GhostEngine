using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    public partial struct ufbx_prop
    {
        public ufbx_string name;

        [NativeTypeName("uint32_t")]
        public uint _internal_key;

        public ufbx_prop_type type;

        public ufbx_prop_flags flags;

        public ufbx_string value_str;

        public ufbx_blob value_blob;

        [NativeTypeName("int64_t")]
        public long value_int;

        [NativeTypeName("__AnonymousRecord_ufbx_L553_C2")]
        public _Anonymous_e__Union Anonymous;

        [UnscopedRef]
        public Span<float> value_real_arr
        {
            get
            {
                return Anonymous.value_real_arr;
            }
        }

        [UnscopedRef]
        public ref float value_real
        {
            get
            {
                return ref Anonymous.value_real;
            }
        }

        [UnscopedRef]
        public ref ufbx_vec2 value_vec2
        {
            get
            {
                return ref Anonymous.value_vec2;
            }
        }

        [UnscopedRef]
        public ref ufbx_vec3 value_vec3
        {
            get
            {
                return ref Anonymous.value_vec3;
            }
        }

        [UnscopedRef]
        public ref ufbx_vec4 value_vec4
        {
            get
            {
                return ref Anonymous.value_vec4;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("ufbx_real[4]")]
            public _value_real_arr_e__FixedBuffer value_real_arr;

            [FieldOffset(0)]
            [NativeTypeName("ufbx_real")]
            public float value_real;

            [FieldOffset(0)]
            public ufbx_vec2 value_vec2;

            [FieldOffset(0)]
            public ufbx_vec3 value_vec3;

            [FieldOffset(0)]
            public ufbx_vec4 value_vec4;

            [InlineArray(4)]
            public partial struct _value_real_arr_e__FixedBuffer
            {
                public float e0;
            }
        }
    }
}
