using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    /// <include file='ufbx_lod_group.xml' path='doc/member[@name="ufbx_lod_group"]/*' />
    public partial struct ufbx_lod_group
    {
        /// <include file='ufbx_lod_group.xml' path='doc/member[@name="ufbx_lod_group.Anonymous"]/*' />
        [NativeTypeName("__AnonymousRecord_ufbx_L1909_C2")]
        public _Anonymous_e__Union Anonymous;

        /// <include file='ufbx_lod_group.xml' path='doc/member[@name="ufbx_lod_group.relative_distances"]/*' />
        [NativeTypeName("_Bool")]
        public bool relative_distances;

        /// <include file='ufbx_lod_group.xml' path='doc/member[@name="ufbx_lod_group.lod_levels"]/*' />
        public ufbx_lod_level_list lod_levels;

        /// <include file='ufbx_lod_group.xml' path='doc/member[@name="ufbx_lod_group.ignore_parent_transform"]/*' />
        [NativeTypeName("_Bool")]
        public bool ignore_parent_transform;

        /// <include file='ufbx_lod_group.xml' path='doc/member[@name="ufbx_lod_group.use_distance_limit"]/*' />
        [NativeTypeName("_Bool")]
        public bool use_distance_limit;

        /// <include file='ufbx_lod_group.xml' path='doc/member[@name="ufbx_lod_group.distance_limit_min"]/*' />
        [NativeTypeName("ufbx_real")]
        public float distance_limit_min;

        /// <include file='ufbx_lod_group.xml' path='doc/member[@name="ufbx_lod_group.distance_limit_max"]/*' />
        [NativeTypeName("ufbx_real")]
        public float distance_limit_max;

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
            [NativeTypeName("__AnonymousRecord_ufbx_L1909_C32")]
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
