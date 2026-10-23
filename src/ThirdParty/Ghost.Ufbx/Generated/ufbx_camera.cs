using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    /// <include file='ufbx_camera.xml' path='doc/member[@name="ufbx_camera"]/*' />
    public partial struct ufbx_camera
    {
        /// <include file='ufbx_camera.xml' path='doc/member[@name="ufbx_camera.Anonymous"]/*' />
        [NativeTypeName("__AnonymousRecord_ufbx_L1568_C2")]
        public _Anonymous_e__Union Anonymous;

        /// <include file='ufbx_camera.xml' path='doc/member[@name="ufbx_camera.projection_mode"]/*' />
        public ufbx_projection_mode projection_mode;

        /// <include file='ufbx_camera.xml' path='doc/member[@name="ufbx_camera.resolution_is_pixels"]/*' />
        [NativeTypeName("_Bool")]
        public bool resolution_is_pixels;

        /// <include file='ufbx_camera.xml' path='doc/member[@name="ufbx_camera.resolution"]/*' />
        public ufbx_vec2 resolution;

        /// <include file='ufbx_camera.xml' path='doc/member[@name="ufbx_camera.field_of_view_deg"]/*' />
        public ufbx_vec2 field_of_view_deg;

        /// <include file='ufbx_camera.xml' path='doc/member[@name="ufbx_camera.field_of_view_tan"]/*' />
        public ufbx_vec2 field_of_view_tan;

        /// <include file='ufbx_camera.xml' path='doc/member[@name="ufbx_camera.orthographic_extent"]/*' />
        [NativeTypeName("ufbx_real")]
        public float orthographic_extent;

        /// <include file='ufbx_camera.xml' path='doc/member[@name="ufbx_camera.orthographic_size"]/*' />
        public ufbx_vec2 orthographic_size;

        /// <include file='ufbx_camera.xml' path='doc/member[@name="ufbx_camera.projection_plane"]/*' />
        public ufbx_vec2 projection_plane;

        /// <include file='ufbx_camera.xml' path='doc/member[@name="ufbx_camera.aspect_ratio"]/*' />
        [NativeTypeName("ufbx_real")]
        public float aspect_ratio;

        /// <include file='ufbx_camera.xml' path='doc/member[@name="ufbx_camera.near_plane"]/*' />
        [NativeTypeName("ufbx_real")]
        public float near_plane;

        /// <include file='ufbx_camera.xml' path='doc/member[@name="ufbx_camera.far_plane"]/*' />
        [NativeTypeName("ufbx_real")]
        public float far_plane;

        /// <include file='ufbx_camera.xml' path='doc/member[@name="ufbx_camera.projection_axes"]/*' />
        public ufbx_coordinate_axes projection_axes;

        /// <include file='ufbx_camera.xml' path='doc/member[@name="ufbx_camera.aspect_mode"]/*' />
        public ufbx_aspect_mode aspect_mode;

        /// <include file='ufbx_camera.xml' path='doc/member[@name="ufbx_camera.aperture_mode"]/*' />
        public ufbx_aperture_mode aperture_mode;

        /// <include file='ufbx_camera.xml' path='doc/member[@name="ufbx_camera.gate_fit"]/*' />
        public ufbx_gate_fit gate_fit;

        /// <include file='ufbx_camera.xml' path='doc/member[@name="ufbx_camera.aperture_format"]/*' />
        public ufbx_aperture_format aperture_format;

        /// <include file='ufbx_camera.xml' path='doc/member[@name="ufbx_camera.focal_length_mm"]/*' />
        [NativeTypeName("ufbx_real")]
        public float focal_length_mm;

        /// <include file='ufbx_camera.xml' path='doc/member[@name="ufbx_camera.film_size_inch"]/*' />
        public ufbx_vec2 film_size_inch;

        /// <include file='ufbx_camera.xml' path='doc/member[@name="ufbx_camera.aperture_size_inch"]/*' />
        public ufbx_vec2 aperture_size_inch;

        /// <include file='ufbx_camera.xml' path='doc/member[@name="ufbx_camera.squeeze_ratio"]/*' />
        [NativeTypeName("ufbx_real")]
        public float squeeze_ratio;

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
            [NativeTypeName("__AnonymousRecord_ufbx_L1568_C32")]
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
