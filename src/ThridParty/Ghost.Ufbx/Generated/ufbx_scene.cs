using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_scene
    {
        public ufbx_metadata metadata;

        public ufbx_scene_settings settings;

        public ufbx_node* root_node;

        public ufbx_anim* anim;

        [NativeTypeName("__AnonymousRecord_ufbx_L3936_C2")]
        public _Anonymous_e__Union Anonymous;

        public ufbx_texture_file_list texture_files;

        public ufbx_element_list elements;

        public ufbx_connection_list connections_src;

        public ufbx_connection_list connections_dst;

        public ufbx_name_element_list elements_by_name;

        public ufbx_dom_node* dom_root;

        [UnscopedRef]
        public ref ufbx_unknown_list unknowns
        {
            get
            {
                return ref Anonymous.Anonymous.unknowns;
            }
        }

        [UnscopedRef]
        public ref ufbx_node_list nodes
        {
            get
            {
                return ref Anonymous.Anonymous.nodes;
            }
        }

        [UnscopedRef]
        public ref ufbx_mesh_list meshes
        {
            get
            {
                return ref Anonymous.Anonymous.meshes;
            }
        }

        [UnscopedRef]
        public ref ufbx_light_list lights
        {
            get
            {
                return ref Anonymous.Anonymous.lights;
            }
        }

        [UnscopedRef]
        public ref ufbx_camera_list cameras
        {
            get
            {
                return ref Anonymous.Anonymous.cameras;
            }
        }

        [UnscopedRef]
        public ref ufbx_bone_list bones
        {
            get
            {
                return ref Anonymous.Anonymous.bones;
            }
        }

        [UnscopedRef]
        public ref ufbx_empty_list empties
        {
            get
            {
                return ref Anonymous.Anonymous.empties;
            }
        }

        [UnscopedRef]
        public ref ufbx_line_curve_list line_curves
        {
            get
            {
                return ref Anonymous.Anonymous.line_curves;
            }
        }

        [UnscopedRef]
        public ref ufbx_nurbs_curve_list nurbs_curves
        {
            get
            {
                return ref Anonymous.Anonymous.nurbs_curves;
            }
        }

        [UnscopedRef]
        public ref ufbx_nurbs_surface_list nurbs_surfaces
        {
            get
            {
                return ref Anonymous.Anonymous.nurbs_surfaces;
            }
        }

        [UnscopedRef]
        public ref ufbx_nurbs_trim_surface_list nurbs_trim_surfaces
        {
            get
            {
                return ref Anonymous.Anonymous.nurbs_trim_surfaces;
            }
        }

        [UnscopedRef]
        public ref ufbx_nurbs_trim_boundary_list nurbs_trim_boundaries
        {
            get
            {
                return ref Anonymous.Anonymous.nurbs_trim_boundaries;
            }
        }

        [UnscopedRef]
        public ref ufbx_procedural_geometry_list procedural_geometries
        {
            get
            {
                return ref Anonymous.Anonymous.procedural_geometries;
            }
        }

        [UnscopedRef]
        public ref ufbx_stereo_camera_list stereo_cameras
        {
            get
            {
                return ref Anonymous.Anonymous.stereo_cameras;
            }
        }

        [UnscopedRef]
        public ref ufbx_camera_switcher_list camera_switchers
        {
            get
            {
                return ref Anonymous.Anonymous.camera_switchers;
            }
        }

        [UnscopedRef]
        public ref ufbx_marker_list markers
        {
            get
            {
                return ref Anonymous.Anonymous.markers;
            }
        }

        [UnscopedRef]
        public ref ufbx_lod_group_list lod_groups
        {
            get
            {
                return ref Anonymous.Anonymous.lod_groups;
            }
        }

        [UnscopedRef]
        public ref ufbx_skin_deformer_list skin_deformers
        {
            get
            {
                return ref Anonymous.Anonymous.skin_deformers;
            }
        }

        [UnscopedRef]
        public ref ufbx_skin_cluster_list skin_clusters
        {
            get
            {
                return ref Anonymous.Anonymous.skin_clusters;
            }
        }

        [UnscopedRef]
        public ref ufbx_blend_deformer_list blend_deformers
        {
            get
            {
                return ref Anonymous.Anonymous.blend_deformers;
            }
        }

        [UnscopedRef]
        public ref ufbx_blend_channel_list blend_channels
        {
            get
            {
                return ref Anonymous.Anonymous.blend_channels;
            }
        }

        [UnscopedRef]
        public ref ufbx_blend_shape_list blend_shapes
        {
            get
            {
                return ref Anonymous.Anonymous.blend_shapes;
            }
        }

        [UnscopedRef]
        public ref ufbx_cache_deformer_list cache_deformers
        {
            get
            {
                return ref Anonymous.Anonymous.cache_deformers;
            }
        }

        [UnscopedRef]
        public ref ufbx_cache_file_list cache_files
        {
            get
            {
                return ref Anonymous.Anonymous.cache_files;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_list materials
        {
            get
            {
                return ref Anonymous.Anonymous.materials;
            }
        }

        [UnscopedRef]
        public ref ufbx_texture_list textures
        {
            get
            {
                return ref Anonymous.Anonymous.textures;
            }
        }

        [UnscopedRef]
        public ref ufbx_video_list videos
        {
            get
            {
                return ref Anonymous.Anonymous.videos;
            }
        }

        [UnscopedRef]
        public ref ufbx_shader_list shaders
        {
            get
            {
                return ref Anonymous.Anonymous.shaders;
            }
        }

        [UnscopedRef]
        public ref ufbx_shader_binding_list shader_bindings
        {
            get
            {
                return ref Anonymous.Anonymous.shader_bindings;
            }
        }

        [UnscopedRef]
        public ref ufbx_anim_stack_list anim_stacks
        {
            get
            {
                return ref Anonymous.Anonymous.anim_stacks;
            }
        }

        [UnscopedRef]
        public ref ufbx_anim_layer_list anim_layers
        {
            get
            {
                return ref Anonymous.Anonymous.anim_layers;
            }
        }

        [UnscopedRef]
        public ref ufbx_anim_value_list anim_values
        {
            get
            {
                return ref Anonymous.Anonymous.anim_values;
            }
        }

        [UnscopedRef]
        public ref ufbx_anim_curve_list anim_curves
        {
            get
            {
                return ref Anonymous.Anonymous.anim_curves;
            }
        }

        [UnscopedRef]
        public ref ufbx_display_layer_list display_layers
        {
            get
            {
                return ref Anonymous.Anonymous.display_layers;
            }
        }

        [UnscopedRef]
        public ref ufbx_selection_set_list selection_sets
        {
            get
            {
                return ref Anonymous.Anonymous.selection_sets;
            }
        }

        [UnscopedRef]
        public ref ufbx_selection_node_list selection_nodes
        {
            get
            {
                return ref Anonymous.Anonymous.selection_nodes;
            }
        }

        [UnscopedRef]
        public ref ufbx_character_list characters
        {
            get
            {
                return ref Anonymous.Anonymous.characters;
            }
        }

        [UnscopedRef]
        public ref ufbx_constraint_list constraints
        {
            get
            {
                return ref Anonymous.Anonymous.constraints;
            }
        }

        [UnscopedRef]
        public ref ufbx_audio_layer_list audio_layers
        {
            get
            {
                return ref Anonymous.Anonymous.audio_layers;
            }
        }

        [UnscopedRef]
        public ref ufbx_audio_clip_list audio_clips
        {
            get
            {
                return ref Anonymous.Anonymous.audio_clips;
            }
        }

        [UnscopedRef]
        public ref ufbx_pose_list poses
        {
            get
            {
                return ref Anonymous.Anonymous.poses;
            }
        }

        [UnscopedRef]
        public ref ufbx_metadata_object_list metadata_objects
        {
            get
            {
                return ref Anonymous.Anonymous.metadata_objects;
            }
        }

        [UnscopedRef]
        public Span<ufbx_element_list> elements_by_type
        {
            get
            {
                return Anonymous.elements_by_type;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_ufbx_L3937_C3")]
            public _Anonymous_e__Struct Anonymous;

            [FieldOffset(0)]
            [NativeTypeName("ufbx_element_list[42]")]
            public _elements_by_type_e__FixedBuffer elements_by_type;

            public partial struct _Anonymous_e__Struct
            {
                public ufbx_unknown_list unknowns;

                public ufbx_node_list nodes;

                public ufbx_mesh_list meshes;

                public ufbx_light_list lights;

                public ufbx_camera_list cameras;

                public ufbx_bone_list bones;

                public ufbx_empty_list empties;

                public ufbx_line_curve_list line_curves;

                public ufbx_nurbs_curve_list nurbs_curves;

                public ufbx_nurbs_surface_list nurbs_surfaces;

                public ufbx_nurbs_trim_surface_list nurbs_trim_surfaces;

                public ufbx_nurbs_trim_boundary_list nurbs_trim_boundaries;

                public ufbx_procedural_geometry_list procedural_geometries;

                public ufbx_stereo_camera_list stereo_cameras;

                public ufbx_camera_switcher_list camera_switchers;

                public ufbx_marker_list markers;

                public ufbx_lod_group_list lod_groups;

                public ufbx_skin_deformer_list skin_deformers;

                public ufbx_skin_cluster_list skin_clusters;

                public ufbx_blend_deformer_list blend_deformers;

                public ufbx_blend_channel_list blend_channels;

                public ufbx_blend_shape_list blend_shapes;

                public ufbx_cache_deformer_list cache_deformers;

                public ufbx_cache_file_list cache_files;

                public ufbx_material_list materials;

                public ufbx_texture_list textures;

                public ufbx_video_list videos;

                public ufbx_shader_list shaders;

                public ufbx_shader_binding_list shader_bindings;

                public ufbx_anim_stack_list anim_stacks;

                public ufbx_anim_layer_list anim_layers;

                public ufbx_anim_value_list anim_values;

                public ufbx_anim_curve_list anim_curves;

                public ufbx_display_layer_list display_layers;

                public ufbx_selection_set_list selection_sets;

                public ufbx_selection_node_list selection_nodes;

                public ufbx_character_list characters;

                public ufbx_constraint_list constraints;

                public ufbx_audio_layer_list audio_layers;

                public ufbx_audio_clip_list audio_clips;

                public ufbx_pose_list poses;

                public ufbx_metadata_object_list metadata_objects;
            }

            [InlineArray(42)]
            public partial struct _elements_by_type_e__FixedBuffer
            {
                public ufbx_element_list e0;
            }
        }
    }
}
