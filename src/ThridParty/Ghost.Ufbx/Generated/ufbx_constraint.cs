using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    /// <include file='ufbx_constraint.xml' path='doc/member[@name="ufbx_constraint"]/*' />
    public unsafe partial struct ufbx_constraint
    {
        /// <include file='ufbx_constraint.xml' path='doc/member[@name="ufbx_constraint.Anonymous"]/*' />
        [NativeTypeName("__AnonymousRecord_ufbx_L3355_C2")]
        public _Anonymous_e__Union Anonymous;

        /// <include file='ufbx_constraint.xml' path='doc/member[@name="ufbx_constraint.type"]/*' />
        public ufbx_constraint_type type;

        /// <include file='ufbx_constraint.xml' path='doc/member[@name="ufbx_constraint.type_name"]/*' />
        public ufbx_string type_name;

        /// <include file='ufbx_constraint.xml' path='doc/member[@name="ufbx_constraint.node"]/*' />
        public ufbx_node* node;

        /// <include file='ufbx_constraint.xml' path='doc/member[@name="ufbx_constraint.targets"]/*' />
        public ufbx_constraint_target_list targets;

        /// <include file='ufbx_constraint.xml' path='doc/member[@name="ufbx_constraint.weight"]/*' />
        [NativeTypeName("ufbx_real")]
        public float weight;

        /// <include file='ufbx_constraint.xml' path='doc/member[@name="ufbx_constraint.active"]/*' />
        [NativeTypeName("_Bool")]
        public bool active;

        /// <include file='ufbx_constraint.xml' path='doc/member[@name="ufbx_constraint.constrain_translation"]/*' />
        [NativeTypeName("_Bool[3]")]
        public _constrain_translation_e__FixedBuffer constrain_translation;

        /// <include file='ufbx_constraint.xml' path='doc/member[@name="ufbx_constraint.constrain_rotation"]/*' />
        [NativeTypeName("_Bool[3]")]
        public _constrain_rotation_e__FixedBuffer constrain_rotation;

        /// <include file='ufbx_constraint.xml' path='doc/member[@name="ufbx_constraint.constrain_scale"]/*' />
        [NativeTypeName("_Bool[3]")]
        public _constrain_scale_e__FixedBuffer constrain_scale;

        /// <include file='ufbx_constraint.xml' path='doc/member[@name="ufbx_constraint.transform_offset"]/*' />
        public ufbx_transform transform_offset;

        /// <include file='ufbx_constraint.xml' path='doc/member[@name="ufbx_constraint.aim_vector"]/*' />
        public ufbx_vec3 aim_vector;

        /// <include file='ufbx_constraint.xml' path='doc/member[@name="ufbx_constraint.aim_up_type"]/*' />
        public ufbx_constraint_aim_up_type aim_up_type;

        /// <include file='ufbx_constraint.xml' path='doc/member[@name="ufbx_constraint.aim_up_node"]/*' />
        public ufbx_node* aim_up_node;

        /// <include file='ufbx_constraint.xml' path='doc/member[@name="ufbx_constraint.aim_up_vector"]/*' />
        public ufbx_vec3 aim_up_vector;

        /// <include file='ufbx_constraint.xml' path='doc/member[@name="ufbx_constraint.ik_effector"]/*' />
        public ufbx_node* ik_effector;

        /// <include file='ufbx_constraint.xml' path='doc/member[@name="ufbx_constraint.ik_end_node"]/*' />
        public ufbx_node* ik_end_node;

        /// <include file='ufbx_constraint.xml' path='doc/member[@name="ufbx_constraint.ik_pole_vector"]/*' />
        public ufbx_vec3 ik_pole_vector;

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
            [NativeTypeName("__AnonymousRecord_ufbx_L3355_C32")]
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

        /// <include file='_constrain_translation_e__FixedBuffer.xml' path='doc/member[@name="_constrain_translation_e__FixedBuffer"]/*' />
        [InlineArray(3)]
        public partial struct _constrain_translation_e__FixedBuffer
        {
            public bool e0;
        }

        /// <include file='_constrain_rotation_e__FixedBuffer.xml' path='doc/member[@name="_constrain_rotation_e__FixedBuffer"]/*' />
        [InlineArray(3)]
        public partial struct _constrain_rotation_e__FixedBuffer
        {
            public bool e0;
        }

        /// <include file='_constrain_scale_e__FixedBuffer.xml' path='doc/member[@name="_constrain_scale_e__FixedBuffer"]/*' />
        [InlineArray(3)]
        public partial struct _constrain_scale_e__FixedBuffer
        {
            public bool e0;
        }
    }
}
