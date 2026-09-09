using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    /// <include file='ufbx_anim_layer.xml' path='doc/member[@name="ufbx_anim_layer"]/*' />
    public unsafe partial struct ufbx_anim_layer
    {
        /// <include file='ufbx_anim_layer.xml' path='doc/member[@name="ufbx_anim_layer.Anonymous"]/*' />
        [NativeTypeName("__AnonymousRecord_ufbx_L3117_C2")]
        public _Anonymous_e__Union Anonymous;

        /// <include file='ufbx_anim_layer.xml' path='doc/member[@name="ufbx_anim_layer.weight"]/*' />
        [NativeTypeName("ufbx_real")]
        public float weight;

        /// <include file='ufbx_anim_layer.xml' path='doc/member[@name="ufbx_anim_layer.weight_is_animated"]/*' />
        [NativeTypeName("_Bool")]
        public bool weight_is_animated;

        /// <include file='ufbx_anim_layer.xml' path='doc/member[@name="ufbx_anim_layer.blended"]/*' />
        [NativeTypeName("_Bool")]
        public bool blended;

        /// <include file='ufbx_anim_layer.xml' path='doc/member[@name="ufbx_anim_layer.additive"]/*' />
        [NativeTypeName("_Bool")]
        public bool additive;

        /// <include file='ufbx_anim_layer.xml' path='doc/member[@name="ufbx_anim_layer.compose_rotation"]/*' />
        [NativeTypeName("_Bool")]
        public bool compose_rotation;

        /// <include file='ufbx_anim_layer.xml' path='doc/member[@name="ufbx_anim_layer.compose_scale"]/*' />
        [NativeTypeName("_Bool")]
        public bool compose_scale;

        /// <include file='ufbx_anim_layer.xml' path='doc/member[@name="ufbx_anim_layer.anim_values"]/*' />
        public ufbx_anim_value_list anim_values;

        /// <include file='ufbx_anim_layer.xml' path='doc/member[@name="ufbx_anim_layer.anim_props"]/*' />
        public ufbx_anim_prop_list anim_props;

        /// <include file='ufbx_anim_layer.xml' path='doc/member[@name="ufbx_anim_layer.anim"]/*' />
        public ufbx_anim* anim;

        /// <include file='ufbx_anim_layer.xml' path='doc/member[@name="ufbx_anim_layer._min_element_id"]/*' />
        [NativeTypeName("uint32_t")]
        public uint _min_element_id;

        /// <include file='ufbx_anim_layer.xml' path='doc/member[@name="ufbx_anim_layer._max_element_id"]/*' />
        [NativeTypeName("uint32_t")]
        public uint _max_element_id;

        /// <include file='ufbx_anim_layer.xml' path='doc/member[@name="ufbx_anim_layer._element_id_bitmask"]/*' />
        [NativeTypeName("uint32_t[4]")]
        public __element_id_bitmask_e__FixedBuffer _element_id_bitmask;

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
            [NativeTypeName("__AnonymousRecord_ufbx_L3117_C32")]
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

        /// <include file='__element_id_bitmask_e__FixedBuffer.xml' path='doc/member[@name="__element_id_bitmask_e__FixedBuffer"]/*' />
        [InlineArray(4)]
        public partial struct __element_id_bitmask_e__FixedBuffer
        {
            public uint e0;
        }
    }
}
