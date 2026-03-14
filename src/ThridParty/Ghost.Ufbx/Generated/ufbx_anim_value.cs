using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    public partial struct ufbx_anim_value
    {
        [NativeTypeName("__AnonymousRecord_ufbx_L3136_C2")]
        public _Anonymous_e__Union Anonymous;

        [NativeTypeName("ufbx_vec3")]
        public Misaki.HighPerformance.Mathematics.float3 default_value;

        [NativeTypeName("ufbx_anim_curve *[3]")]
        public _curves_e__FixedBuffer curves;

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
            [NativeTypeName("__AnonymousRecord_ufbx_L3136_C32")]
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

        public unsafe partial struct _curves_e__FixedBuffer
        {
            public ufbx_anim_curve* e0;
            public ufbx_anim_curve* e1;
            public ufbx_anim_curve* e2;

            public ref ufbx_anim_curve* this[int index]
            {
                get
                {
                    fixed (ufbx_anim_curve** pThis = &e0)
                    {
                        return ref pThis[index];
                    }
                }
            }
        }
    }
}
