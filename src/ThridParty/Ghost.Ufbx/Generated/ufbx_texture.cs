using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_texture
    {
        [NativeTypeName("__AnonymousRecord_ufbx_L2885_C2")]
        public _Anonymous_e__Union Anonymous;

        public ufbx_texture_type type;

        public ufbx_string filename;

        public ufbx_string absolute_filename;

        public ufbx_string relative_filename;

        public ufbx_blob raw_filename;

        public ufbx_blob raw_absolute_filename;

        public ufbx_blob raw_relative_filename;

        public ufbx_blob content;

        public ufbx_video* video;

        [NativeTypeName("uint32_t")]
        public uint file_index;

        [NativeTypeName("_Bool")]
        public bool has_file;

        public ufbx_texture_layer_list layers;

        public ufbx_shader_texture* shader;

        public ufbx_texture_list file_textures;

        public ufbx_string uv_set;

        public ufbx_wrap_mode wrap_u;

        public ufbx_wrap_mode wrap_v;

        [NativeTypeName("_Bool")]
        public bool has_uv_transform;

        public ufbx_transform uv_transform;

        public ufbx_matrix texture_to_uv;

        public ufbx_matrix uv_to_texture;

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
            [NativeTypeName("__AnonymousRecord_ufbx_L2885_C32")]
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
