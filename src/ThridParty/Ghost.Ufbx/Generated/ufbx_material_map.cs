using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    /// <include file='ufbx_material_map.xml' path='doc/member[@name="ufbx_material_map"]/*' />
    public unsafe partial struct ufbx_material_map
    {
        /// <include file='ufbx_material_map.xml' path='doc/member[@name="ufbx_material_map.Anonymous"]/*' />
        [NativeTypeName("__AnonymousRecord_ufbx_L2293_C2")]
        public _Anonymous_e__Union Anonymous;

        /// <include file='ufbx_material_map.xml' path='doc/member[@name="ufbx_material_map.value_int"]/*' />
        [NativeTypeName("int64_t")]
        public long value_int;

        /// <include file='ufbx_material_map.xml' path='doc/member[@name="ufbx_material_map.texture"]/*' />
        public ufbx_texture* texture;

        /// <include file='ufbx_material_map.xml' path='doc/member[@name="ufbx_material_map.has_value"]/*' />
        [NativeTypeName("_Bool")]
        public bool has_value;

        /// <include file='ufbx_material_map.xml' path='doc/member[@name="ufbx_material_map.texture_enabled"]/*' />
        [NativeTypeName("_Bool")]
        public bool texture_enabled;

        /// <include file='ufbx_material_map.xml' path='doc/member[@name="ufbx_material_map.feature_disabled"]/*' />
        [NativeTypeName("_Bool")]
        public bool feature_disabled;

        /// <include file='ufbx_material_map.xml' path='doc/member[@name="ufbx_material_map.value_components"]/*' />
        [NativeTypeName("uint8_t")]
        public byte value_components;

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
