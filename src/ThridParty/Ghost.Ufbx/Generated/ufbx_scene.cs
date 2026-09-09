using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    /// <include file='ufbx_scene.xml' path='doc/member[@name="ufbx_scene"]/*' />
    public unsafe partial struct ufbx_scene
    {
        /// <include file='ufbx_scene.xml' path='doc/member[@name="ufbx_scene.metadata"]/*' />
        public ufbx_metadata metadata;

        /// <include file='ufbx_scene.xml' path='doc/member[@name="ufbx_scene.settings"]/*' />
        public ufbx_scene_settings settings;

        /// <include file='ufbx_scene.xml' path='doc/member[@name="ufbx_scene.root_node"]/*' />
        public ufbx_node* root_node;

        /// <include file='ufbx_scene.xml' path='doc/member[@name="ufbx_scene.anim"]/*' />
        public ufbx_anim* anim;

        /// <include file='ufbx_scene.xml' path='doc/member[@name="ufbx_scene.Anonymous"]/*' />
        [NativeTypeName("__AnonymousRecord_ufbx_L3947_C2")]
        public _Anonymous_e__Union Anonymous;

        /// <include file='ufbx_scene.xml' path='doc/member[@name="ufbx_scene.texture_files"]/*' />
        public ufbx_texture_file_list texture_files;

        /// <include file='ufbx_scene.xml' path='doc/member[@name="ufbx_scene.elements"]/*' />
        public ufbx_element_list elements;

        /// <include file='ufbx_scene.xml' path='doc/member[@name="ufbx_scene.connections_src"]/*' />
        public ufbx_connection_list connections_src;

        /// <include file='ufbx_scene.xml' path='doc/member[@name="ufbx_scene.connections_dst"]/*' />
        public ufbx_connection_list connections_dst;

        /// <include file='ufbx_scene.xml' path='doc/member[@name="ufbx_scene.elements_by_name"]/*' />
        public ufbx_name_element_list elements_by_name;

        /// <include file='ufbx_scene.xml' path='doc/member[@name="ufbx_scene.dom_root"]/*' />
        public ufbx_dom_node* dom_root;

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.unknowns"]/*' />
        [UnscopedRef]
        public ref ufbx_unknown_list unknowns
        {
            get
            {
                return ref Anonymous.Anonymous.unknowns;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.nodes"]/*' />
        [UnscopedRef]
        public ref ufbx_node_list nodes
        {
            get
            {
                return ref Anonymous.Anonymous.nodes;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.meshes"]/*' />
        [UnscopedRef]
        public ref ufbx_mesh_list meshes
        {
            get
            {
                return ref Anonymous.Anonymous.meshes;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.lights"]/*' />
        [UnscopedRef]
        public ref ufbx_light_list lights
        {
            get
            {
                return ref Anonymous.Anonymous.lights;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.cameras"]/*' />
        [UnscopedRef]
        public ref ufbx_camera_list cameras
        {
            get
            {
                return ref Anonymous.Anonymous.cameras;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.bones"]/*' />
        [UnscopedRef]
        public ref ufbx_bone_list bones
        {
            get
            {
                return ref Anonymous.Anonymous.bones;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.empties"]/*' />
        [UnscopedRef]
        public ref ufbx_empty_list empties
        {
            get
            {
                return ref Anonymous.Anonymous.empties;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.line_curves"]/*' />
        [UnscopedRef]
        public ref ufbx_line_curve_list line_curves
        {
            get
            {
                return ref Anonymous.Anonymous.line_curves;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.nurbs_curves"]/*' />
        [UnscopedRef]
        public ref ufbx_nurbs_curve_list nurbs_curves
        {
            get
            {
                return ref Anonymous.Anonymous.nurbs_curves;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.nurbs_surfaces"]/*' />
        [UnscopedRef]
        public ref ufbx_nurbs_surface_list nurbs_surfaces
        {
            get
            {
                return ref Anonymous.Anonymous.nurbs_surfaces;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.nurbs_trim_surfaces"]/*' />
        [UnscopedRef]
        public ref ufbx_nurbs_trim_surface_list nurbs_trim_surfaces
        {
            get
            {
                return ref Anonymous.Anonymous.nurbs_trim_surfaces;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.nurbs_trim_boundaries"]/*' />
        [UnscopedRef]
        public ref ufbx_nurbs_trim_boundary_list nurbs_trim_boundaries
        {
            get
            {
                return ref Anonymous.Anonymous.nurbs_trim_boundaries;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.procedural_geometries"]/*' />
        [UnscopedRef]
        public ref ufbx_procedural_geometry_list procedural_geometries
        {
            get
            {
                return ref Anonymous.Anonymous.procedural_geometries;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.stereo_cameras"]/*' />
        [UnscopedRef]
        public ref ufbx_stereo_camera_list stereo_cameras
        {
            get
            {
                return ref Anonymous.Anonymous.stereo_cameras;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.camera_switchers"]/*' />
        [UnscopedRef]
        public ref ufbx_camera_switcher_list camera_switchers
        {
            get
            {
                return ref Anonymous.Anonymous.camera_switchers;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.markers"]/*' />
        [UnscopedRef]
        public ref ufbx_marker_list markers
        {
            get
            {
                return ref Anonymous.Anonymous.markers;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.lod_groups"]/*' />
        [UnscopedRef]
        public ref ufbx_lod_group_list lod_groups
        {
            get
            {
                return ref Anonymous.Anonymous.lod_groups;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.skin_deformers"]/*' />
        [UnscopedRef]
        public ref ufbx_skin_deformer_list skin_deformers
        {
            get
            {
                return ref Anonymous.Anonymous.skin_deformers;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.skin_clusters"]/*' />
        [UnscopedRef]
        public ref ufbx_skin_cluster_list skin_clusters
        {
            get
            {
                return ref Anonymous.Anonymous.skin_clusters;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.blend_deformers"]/*' />
        [UnscopedRef]
        public ref ufbx_blend_deformer_list blend_deformers
        {
            get
            {
                return ref Anonymous.Anonymous.blend_deformers;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.blend_channels"]/*' />
        [UnscopedRef]
        public ref ufbx_blend_channel_list blend_channels
        {
            get
            {
                return ref Anonymous.Anonymous.blend_channels;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.blend_shapes"]/*' />
        [UnscopedRef]
        public ref ufbx_blend_shape_list blend_shapes
        {
            get
            {
                return ref Anonymous.Anonymous.blend_shapes;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.cache_deformers"]/*' />
        [UnscopedRef]
        public ref ufbx_cache_deformer_list cache_deformers
        {
            get
            {
                return ref Anonymous.Anonymous.cache_deformers;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.cache_files"]/*' />
        [UnscopedRef]
        public ref ufbx_cache_file_list cache_files
        {
            get
            {
                return ref Anonymous.Anonymous.cache_files;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.materials"]/*' />
        [UnscopedRef]
        public ref ufbx_material_list materials
        {
            get
            {
                return ref Anonymous.Anonymous.materials;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.textures"]/*' />
        [UnscopedRef]
        public ref ufbx_texture_list textures
        {
            get
            {
                return ref Anonymous.Anonymous.textures;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.videos"]/*' />
        [UnscopedRef]
        public ref ufbx_video_list videos
        {
            get
            {
                return ref Anonymous.Anonymous.videos;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.shaders"]/*' />
        [UnscopedRef]
        public ref ufbx_shader_list shaders
        {
            get
            {
                return ref Anonymous.Anonymous.shaders;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.shader_bindings"]/*' />
        [UnscopedRef]
        public ref ufbx_shader_binding_list shader_bindings
        {
            get
            {
                return ref Anonymous.Anonymous.shader_bindings;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.anim_stacks"]/*' />
        [UnscopedRef]
        public ref ufbx_anim_stack_list anim_stacks
        {
            get
            {
                return ref Anonymous.Anonymous.anim_stacks;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.anim_layers"]/*' />
        [UnscopedRef]
        public ref ufbx_anim_layer_list anim_layers
        {
            get
            {
                return ref Anonymous.Anonymous.anim_layers;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.anim_values"]/*' />
        [UnscopedRef]
        public ref ufbx_anim_value_list anim_values
        {
            get
            {
                return ref Anonymous.Anonymous.anim_values;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.anim_curves"]/*' />
        [UnscopedRef]
        public ref ufbx_anim_curve_list anim_curves
        {
            get
            {
                return ref Anonymous.Anonymous.anim_curves;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.display_layers"]/*' />
        [UnscopedRef]
        public ref ufbx_display_layer_list display_layers
        {
            get
            {
                return ref Anonymous.Anonymous.display_layers;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.selection_sets"]/*' />
        [UnscopedRef]
        public ref ufbx_selection_set_list selection_sets
        {
            get
            {
                return ref Anonymous.Anonymous.selection_sets;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.selection_nodes"]/*' />
        [UnscopedRef]
        public ref ufbx_selection_node_list selection_nodes
        {
            get
            {
                return ref Anonymous.Anonymous.selection_nodes;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.characters"]/*' />
        [UnscopedRef]
        public ref ufbx_character_list characters
        {
            get
            {
                return ref Anonymous.Anonymous.characters;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.constraints"]/*' />
        [UnscopedRef]
        public ref ufbx_constraint_list constraints
        {
            get
            {
                return ref Anonymous.Anonymous.constraints;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.audio_layers"]/*' />
        [UnscopedRef]
        public ref ufbx_audio_layer_list audio_layers
        {
            get
            {
                return ref Anonymous.Anonymous.audio_layers;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.audio_clips"]/*' />
        [UnscopedRef]
        public ref ufbx_audio_clip_list audio_clips
        {
            get
            {
                return ref Anonymous.Anonymous.audio_clips;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.poses"]/*' />
        [UnscopedRef]
        public ref ufbx_pose_list poses
        {
            get
            {
                return ref Anonymous.Anonymous.poses;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.metadata_objects"]/*' />
        [UnscopedRef]
        public ref ufbx_metadata_object_list metadata_objects
        {
            get
            {
                return ref Anonymous.Anonymous.metadata_objects;
            }
        }

        /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union.elements_by_type"]/*' />
        [UnscopedRef]
        public Span<ufbx_element_list> elements_by_type
        {
            get
            {
                return Anonymous.elements_by_type;
            }
        }

        /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union"]/*' />
        [StructLayout(LayoutKind.Explicit)]
        public partial struct _Anonymous_e__Union
        {
            /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union.Anonymous"]/*' />
            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_ufbx_L3948_C3")]
            public _Anonymous_e__Struct Anonymous;

            /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union.elements_by_type"]/*' />
            [FieldOffset(0)]
            [NativeTypeName("ufbx_element_list[42]")]
            public _elements_by_type_e__FixedBuffer elements_by_type;

            /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct"]/*' />
            public partial struct _Anonymous_e__Struct
            {
                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.unknowns"]/*' />
                public ufbx_unknown_list unknowns;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.nodes"]/*' />
                public ufbx_node_list nodes;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.meshes"]/*' />
                public ufbx_mesh_list meshes;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.lights"]/*' />
                public ufbx_light_list lights;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.cameras"]/*' />
                public ufbx_camera_list cameras;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.bones"]/*' />
                public ufbx_bone_list bones;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.empties"]/*' />
                public ufbx_empty_list empties;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.line_curves"]/*' />
                public ufbx_line_curve_list line_curves;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.nurbs_curves"]/*' />
                public ufbx_nurbs_curve_list nurbs_curves;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.nurbs_surfaces"]/*' />
                public ufbx_nurbs_surface_list nurbs_surfaces;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.nurbs_trim_surfaces"]/*' />
                public ufbx_nurbs_trim_surface_list nurbs_trim_surfaces;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.nurbs_trim_boundaries"]/*' />
                public ufbx_nurbs_trim_boundary_list nurbs_trim_boundaries;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.procedural_geometries"]/*' />
                public ufbx_procedural_geometry_list procedural_geometries;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.stereo_cameras"]/*' />
                public ufbx_stereo_camera_list stereo_cameras;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.camera_switchers"]/*' />
                public ufbx_camera_switcher_list camera_switchers;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.markers"]/*' />
                public ufbx_marker_list markers;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.lod_groups"]/*' />
                public ufbx_lod_group_list lod_groups;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.skin_deformers"]/*' />
                public ufbx_skin_deformer_list skin_deformers;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.skin_clusters"]/*' />
                public ufbx_skin_cluster_list skin_clusters;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.blend_deformers"]/*' />
                public ufbx_blend_deformer_list blend_deformers;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.blend_channels"]/*' />
                public ufbx_blend_channel_list blend_channels;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.blend_shapes"]/*' />
                public ufbx_blend_shape_list blend_shapes;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.cache_deformers"]/*' />
                public ufbx_cache_deformer_list cache_deformers;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.cache_files"]/*' />
                public ufbx_cache_file_list cache_files;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.materials"]/*' />
                public ufbx_material_list materials;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.textures"]/*' />
                public ufbx_texture_list textures;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.videos"]/*' />
                public ufbx_video_list videos;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.shaders"]/*' />
                public ufbx_shader_list shaders;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.shader_bindings"]/*' />
                public ufbx_shader_binding_list shader_bindings;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.anim_stacks"]/*' />
                public ufbx_anim_stack_list anim_stacks;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.anim_layers"]/*' />
                public ufbx_anim_layer_list anim_layers;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.anim_values"]/*' />
                public ufbx_anim_value_list anim_values;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.anim_curves"]/*' />
                public ufbx_anim_curve_list anim_curves;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.display_layers"]/*' />
                public ufbx_display_layer_list display_layers;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.selection_sets"]/*' />
                public ufbx_selection_set_list selection_sets;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.selection_nodes"]/*' />
                public ufbx_selection_node_list selection_nodes;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.characters"]/*' />
                public ufbx_character_list characters;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.constraints"]/*' />
                public ufbx_constraint_list constraints;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.audio_layers"]/*' />
                public ufbx_audio_layer_list audio_layers;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.audio_clips"]/*' />
                public ufbx_audio_clip_list audio_clips;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.poses"]/*' />
                public ufbx_pose_list poses;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.metadata_objects"]/*' />
                public ufbx_metadata_object_list metadata_objects;
            }

            /// <include file='_elements_by_type_e__FixedBuffer.xml' path='doc/member[@name="_elements_by_type_e__FixedBuffer"]/*' />
            [InlineArray(42)]
            public partial struct _elements_by_type_e__FixedBuffer
            {
                public ufbx_element_list e0;
            }
        }
    }
}
