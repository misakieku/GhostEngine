using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_nurbs_surface
    {
        [NativeTypeName("__AnonymousRecord_ufbx_L1758_C2")]
        public _Anonymous_e__Union Anonymous;

        public ufbx_nurbs_basis basis_u;

        public ufbx_nurbs_basis basis_v;

        [NativeTypeName("size_t")]
        public nuint num_control_points_u;

        [NativeTypeName("size_t")]
        public nuint num_control_points_v;

        public ufbx_vec4_list control_points;

        [NativeTypeName("uint32_t")]
        public uint span_subdivision_u;

        [NativeTypeName("uint32_t")]
        public uint span_subdivision_v;

        [NativeTypeName("_Bool")]
        public bool flip_normals;

        public ufbx_material* material;

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
            [NativeTypeName("__AnonymousRecord_ufbx_L1758_C32")]
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
