using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    /// <include file='ufbx_skin_deformer.xml' path='doc/member[@name="ufbx_skin_deformer"]/*' />
    public partial struct ufbx_skin_deformer
    {
        /// <include file='ufbx_skin_deformer.xml' path='doc/member[@name="ufbx_skin_deformer.Anonymous"]/*' />
        [NativeTypeName("__AnonymousRecord_ufbx_L1983_C2")]
        public _Anonymous_e__Union Anonymous;

        /// <include file='ufbx_skin_deformer.xml' path='doc/member[@name="ufbx_skin_deformer.skinning_method"]/*' />
        public ufbx_skinning_method skinning_method;

        /// <include file='ufbx_skin_deformer.xml' path='doc/member[@name="ufbx_skin_deformer.clusters"]/*' />
        public ufbx_skin_cluster_list clusters;

        /// <include file='ufbx_skin_deformer.xml' path='doc/member[@name="ufbx_skin_deformer.vertices"]/*' />
        public ufbx_skin_vertex_list vertices;

        /// <include file='ufbx_skin_deformer.xml' path='doc/member[@name="ufbx_skin_deformer.weights"]/*' />
        public ufbx_skin_weight_list weights;

        /// <include file='ufbx_skin_deformer.xml' path='doc/member[@name="ufbx_skin_deformer.max_weights_per_vertex"]/*' />
        [NativeTypeName("size_t")]
        public nuint max_weights_per_vertex;

        /// <include file='ufbx_skin_deformer.xml' path='doc/member[@name="ufbx_skin_deformer.num_dq_weights"]/*' />
        [NativeTypeName("size_t")]
        public nuint num_dq_weights;

        /// <include file='ufbx_skin_deformer.xml' path='doc/member[@name="ufbx_skin_deformer.dq_vertices"]/*' />
        public ufbx_uint32_list dq_vertices;

        /// <include file='ufbx_skin_deformer.xml' path='doc/member[@name="ufbx_skin_deformer.dq_weights"]/*' />
        public ufbx_real_list dq_weights;

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
            [NativeTypeName("__AnonymousRecord_ufbx_L1983_C32")]
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
