using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    /// <include file='ufbx_prop.xml' path='doc/member[@name="ufbx_prop"]/*' />
    public partial struct ufbx_prop
    {
        /// <include file='ufbx_prop.xml' path='doc/member[@name="ufbx_prop.name"]/*' />
        public ufbx_string name;

        /// <include file='ufbx_prop.xml' path='doc/member[@name="ufbx_prop._internal_key"]/*' />
        [NativeTypeName("uint32_t")]
        public uint _internal_key;

        /// <include file='ufbx_prop.xml' path='doc/member[@name="ufbx_prop.type"]/*' />
        public ufbx_prop_type type;

        /// <include file='ufbx_prop.xml' path='doc/member[@name="ufbx_prop.flags"]/*' />
        public ufbx_prop_flags flags;

        /// <include file='ufbx_prop.xml' path='doc/member[@name="ufbx_prop.value_str"]/*' />
        public ufbx_string value_str;

        /// <include file='ufbx_prop.xml' path='doc/member[@name="ufbx_prop.value_blob"]/*' />
        public ufbx_blob value_blob;

        /// <include file='ufbx_prop.xml' path='doc/member[@name="ufbx_prop.value_int"]/*' />
        [NativeTypeName("int64_t")]
        public long value_int;

        /// <include file='ufbx_prop.xml' path='doc/member[@name="ufbx_prop.Anonymous"]/*' />
        [NativeTypeName("__AnonymousRecord_ufbx_L553_C2")]
        public _Anonymous_e__Union Anonymous;

        /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union.value_real_arr"]/*' />
        [UnscopedRef]
        public Span<float> value_real_arr
        {
            get
            {
                return Anonymous.value_real_arr;
            }
        }

        /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union.value_real"]/*' />
        [UnscopedRef]
        public ref float value_real
        {
            get
            {
                return ref Anonymous.value_real;
            }
        }

        /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union.value_vec2"]/*' />
        [UnscopedRef]
        public ref ufbx_vec2 value_vec2
        {
            get
            {
                return ref Anonymous.value_vec2;
            }
        }

        /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union.value_vec3"]/*' />
        [UnscopedRef]
        public ref ufbx_vec3 value_vec3
        {
            get
            {
                return ref Anonymous.value_vec3;
            }
        }

        /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union.value_vec4"]/*' />
        [UnscopedRef]
        public ref ufbx_vec4 value_vec4
        {
            get
            {
                return ref Anonymous.value_vec4;
            }
        }

        /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union"]/*' />
        [StructLayout(LayoutKind.Explicit)]
        public partial struct _Anonymous_e__Union
        {
            /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union.value_real_arr"]/*' />
            [FieldOffset(0)]
            [NativeTypeName("ufbx_real[4]")]
            public _value_real_arr_e__FixedBuffer value_real_arr;

            /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union.value_real"]/*' />
            [FieldOffset(0)]
            [NativeTypeName("ufbx_real")]
            public float value_real;

            /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union.value_vec2"]/*' />
            [FieldOffset(0)]
            public ufbx_vec2 value_vec2;

            /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union.value_vec3"]/*' />
            [FieldOffset(0)]
            public ufbx_vec3 value_vec3;

            /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union.value_vec4"]/*' />
            [FieldOffset(0)]
            public ufbx_vec4 value_vec4;

            /// <include file='_value_real_arr_e__FixedBuffer.xml' path='doc/member[@name="_value_real_arr_e__FixedBuffer"]/*' />
            [InlineArray(4)]
            public partial struct _value_real_arr_e__FixedBuffer
            {
                public float e0;
            }
        }
    }
}
