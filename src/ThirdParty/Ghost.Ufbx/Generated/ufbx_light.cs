using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    /// <include file='ufbx_light.xml' path='doc/member[@name="ufbx_light"]/*' />
    public partial struct ufbx_light
    {
        /// <include file='ufbx_light.xml' path='doc/member[@name="ufbx_light.Anonymous"]/*' />
        [NativeTypeName("__AnonymousRecord_ufbx_L1425_C2")]
        public _Anonymous_e__Union Anonymous;

        /// <include file='ufbx_light.xml' path='doc/member[@name="ufbx_light.color"]/*' />
        public ufbx_vec3 color;

        /// <include file='ufbx_light.xml' path='doc/member[@name="ufbx_light.intensity"]/*' />
        [NativeTypeName("ufbx_real")]
        public float intensity;

        /// <include file='ufbx_light.xml' path='doc/member[@name="ufbx_light.local_direction"]/*' />
        public ufbx_vec3 local_direction;

        /// <include file='ufbx_light.xml' path='doc/member[@name="ufbx_light.type"]/*' />
        public ufbx_light_type type;

        /// <include file='ufbx_light.xml' path='doc/member[@name="ufbx_light.decay"]/*' />
        public ufbx_light_decay decay;

        /// <include file='ufbx_light.xml' path='doc/member[@name="ufbx_light.area_shape"]/*' />
        public ufbx_light_area_shape area_shape;

        /// <include file='ufbx_light.xml' path='doc/member[@name="ufbx_light.inner_angle"]/*' />
        [NativeTypeName("ufbx_real")]
        public float inner_angle;

        /// <include file='ufbx_light.xml' path='doc/member[@name="ufbx_light.outer_angle"]/*' />
        [NativeTypeName("ufbx_real")]
        public float outer_angle;

        /// <include file='ufbx_light.xml' path='doc/member[@name="ufbx_light.cast_light"]/*' />
        [NativeTypeName("_Bool")]
        public bool cast_light;

        /// <include file='ufbx_light.xml' path='doc/member[@name="ufbx_light.cast_shadows"]/*' />
        [NativeTypeName("_Bool")]
        public bool cast_shadows;

        /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union.element"]/*' />
        [UnscopedRef]
        public ref ufbx_element element
        {
            get
            {
                return ref Anonymous.element;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.name"]/*' />
        [UnscopedRef]
        public ref ufbx_string name
        {
            get
            {
                return ref Anonymous.Anonymous.name;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.props"]/*' />
        [UnscopedRef]
        public ref ufbx_props props
        {
            get
            {
                return ref Anonymous.Anonymous.props;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.element_id"]/*' />
        [UnscopedRef]
        public ref uint element_id
        {
            get
            {
                return ref Anonymous.Anonymous.element_id;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.typed_id"]/*' />
        [UnscopedRef]
        public ref uint typed_id
        {
            get
            {
                return ref Anonymous.Anonymous.typed_id;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.instances"]/*' />
        [UnscopedRef]
        public ref ufbx_node_list instances
        {
            get
            {
                return ref Anonymous.Anonymous.instances;
            }
        }

        /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union"]/*' />
        [StructLayout(LayoutKind.Explicit)]
        public partial struct _Anonymous_e__Union
        {
            /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union.element"]/*' />
            [FieldOffset(0)]
            public ufbx_element element;

            /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union.Anonymous"]/*' />
            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_ufbx_L1425_C32")]
            public _Anonymous_e__Struct Anonymous;

            /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct"]/*' />
            public partial struct _Anonymous_e__Struct
            {
                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.name"]/*' />
                public ufbx_string name;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.props"]/*' />
                public ufbx_props props;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.element_id"]/*' />
                [NativeTypeName("uint32_t")]
                public uint element_id;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.typed_id"]/*' />
                [NativeTypeName("uint32_t")]
                public uint typed_id;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.instances"]/*' />
                public ufbx_node_list instances;
            }
        }
    }
}
