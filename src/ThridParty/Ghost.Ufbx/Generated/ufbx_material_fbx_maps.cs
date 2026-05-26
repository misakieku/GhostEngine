using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    public partial struct ufbx_material_fbx_maps
    {
        [NativeTypeName("__AnonymousRecord_ufbx_L2508_C2")]
        public _Anonymous_e__Union Anonymous;

        [UnscopedRef]
        public Span<ufbx_material_map> maps
        {
            get
            {
                return Anonymous.maps;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map diffuse_factor
        {
            get
            {
                return ref Anonymous.Anonymous.diffuse_factor;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map diffuse_color
        {
            get
            {
                return ref Anonymous.Anonymous.diffuse_color;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map specular_factor
        {
            get
            {
                return ref Anonymous.Anonymous.specular_factor;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map specular_color
        {
            get
            {
                return ref Anonymous.Anonymous.specular_color;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map specular_exponent
        {
            get
            {
                return ref Anonymous.Anonymous.specular_exponent;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map reflection_factor
        {
            get
            {
                return ref Anonymous.Anonymous.reflection_factor;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map reflection_color
        {
            get
            {
                return ref Anonymous.Anonymous.reflection_color;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map transparency_factor
        {
            get
            {
                return ref Anonymous.Anonymous.transparency_factor;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map transparency_color
        {
            get
            {
                return ref Anonymous.Anonymous.transparency_color;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map emission_factor
        {
            get
            {
                return ref Anonymous.Anonymous.emission_factor;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map emission_color
        {
            get
            {
                return ref Anonymous.Anonymous.emission_color;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map ambient_factor
        {
            get
            {
                return ref Anonymous.Anonymous.ambient_factor;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map ambient_color
        {
            get
            {
                return ref Anonymous.Anonymous.ambient_color;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map normal_map
        {
            get
            {
                return ref Anonymous.Anonymous.normal_map;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map bump
        {
            get
            {
                return ref Anonymous.Anonymous.bump;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map bump_factor
        {
            get
            {
                return ref Anonymous.Anonymous.bump_factor;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map displacement_factor
        {
            get
            {
                return ref Anonymous.Anonymous.displacement_factor;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map displacement
        {
            get
            {
                return ref Anonymous.Anonymous.displacement;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map vector_displacement_factor
        {
            get
            {
                return ref Anonymous.Anonymous.vector_displacement_factor;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map vector_displacement
        {
            get
            {
                return ref Anonymous.Anonymous.vector_displacement;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("ufbx_material_map[20]")]
            public _maps_e__FixedBuffer maps;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_ufbx_L2510_C3")]
            public _Anonymous_e__Struct Anonymous;

            public partial struct _Anonymous_e__Struct
            {
                public ufbx_material_map diffuse_factor;

                public ufbx_material_map diffuse_color;

                public ufbx_material_map specular_factor;

                public ufbx_material_map specular_color;

                public ufbx_material_map specular_exponent;

                public ufbx_material_map reflection_factor;

                public ufbx_material_map reflection_color;

                public ufbx_material_map transparency_factor;

                public ufbx_material_map transparency_color;

                public ufbx_material_map emission_factor;

                public ufbx_material_map emission_color;

                public ufbx_material_map ambient_factor;

                public ufbx_material_map ambient_color;

                public ufbx_material_map normal_map;

                public ufbx_material_map bump;

                public ufbx_material_map bump_factor;

                public ufbx_material_map displacement_factor;

                public ufbx_material_map displacement;

                public ufbx_material_map vector_displacement_factor;

                public ufbx_material_map vector_displacement;
            }

            [InlineArray(20)]
            public partial struct _maps_e__FixedBuffer
            {
                public ufbx_material_map e0;
            }
        }
    }
}
