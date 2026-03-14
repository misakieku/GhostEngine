using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_mesh
    {
        [NativeTypeName("__AnonymousRecord_ufbx_L1254_C2")]
        public _Anonymous_e__Union Anonymous;

        [NativeTypeName("size_t")]
        public nuint num_vertices;

        [NativeTypeName("size_t")]
        public nuint num_indices;

        [NativeTypeName("size_t")]
        public nuint num_faces;

        [NativeTypeName("size_t")]
        public nuint num_triangles;

        [NativeTypeName("size_t")]
        public nuint num_edges;

        [NativeTypeName("size_t")]
        public nuint max_face_triangles;

        [NativeTypeName("size_t")]
        public nuint num_empty_faces;

        [NativeTypeName("size_t")]
        public nuint num_point_faces;

        [NativeTypeName("size_t")]
        public nuint num_line_faces;

        public ufbx_face_list faces;

        public ufbx_bool_list face_smoothing;

        public ufbx_uint32_list face_material;

        public ufbx_uint32_list face_group;

        public ufbx_bool_list face_hole;

        public ufbx_edge_list edges;

        public ufbx_bool_list edge_smoothing;

        public ufbx_real_list edge_crease;

        public ufbx_bool_list edge_visibility;

        public ufbx_uint32_list vertex_indices;

        public ufbx_vec3_list vertices;

        public ufbx_uint32_list vertex_first_index;

        public ufbx_vertex_vec3 vertex_position;

        public ufbx_vertex_vec3 vertex_normal;

        public ufbx_vertex_vec2 vertex_uv;

        public ufbx_vertex_vec3 vertex_tangent;

        public ufbx_vertex_vec3 vertex_bitangent;

        public ufbx_vertex_vec4 vertex_color;

        public ufbx_vertex_real vertex_crease;

        public ufbx_uv_set_list uv_sets;

        public ufbx_color_set_list color_sets;

        public ufbx_material_list materials;

        public ufbx_face_group_list face_groups;

        public ufbx_mesh_part_list material_parts;

        public ufbx_mesh_part_list face_group_parts;

        public ufbx_uint32_list material_part_usage_order;

        [NativeTypeName("_Bool")]
        public bool skinned_is_local;

        public ufbx_vertex_vec3 skinned_position;

        public ufbx_vertex_vec3 skinned_normal;

        public ufbx_skin_deformer_list skin_deformers;

        public ufbx_blend_deformer_list blend_deformers;

        public ufbx_cache_deformer_list cache_deformers;

        public ufbx_element_list all_deformers;

        [NativeTypeName("uint32_t")]
        public uint subdivision_preview_levels;

        [NativeTypeName("uint32_t")]
        public uint subdivision_render_levels;

        public ufbx_subdivision_display_mode subdivision_display_mode;

        public ufbx_subdivision_boundary subdivision_boundary;

        public ufbx_subdivision_boundary subdivision_uv_boundary;

        [NativeTypeName("_Bool")]
        public bool reversed_winding;

        [NativeTypeName("_Bool")]
        public bool generated_normals;

        [NativeTypeName("_Bool")]
        public bool subdivision_evaluated;

        public ufbx_subdivision_result* subdivision_result;

        [NativeTypeName("_Bool")]
        public bool from_tessellated_nurbs;

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
            [NativeTypeName("__AnonymousRecord_ufbx_L1254_C32")]
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
