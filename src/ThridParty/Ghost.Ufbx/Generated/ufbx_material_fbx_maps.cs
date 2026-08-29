using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    /// <include file='ufbx_material_fbx_maps.xml' path='doc/member[@name="ufbx_material_fbx_maps"]/*' />
    public partial struct ufbx_material_fbx_maps
    {
        /// <include file='ufbx_material_fbx_maps.xml' path='doc/member[@name="ufbx_material_fbx_maps.Anonymous"]/*' />
        [NativeTypeName("__AnonymousRecord_ufbx_L2514_C2")]
        public _Anonymous_e__Union Anonymous;

        /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union.maps"]/*' />
        [UnscopedRef]
        public Span<ufbx_material_map> maps
        {
            get
            {
                return Anonymous.maps;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.diffuse_factor"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map diffuse_factor
        {
            get
            {
                return ref Anonymous.Anonymous.diffuse_factor;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.diffuse_color"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map diffuse_color
        {
            get
            {
                return ref Anonymous.Anonymous.diffuse_color;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.specular_factor"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map specular_factor
        {
            get
            {
                return ref Anonymous.Anonymous.specular_factor;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.specular_color"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map specular_color
        {
            get
            {
                return ref Anonymous.Anonymous.specular_color;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.specular_exponent"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map specular_exponent
        {
            get
            {
                return ref Anonymous.Anonymous.specular_exponent;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.reflection_factor"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map reflection_factor
        {
            get
            {
                return ref Anonymous.Anonymous.reflection_factor;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.reflection_color"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map reflection_color
        {
            get
            {
                return ref Anonymous.Anonymous.reflection_color;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.transparency_factor"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map transparency_factor
        {
            get
            {
                return ref Anonymous.Anonymous.transparency_factor;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.transparency_color"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map transparency_color
        {
            get
            {
                return ref Anonymous.Anonymous.transparency_color;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.emission_factor"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map emission_factor
        {
            get
            {
                return ref Anonymous.Anonymous.emission_factor;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.emission_color"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map emission_color
        {
            get
            {
                return ref Anonymous.Anonymous.emission_color;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.ambient_factor"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map ambient_factor
        {
            get
            {
                return ref Anonymous.Anonymous.ambient_factor;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.ambient_color"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map ambient_color
        {
            get
            {
                return ref Anonymous.Anonymous.ambient_color;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.normal_map"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map normal_map
        {
            get
            {
                return ref Anonymous.Anonymous.normal_map;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.bump"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map bump
        {
            get
            {
                return ref Anonymous.Anonymous.bump;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.bump_factor"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map bump_factor
        {
            get
            {
                return ref Anonymous.Anonymous.bump_factor;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.displacement_factor"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map displacement_factor
        {
            get
            {
                return ref Anonymous.Anonymous.displacement_factor;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.displacement"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map displacement
        {
            get
            {
                return ref Anonymous.Anonymous.displacement;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.vector_displacement_factor"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map vector_displacement_factor
        {
            get
            {
                return ref Anonymous.Anonymous.vector_displacement_factor;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.vector_displacement"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map vector_displacement
        {
            get
            {
                return ref Anonymous.Anonymous.vector_displacement;
            }
        }

        /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union"]/*' />
        [StructLayout(LayoutKind.Explicit)]
        public partial struct _Anonymous_e__Union
        {
            /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union.maps"]/*' />
            [FieldOffset(0)]
            [NativeTypeName("ufbx_material_map[20]")]
            public _maps_e__FixedBuffer maps;

            /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union.Anonymous"]/*' />
            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_ufbx_L2516_C3")]
            public _Anonymous_e__Struct Anonymous;

            /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct"]/*' />
            public partial struct _Anonymous_e__Struct
            {
                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.diffuse_factor"]/*' />
                public ufbx_material_map diffuse_factor;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.diffuse_color"]/*' />
                public ufbx_material_map diffuse_color;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.specular_factor"]/*' />
                public ufbx_material_map specular_factor;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.specular_color"]/*' />
                public ufbx_material_map specular_color;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.specular_exponent"]/*' />
                public ufbx_material_map specular_exponent;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.reflection_factor"]/*' />
                public ufbx_material_map reflection_factor;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.reflection_color"]/*' />
                public ufbx_material_map reflection_color;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.transparency_factor"]/*' />
                public ufbx_material_map transparency_factor;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.transparency_color"]/*' />
                public ufbx_material_map transparency_color;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.emission_factor"]/*' />
                public ufbx_material_map emission_factor;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.emission_color"]/*' />
                public ufbx_material_map emission_color;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.ambient_factor"]/*' />
                public ufbx_material_map ambient_factor;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.ambient_color"]/*' />
                public ufbx_material_map ambient_color;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.normal_map"]/*' />
                public ufbx_material_map normal_map;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.bump"]/*' />
                public ufbx_material_map bump;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.bump_factor"]/*' />
                public ufbx_material_map bump_factor;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.displacement_factor"]/*' />
                public ufbx_material_map displacement_factor;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.displacement"]/*' />
                public ufbx_material_map displacement;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.vector_displacement_factor"]/*' />
                public ufbx_material_map vector_displacement_factor;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.vector_displacement"]/*' />
                public ufbx_material_map vector_displacement;
            }

            /// <include file='_maps_e__FixedBuffer.xml' path='doc/member[@name="_maps_e__FixedBuffer"]/*' />
            [InlineArray(20)]
            public partial struct _maps_e__FixedBuffer
            {
                public ufbx_material_map e0;
            }
        }
    }
}
