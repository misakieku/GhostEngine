using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    public partial struct ufbx_skin_deformer
    {
        [NativeTypeName("__AnonymousRecord_ufbx_L1977_C2")]
        public _Anonymous_e__Union Anonymous;

        public ufbx_skinning_method skinning_method;

        public ufbx_skin_cluster_list clusters;

        public ufbx_skin_vertex_list vertices;

        public ufbx_skin_weight_list weights;

        [NativeTypeName("size_t")]
        public nuint max_weights_per_vertex;

        [NativeTypeName("size_t")]
        public nuint num_dq_weights;

        public ufbx_uint32_list dq_vertices;

        public ufbx_real_list dq_weights;

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

        [StructLayout(LayoutKind.Explicit)]
        public partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            public ufbx_element element;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_ufbx_L1977_C32")]
            public _Anonymous_e__Struct Anonymous;

            public partial struct _Anonymous_e__Struct
            {
                public ufbx_string name;

                public ufbx_props props;

                [NativeTypeName("uint32_t")]
                public uint element_id;

                [NativeTypeName("uint32_t")]
                public uint typed_id;
            }
        }
    }
}
