using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_skin_cluster
    {
        [NativeTypeName("__AnonymousRecord_ufbx_L2006_C2")]
        public _Anonymous_e__Union Anonymous;

        public ufbx_node* bone_node;

        [NativeTypeName("ufbx_matrix")]
        public Misaki.HighPerformance.Mathematics.float3x4 geometry_to_bone;

        [NativeTypeName("ufbx_matrix")]
        public Misaki.HighPerformance.Mathematics.float3x4 mesh_node_to_bone;

        [NativeTypeName("ufbx_matrix")]
        public Misaki.HighPerformance.Mathematics.float3x4 bind_to_world;

        [NativeTypeName("ufbx_matrix")]
        public Misaki.HighPerformance.Mathematics.float3x4 geometry_to_world;

        public ufbx_transform geometry_to_world_transform;

        [NativeTypeName("size_t")]
        public nuint num_weights;

        public ufbx_uint32_list vertices;

        public ufbx_real_list weights;

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
            [NativeTypeName("__AnonymousRecord_ufbx_L2006_C32")]
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
