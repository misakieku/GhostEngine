using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_node
    {
        [NativeTypeName("__AnonymousRecord_ufbx_L845_C2")]
        public _Anonymous_e__Union Anonymous;

        public ufbx_node* parent;

        public ufbx_node_list children;

        public ufbx_mesh* mesh;

        public ufbx_light* light;

        public ufbx_camera* camera;

        public ufbx_bone* bone;

        public ufbx_element* attrib;

        public ufbx_node* geometry_transform_helper;

        public ufbx_node* scale_helper;

        public ufbx_element_type attrib_type;

        public ufbx_element_list all_attribs;

        public ufbx_inherit_mode inherit_mode;

        public ufbx_inherit_mode original_inherit_mode;

        public ufbx_transform local_transform;

        public ufbx_transform geometry_transform;

        public ufbx_vec3 inherit_scale;

        public ufbx_node* inherit_scale_node;

        public ufbx_rotation_order rotation_order;

        public ufbx_vec3 euler_rotation;

        public ufbx_matrix node_to_parent;

        public ufbx_matrix node_to_world;

        public ufbx_matrix geometry_to_node;

        public ufbx_matrix geometry_to_world;

        public ufbx_matrix unscaled_node_to_world;

        public ufbx_vec3 adjust_pre_translation;

        public ufbx_quat adjust_pre_rotation;

        [NativeTypeName("ufbx_real")]
        public float adjust_pre_scale;

        public ufbx_quat adjust_post_rotation;

        [NativeTypeName("ufbx_real")]
        public float adjust_post_scale;

        [NativeTypeName("ufbx_real")]
        public float adjust_translation_scale;

        public ufbx_mirror_axis adjust_mirror_axis;

        public ufbx_material_list materials;

        public ufbx_pose* bind_pose;

        [NativeTypeName("_Bool")]
        public bool visible;

        [NativeTypeName("_Bool")]
        public bool is_root;

        [NativeTypeName("_Bool")]
        public bool has_geometry_transform;

        [NativeTypeName("_Bool")]
        public bool has_adjust_transform;

        [NativeTypeName("_Bool")]
        public bool has_root_adjust_transform;

        [NativeTypeName("_Bool")]
        public bool is_geometry_transform_helper;

        [NativeTypeName("_Bool")]
        public bool is_scale_helper;

        [NativeTypeName("_Bool")]
        public bool is_scale_compensate_parent;

        [NativeTypeName("uint32_t")]
        public uint node_depth;

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
            [NativeTypeName("__AnonymousRecord_ufbx_L845_C32")]
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
