using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_shader_texture_input
    {
        public ufbx_string name;

        [NativeTypeName("__AnonymousRecord_ufbx_L2775_C2")]
        public _Anonymous_e__Union Anonymous;

        [NativeTypeName("int64_t")]
        public long value_int;

        public ufbx_string value_str;

        public ufbx_blob value_blob;

        public ufbx_texture* texture;

        [NativeTypeName("int64_t")]
        public long texture_output_index;

        [NativeTypeName("_Bool")]
        public bool texture_enabled;

        public ufbx_prop* prop;

        public ufbx_prop* texture_prop;

        public ufbx_prop* texture_enabled_prop;

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
            [NativeTypeName("ufbx_real")]
            public float value_real;

            [FieldOffset(0)]
            public ufbx_vec2 value_vec2;

            [FieldOffset(0)]
            public ufbx_vec3 value_vec3;

            [FieldOffset(0)]
            public ufbx_vec4 value_vec4;
        }
    }
}
