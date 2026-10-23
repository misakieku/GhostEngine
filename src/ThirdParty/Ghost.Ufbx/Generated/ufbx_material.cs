using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    /// <include file='ufbx_material.xml' path='doc/member[@name="ufbx_material"]/*' />
    public unsafe partial struct ufbx_material
    {
        /// <include file='ufbx_material.xml' path='doc/member[@name="ufbx_material.Anonymous"]/*' />
        [NativeTypeName("__AnonymousRecord_ufbx_L2639_C2")]
        public _Anonymous_e__Union Anonymous;

        /// <include file='ufbx_material.xml' path='doc/member[@name="ufbx_material.fbx"]/*' />
        public ufbx_material_fbx_maps fbx;

        /// <include file='ufbx_material.xml' path='doc/member[@name="ufbx_material.pbr"]/*' />
        public ufbx_material_pbr_maps pbr;

        /// <include file='ufbx_material.xml' path='doc/member[@name="ufbx_material.features"]/*' />
        public ufbx_material_features features;

        /// <include file='ufbx_material.xml' path='doc/member[@name="ufbx_material.shader_type"]/*' />
        public ufbx_shader_type shader_type;

        /// <include file='ufbx_material.xml' path='doc/member[@name="ufbx_material.shader"]/*' />
        public ufbx_shader* shader;

        /// <include file='ufbx_material.xml' path='doc/member[@name="ufbx_material.shading_model_name"]/*' />
        public ufbx_string shading_model_name;

        /// <include file='ufbx_material.xml' path='doc/member[@name="ufbx_material.shader_prop_prefix"]/*' />
        public ufbx_string shader_prop_prefix;

        /// <include file='ufbx_material.xml' path='doc/member[@name="ufbx_material.textures"]/*' />
        public ufbx_material_texture_list textures;

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
            [NativeTypeName("__AnonymousRecord_ufbx_L2639_C32")]
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
