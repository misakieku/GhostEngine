using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    /// <include file='ufbx_texture.xml' path='doc/member[@name="ufbx_texture"]/*' />
    public unsafe partial struct ufbx_texture
    {
        /// <include file='ufbx_texture.xml' path='doc/member[@name="ufbx_texture.Anonymous"]/*' />
        [NativeTypeName("__AnonymousRecord_ufbx_L2891_C2")]
        public _Anonymous_e__Union Anonymous;

        /// <include file='ufbx_texture.xml' path='doc/member[@name="ufbx_texture.type"]/*' />
        public ufbx_texture_type type;

        /// <include file='ufbx_texture.xml' path='doc/member[@name="ufbx_texture.filename"]/*' />
        public ufbx_string filename;

        /// <include file='ufbx_texture.xml' path='doc/member[@name="ufbx_texture.absolute_filename"]/*' />
        public ufbx_string absolute_filename;

        /// <include file='ufbx_texture.xml' path='doc/member[@name="ufbx_texture.relative_filename"]/*' />
        public ufbx_string relative_filename;

        /// <include file='ufbx_texture.xml' path='doc/member[@name="ufbx_texture.raw_filename"]/*' />
        public ufbx_blob raw_filename;

        /// <include file='ufbx_texture.xml' path='doc/member[@name="ufbx_texture.raw_absolute_filename"]/*' />
        public ufbx_blob raw_absolute_filename;

        /// <include file='ufbx_texture.xml' path='doc/member[@name="ufbx_texture.raw_relative_filename"]/*' />
        public ufbx_blob raw_relative_filename;

        /// <include file='ufbx_texture.xml' path='doc/member[@name="ufbx_texture.content"]/*' />
        public ufbx_blob content;

        /// <include file='ufbx_texture.xml' path='doc/member[@name="ufbx_texture.video"]/*' />
        public ufbx_video* video;

        /// <include file='ufbx_texture.xml' path='doc/member[@name="ufbx_texture.file_index"]/*' />
        [NativeTypeName("uint32_t")]
        public uint file_index;

        /// <include file='ufbx_texture.xml' path='doc/member[@name="ufbx_texture.has_file"]/*' />
        [NativeTypeName("_Bool")]
        public bool has_file;

        /// <include file='ufbx_texture.xml' path='doc/member[@name="ufbx_texture.layers"]/*' />
        public ufbx_texture_layer_list layers;

        /// <include file='ufbx_texture.xml' path='doc/member[@name="ufbx_texture.shader"]/*' />
        public ufbx_shader_texture* shader;

        /// <include file='ufbx_texture.xml' path='doc/member[@name="ufbx_texture.file_textures"]/*' />
        public ufbx_texture_list file_textures;

        /// <include file='ufbx_texture.xml' path='doc/member[@name="ufbx_texture.uv_set"]/*' />
        public ufbx_string uv_set;

        /// <include file='ufbx_texture.xml' path='doc/member[@name="ufbx_texture.wrap_u"]/*' />
        public ufbx_wrap_mode wrap_u;

        /// <include file='ufbx_texture.xml' path='doc/member[@name="ufbx_texture.wrap_v"]/*' />
        public ufbx_wrap_mode wrap_v;

        /// <include file='ufbx_texture.xml' path='doc/member[@name="ufbx_texture.has_uv_transform"]/*' />
        [NativeTypeName("_Bool")]
        public bool has_uv_transform;

        /// <include file='ufbx_texture.xml' path='doc/member[@name="ufbx_texture.uv_transform"]/*' />
        public ufbx_transform uv_transform;

        /// <include file='ufbx_texture.xml' path='doc/member[@name="ufbx_texture.texture_to_uv"]/*' />
        public ufbx_matrix texture_to_uv;

        /// <include file='ufbx_texture.xml' path='doc/member[@name="ufbx_texture.uv_to_texture"]/*' />
        public ufbx_matrix uv_to_texture;

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
            [NativeTypeName("__AnonymousRecord_ufbx_L2891_C32")]
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
