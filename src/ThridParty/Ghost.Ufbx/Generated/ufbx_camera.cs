using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    public partial struct ufbx_camera
    {
        [NativeTypeName("__AnonymousRecord_ufbx_L1564_C2")]
        public _Anonymous_e__Union Anonymous;

        public ufbx_projection_mode projection_mode;

        [NativeTypeName("_Bool")]
        public bool resolution_is_pixels;

        [NativeTypeName("ufbx_vec2")]
        public Misaki.HighPerformance.Mathematics.float2 resolution;

        [NativeTypeName("ufbx_vec2")]
        public Misaki.HighPerformance.Mathematics.float2 field_of_view_deg;

        [NativeTypeName("ufbx_vec2")]
        public Misaki.HighPerformance.Mathematics.float2 field_of_view_tan;

        [NativeTypeName("ufbx_real")]
        public float orthographic_extent;

        [NativeTypeName("ufbx_vec2")]
        public Misaki.HighPerformance.Mathematics.float2 orthographic_size;

        [NativeTypeName("ufbx_vec2")]
        public Misaki.HighPerformance.Mathematics.float2 projection_plane;

        [NativeTypeName("ufbx_real")]
        public float aspect_ratio;

        [NativeTypeName("ufbx_real")]
        public float near_plane;

        [NativeTypeName("ufbx_real")]
        public float far_plane;

        public ufbx_coordinate_axes projection_axes;

        public ufbx_aspect_mode aspect_mode;

        public ufbx_aperture_mode aperture_mode;

        public ufbx_gate_fit gate_fit;

        public ufbx_aperture_format aperture_format;

        [NativeTypeName("ufbx_real")]
        public float focal_length_mm;

        [NativeTypeName("ufbx_vec2")]
        public Misaki.HighPerformance.Mathematics.float2 film_size_inch;

        [NativeTypeName("ufbx_vec2")]
        public Misaki.HighPerformance.Mathematics.float2 aperture_size_inch;

        [NativeTypeName("ufbx_real")]
        public float squeeze_ratio;

        [UnscopedRef]
        public ref ufbx_element element
        {
            get
            {
                return ref Anonymous.element;
            }
        }

        [UnscopedRef]
        public ref ufbx_string name
        {
            get
            {
                return ref Anonymous.Anonymous.name;
            }
        }

        [UnscopedRef]
        public ref ufbx_props props
        {
            get
            {
                return ref Anonymous.Anonymous.props;
            }
        }

        [UnscopedRef]
        public ref uint element_id
        {
            get
            {
                return ref Anonymous.Anonymous.element_id;
            }
        }

        [UnscopedRef]
        public ref uint typed_id
        {
            get
            {
                return ref Anonymous.Anonymous.typed_id;
            }
        }

        [UnscopedRef]
        public ref ufbx_node_list instances
        {
            get
            {
                return ref Anonymous.Anonymous.instances;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            public ufbx_element element;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_ufbx_L1564_C32")]
            public _Anonymous_e__Struct Anonymous;

            public partial struct _Anonymous_e__Struct
            {
                public ufbx_string name;

                public ufbx_props props;

                [NativeTypeName("uint32_t")]
                public uint element_id;

                [NativeTypeName("uint32_t")]
                public uint typed_id;

                public ufbx_node_list instances;
            }
        }
    }
}
