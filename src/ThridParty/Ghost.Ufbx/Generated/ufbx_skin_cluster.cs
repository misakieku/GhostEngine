using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    /// <include file='ufbx_skin_cluster.xml' path='doc/member[@name="ufbx_skin_cluster"]/*' />
    public unsafe partial struct ufbx_skin_cluster
    {
        /// <include file='ufbx_skin_cluster.xml' path='doc/member[@name="ufbx_skin_cluster.Anonymous"]/*' />
        [NativeTypeName("__AnonymousRecord_ufbx_L2012_C2")]
        public _Anonymous_e__Union Anonymous;

        /// <include file='ufbx_skin_cluster.xml' path='doc/member[@name="ufbx_skin_cluster.bone_node"]/*' />
        public ufbx_node* bone_node;

        /// <include file='ufbx_skin_cluster.xml' path='doc/member[@name="ufbx_skin_cluster.geometry_to_bone"]/*' />
        public ufbx_matrix geometry_to_bone;

        /// <include file='ufbx_skin_cluster.xml' path='doc/member[@name="ufbx_skin_cluster.mesh_node_to_bone"]/*' />
        public ufbx_matrix mesh_node_to_bone;

        /// <include file='ufbx_skin_cluster.xml' path='doc/member[@name="ufbx_skin_cluster.bind_to_world"]/*' />
        public ufbx_matrix bind_to_world;

        /// <include file='ufbx_skin_cluster.xml' path='doc/member[@name="ufbx_skin_cluster.geometry_to_world"]/*' />
        public ufbx_matrix geometry_to_world;

        /// <include file='ufbx_skin_cluster.xml' path='doc/member[@name="ufbx_skin_cluster.geometry_to_world_transform"]/*' />
        public ufbx_transform geometry_to_world_transform;

        /// <include file='ufbx_skin_cluster.xml' path='doc/member[@name="ufbx_skin_cluster.num_weights"]/*' />
        [NativeTypeName("size_t")]
        public nuint num_weights;

        /// <include file='ufbx_skin_cluster.xml' path='doc/member[@name="ufbx_skin_cluster.vertices"]/*' />
        public ufbx_uint32_list vertices;

        /// <include file='ufbx_skin_cluster.xml' path='doc/member[@name="ufbx_skin_cluster.weights"]/*' />
        public ufbx_real_list weights;

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

        /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union"]/*' />
        [StructLayout(LayoutKind.Explicit)]
        public partial struct _Anonymous_e__Union
        {
            /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union.element"]/*' />
            [FieldOffset(0)]
            public ufbx_element element;

            /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union.Anonymous"]/*' />
            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_ufbx_L2012_C32")]
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
            }
        }
    }
}
