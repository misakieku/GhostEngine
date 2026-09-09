using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    /// <include file='ufbx_shader_texture_input.xml' path='doc/member[@name="ufbx_shader_texture_input"]/*' />
    public unsafe partial struct ufbx_shader_texture_input
    {
        /// <include file='ufbx_shader_texture_input.xml' path='doc/member[@name="ufbx_shader_texture_input.name"]/*' />
        public ufbx_string name;

        /// <include file='ufbx_shader_texture_input.xml' path='doc/member[@name="ufbx_shader_texture_input.Anonymous"]/*' />
        [NativeTypeName("__AnonymousRecord_ufbx_L2781_C2")]
        public _Anonymous_e__Union Anonymous;

        /// <include file='ufbx_shader_texture_input.xml' path='doc/member[@name="ufbx_shader_texture_input.value_int"]/*' />
        [NativeTypeName("int64_t")]
        public long value_int;

        /// <include file='ufbx_shader_texture_input.xml' path='doc/member[@name="ufbx_shader_texture_input.value_str"]/*' />
        public ufbx_string value_str;

        /// <include file='ufbx_shader_texture_input.xml' path='doc/member[@name="ufbx_shader_texture_input.value_blob"]/*' />
        public ufbx_blob value_blob;

        /// <include file='ufbx_shader_texture_input.xml' path='doc/member[@name="ufbx_shader_texture_input.texture"]/*' />
        public ufbx_texture* texture;

        /// <include file='ufbx_shader_texture_input.xml' path='doc/member[@name="ufbx_shader_texture_input.texture_output_index"]/*' />
        [NativeTypeName("int64_t")]
        public long texture_output_index;

        /// <include file='ufbx_shader_texture_input.xml' path='doc/member[@name="ufbx_shader_texture_input.texture_enabled"]/*' />
        [NativeTypeName("_Bool")]
        public bool texture_enabled;

        /// <include file='ufbx_shader_texture_input.xml' path='doc/member[@name="ufbx_shader_texture_input.prop"]/*' />
        public ufbx_prop* prop;

        /// <include file='ufbx_shader_texture_input.xml' path='doc/member[@name="ufbx_shader_texture_input.texture_prop"]/*' />
        public ufbx_prop* texture_prop;

        /// <include file='ufbx_shader_texture_input.xml' path='doc/member[@name="ufbx_shader_texture_input.texture_enabled_prop"]/*' />
        public ufbx_prop* texture_enabled_prop;

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
        }
    }
}
