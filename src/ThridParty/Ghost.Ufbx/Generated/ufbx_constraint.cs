using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_constraint
    {
        [NativeTypeName("__AnonymousRecord_ufbx_L3349_C2")]
        public _Anonymous_e__Union Anonymous;

        public ufbx_constraint_type type;

        public ufbx_string type_name;

        public ufbx_node* node;

        public ufbx_constraint_target_list targets;

        [NativeTypeName("ufbx_real")]
        public float weight;

        [NativeTypeName("_Bool")]
        public bool active;

        [NativeTypeName("_Bool[3]")]
        public _constrain_translation_e__FixedBuffer constrain_translation;

        [NativeTypeName("_Bool[3]")]
        public _constrain_rotation_e__FixedBuffer constrain_rotation;

        [NativeTypeName("_Bool[3]")]
        public _constrain_scale_e__FixedBuffer constrain_scale;

        public ufbx_transform transform_offset;

        [NativeTypeName("ufbx_vec3")]
        public Misaki.HighPerformance.Mathematics.float3 aim_vector;

        public ufbx_constraint_aim_up_type aim_up_type;

        public ufbx_node* aim_up_node;

        [NativeTypeName("ufbx_vec3")]
        public Misaki.HighPerformance.Mathematics.float3 aim_up_vector;

        public ufbx_node* ik_effector;

        public ufbx_node* ik_end_node;

        [NativeTypeName("ufbx_vec3")]
        public Misaki.HighPerformance.Mathematics.float3 ik_pole_vector;

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
            [NativeTypeName("__AnonymousRecord_ufbx_L3349_C32")]
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

        [InlineArray(3)]
        public partial struct _constrain_translation_e__FixedBuffer
        {
            public bool e0;
        }

        [InlineArray(3)]
        public partial struct _constrain_rotation_e__FixedBuffer
        {
            public bool e0;
        }

        [InlineArray(3)]
        public partial struct _constrain_scale_e__FixedBuffer
        {
            public bool e0;
        }
    }
}
