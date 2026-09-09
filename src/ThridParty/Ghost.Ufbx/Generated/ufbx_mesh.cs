using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh"]/*' />
    public unsafe partial struct ufbx_mesh
    {
        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.Anonymous"]/*' />
        [NativeTypeName("__AnonymousRecord_ufbx_L1258_C2")]
        public _Anonymous_e__Union Anonymous;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.num_vertices"]/*' />
        [NativeTypeName("size_t")]
        public nuint num_vertices;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.num_indices"]/*' />
        [NativeTypeName("size_t")]
        public nuint num_indices;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.num_faces"]/*' />
        [NativeTypeName("size_t")]
        public nuint num_faces;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.num_triangles"]/*' />
        [NativeTypeName("size_t")]
        public nuint num_triangles;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.num_edges"]/*' />
        [NativeTypeName("size_t")]
        public nuint num_edges;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.max_face_triangles"]/*' />
        [NativeTypeName("size_t")]
        public nuint max_face_triangles;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.num_empty_faces"]/*' />
        [NativeTypeName("size_t")]
        public nuint num_empty_faces;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.num_point_faces"]/*' />
        [NativeTypeName("size_t")]
        public nuint num_point_faces;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.num_line_faces"]/*' />
        [NativeTypeName("size_t")]
        public nuint num_line_faces;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.faces"]/*' />
        public ufbx_face_list faces;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.face_smoothing"]/*' />
        public ufbx_bool_list face_smoothing;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.face_material"]/*' />
        public ufbx_uint32_list face_material;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.face_group"]/*' />
        public ufbx_uint32_list face_group;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.face_hole"]/*' />
        public ufbx_bool_list face_hole;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.edges"]/*' />
        public ufbx_edge_list edges;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.edge_smoothing"]/*' />
        public ufbx_bool_list edge_smoothing;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.edge_crease"]/*' />
        public ufbx_real_list edge_crease;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.edge_visibility"]/*' />
        public ufbx_bool_list edge_visibility;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.vertex_indices"]/*' />
        public ufbx_uint32_list vertex_indices;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.vertices"]/*' />
        public ufbx_vec3_list vertices;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.vertex_first_index"]/*' />
        public ufbx_uint32_list vertex_first_index;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.vertex_position"]/*' />
        public ufbx_vertex_vec3 vertex_position;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.vertex_normal"]/*' />
        public ufbx_vertex_vec3 vertex_normal;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.vertex_uv"]/*' />
        public ufbx_vertex_vec2 vertex_uv;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.vertex_tangent"]/*' />
        public ufbx_vertex_vec3 vertex_tangent;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.vertex_bitangent"]/*' />
        public ufbx_vertex_vec3 vertex_bitangent;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.vertex_color"]/*' />
        public ufbx_vertex_vec4 vertex_color;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.vertex_crease"]/*' />
        public ufbx_vertex_real vertex_crease;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.uv_sets"]/*' />
        public ufbx_uv_set_list uv_sets;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.color_sets"]/*' />
        public ufbx_color_set_list color_sets;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.materials"]/*' />
        public ufbx_material_list materials;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.face_groups"]/*' />
        public ufbx_face_group_list face_groups;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.material_parts"]/*' />
        public ufbx_mesh_part_list material_parts;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.face_group_parts"]/*' />
        public ufbx_mesh_part_list face_group_parts;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.material_part_usage_order"]/*' />
        public ufbx_uint32_list material_part_usage_order;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.skinned_is_local"]/*' />
        [NativeTypeName("_Bool")]
        public bool skinned_is_local;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.skinned_position"]/*' />
        public ufbx_vertex_vec3 skinned_position;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.skinned_normal"]/*' />
        public ufbx_vertex_vec3 skinned_normal;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.skin_deformers"]/*' />
        public ufbx_skin_deformer_list skin_deformers;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.blend_deformers"]/*' />
        public ufbx_blend_deformer_list blend_deformers;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.cache_deformers"]/*' />
        public ufbx_cache_deformer_list cache_deformers;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.all_deformers"]/*' />
        public ufbx_element_list all_deformers;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.subdivision_preview_levels"]/*' />
        [NativeTypeName("uint32_t")]
        public uint subdivision_preview_levels;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.subdivision_render_levels"]/*' />
        [NativeTypeName("uint32_t")]
        public uint subdivision_render_levels;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.subdivision_display_mode"]/*' />
        public ufbx_subdivision_display_mode subdivision_display_mode;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.subdivision_boundary"]/*' />
        public ufbx_subdivision_boundary subdivision_boundary;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.subdivision_uv_boundary"]/*' />
        public ufbx_subdivision_boundary subdivision_uv_boundary;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.reversed_winding"]/*' />
        [NativeTypeName("_Bool")]
        public bool reversed_winding;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.generated_normals"]/*' />
        [NativeTypeName("_Bool")]
        public bool generated_normals;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.subdivision_evaluated"]/*' />
        [NativeTypeName("_Bool")]
        public bool subdivision_evaluated;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.subdivision_result"]/*' />
        public ufbx_subdivision_result* subdivision_result;

        /// <include file='ufbx_mesh.xml' path='doc/member[@name="ufbx_mesh.from_tessellated_nurbs"]/*' />
        [NativeTypeName("_Bool")]
        public bool from_tessellated_nurbs;

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
            [NativeTypeName("__AnonymousRecord_ufbx_L1258_C32")]
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
