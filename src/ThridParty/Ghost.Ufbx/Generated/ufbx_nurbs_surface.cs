using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    /// <include file='ufbx_nurbs_surface.xml' path='doc/member[@name="ufbx_nurbs_surface"]/*' />
    public unsafe partial struct ufbx_nurbs_surface
    {
        /// <include file='ufbx_nurbs_surface.xml' path='doc/member[@name="ufbx_nurbs_surface.Anonymous"]/*' />
        [NativeTypeName("__AnonymousRecord_ufbx_L1764_C2")]
        public _Anonymous_e__Union Anonymous;

        /// <include file='ufbx_nurbs_surface.xml' path='doc/member[@name="ufbx_nurbs_surface.basis_u"]/*' />
        public ufbx_nurbs_basis basis_u;

        /// <include file='ufbx_nurbs_surface.xml' path='doc/member[@name="ufbx_nurbs_surface.basis_v"]/*' />
        public ufbx_nurbs_basis basis_v;

        /// <include file='ufbx_nurbs_surface.xml' path='doc/member[@name="ufbx_nurbs_surface.num_control_points_u"]/*' />
        [NativeTypeName("size_t")]
        public nuint num_control_points_u;

        /// <include file='ufbx_nurbs_surface.xml' path='doc/member[@name="ufbx_nurbs_surface.num_control_points_v"]/*' />
        [NativeTypeName("size_t")]
        public nuint num_control_points_v;

        /// <include file='ufbx_nurbs_surface.xml' path='doc/member[@name="ufbx_nurbs_surface.control_points"]/*' />
        public ufbx_vec4_list control_points;

        /// <include file='ufbx_nurbs_surface.xml' path='doc/member[@name="ufbx_nurbs_surface.span_subdivision_u"]/*' />
        [NativeTypeName("uint32_t")]
        public uint span_subdivision_u;

        /// <include file='ufbx_nurbs_surface.xml' path='doc/member[@name="ufbx_nurbs_surface.span_subdivision_v"]/*' />
        [NativeTypeName("uint32_t")]
        public uint span_subdivision_v;

        /// <include file='ufbx_nurbs_surface.xml' path='doc/member[@name="ufbx_nurbs_surface.flip_normals"]/*' />
        [NativeTypeName("_Bool")]
        public bool flip_normals;

        /// <include file='ufbx_nurbs_surface.xml' path='doc/member[@name="ufbx_nurbs_surface.material"]/*' />
        public ufbx_material* material;

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
            [NativeTypeName("__AnonymousRecord_ufbx_L1764_C32")]
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
