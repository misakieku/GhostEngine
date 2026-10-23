using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node"]/*' />
    public unsafe partial struct ufbx_node
    {
        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.Anonymous"]/*' />
        [NativeTypeName("__AnonymousRecord_ufbx_L845_C2")]
        public _Anonymous_e__Union Anonymous;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.parent"]/*' />
        public ufbx_node* parent;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.children"]/*' />
        public ufbx_node_list children;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.mesh"]/*' />
        public ufbx_mesh* mesh;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.light"]/*' />
        public ufbx_light* light;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.camera"]/*' />
        public ufbx_camera* camera;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.bone"]/*' />
        public ufbx_bone* bone;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.attrib"]/*' />
        public ufbx_element* attrib;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.geometry_transform_helper"]/*' />
        public ufbx_node* geometry_transform_helper;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.scale_helper"]/*' />
        public ufbx_node* scale_helper;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.attrib_type"]/*' />
        public ufbx_element_type attrib_type;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.all_attribs"]/*' />
        public ufbx_element_list all_attribs;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.inherit_mode"]/*' />
        public ufbx_inherit_mode inherit_mode;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.original_inherit_mode"]/*' />
        public ufbx_inherit_mode original_inherit_mode;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.local_transform"]/*' />
        public ufbx_transform local_transform;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.geometry_transform"]/*' />
        public ufbx_transform geometry_transform;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.inherit_scale"]/*' />
        public ufbx_vec3 inherit_scale;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.inherit_scale_node"]/*' />
        public ufbx_node* inherit_scale_node;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.rotation_order"]/*' />
        public ufbx_rotation_order rotation_order;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.euler_rotation"]/*' />
        public ufbx_vec3 euler_rotation;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.node_to_parent"]/*' />
        public ufbx_matrix node_to_parent;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.node_to_world"]/*' />
        public ufbx_matrix node_to_world;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.geometry_to_node"]/*' />
        public ufbx_matrix geometry_to_node;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.geometry_to_world"]/*' />
        public ufbx_matrix geometry_to_world;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.unscaled_node_to_world"]/*' />
        public ufbx_matrix unscaled_node_to_world;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.adjust_pre_translation"]/*' />
        public ufbx_vec3 adjust_pre_translation;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.adjust_pre_rotation"]/*' />
        public ufbx_quat adjust_pre_rotation;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.adjust_pre_scale"]/*' />
        [NativeTypeName("ufbx_real")]
        public float adjust_pre_scale;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.adjust_post_rotation"]/*' />
        public ufbx_quat adjust_post_rotation;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.adjust_post_scale"]/*' />
        [NativeTypeName("ufbx_real")]
        public float adjust_post_scale;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.adjust_translation_scale"]/*' />
        [NativeTypeName("ufbx_real")]
        public float adjust_translation_scale;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.adjust_mirror_axis"]/*' />
        public ufbx_mirror_axis adjust_mirror_axis;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.materials"]/*' />
        public ufbx_material_list materials;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.bind_pose"]/*' />
        public ufbx_pose* bind_pose;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.visible"]/*' />
        [NativeTypeName("_Bool")]
        public bool visible;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.is_root"]/*' />
        [NativeTypeName("_Bool")]
        public bool is_root;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.has_geometry_transform"]/*' />
        [NativeTypeName("_Bool")]
        public bool has_geometry_transform;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.use_rotation_space"]/*' />
        [NativeTypeName("_Bool")]
        public bool use_rotation_space;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.has_adjust_transform"]/*' />
        [NativeTypeName("_Bool")]
        public bool has_adjust_transform;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.has_root_adjust_transform"]/*' />
        [NativeTypeName("_Bool")]
        public bool has_root_adjust_transform;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.is_geometry_transform_helper"]/*' />
        [NativeTypeName("_Bool")]
        public bool is_geometry_transform_helper;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.is_scale_helper"]/*' />
        [NativeTypeName("_Bool")]
        public bool is_scale_helper;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.is_scale_compensate_parent"]/*' />
        [NativeTypeName("_Bool")]
        public bool is_scale_compensate_parent;

        /// <include file='ufbx_node.xml' path='doc/member[@name="ufbx_node.node_depth"]/*' />
        [NativeTypeName("uint32_t")]
        public uint node_depth;

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
            [NativeTypeName("__AnonymousRecord_ufbx_L845_C32")]
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
