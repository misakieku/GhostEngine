using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_anim_layer
    {
        [NativeTypeName("__AnonymousRecord_ufbx_L3111_C2")]
        public _Anonymous_e__Union Anonymous;

        [NativeTypeName("ufbx_real")]
        public float weight;

        [NativeTypeName("_Bool")]
        public bool weight_is_animated;

        [NativeTypeName("_Bool")]
        public bool blended;

        [NativeTypeName("_Bool")]
        public bool additive;

        [NativeTypeName("_Bool")]
        public bool compose_rotation;

        [NativeTypeName("_Bool")]
        public bool compose_scale;

        public ufbx_anim_value_list anim_values;

        public ufbx_anim_prop_list anim_props;

        public ufbx_anim* anim;

        [NativeTypeName("uint32_t")]
        public uint _min_element_id;

        [NativeTypeName("uint32_t")]
        public uint _max_element_id;

        [NativeTypeName("uint32_t[4]")]
        public __element_id_bitmask_e__FixedBuffer _element_id_bitmask;

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
            [NativeTypeName("__AnonymousRecord_ufbx_L3111_C32")]
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

        [InlineArray(4)]
        public partial struct __element_id_bitmask_e__FixedBuffer
        {
            public uint e0;
        }
    }
}
