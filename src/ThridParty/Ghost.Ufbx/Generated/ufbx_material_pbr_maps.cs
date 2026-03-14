using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    public partial struct ufbx_material_pbr_maps
    {
        [NativeTypeName("__AnonymousRecord_ufbx_L2536_C2")]
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
        public ref ufbx_material_map base_factor
        {
            get
            {
                return ref Anonymous.Anonymous.base_factor;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map base_color
        {
            get
            {
                return ref Anonymous.Anonymous.base_color;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map roughness
        {
            get
            {
                return ref Anonymous.Anonymous.roughness;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map metalness
        {
            get
            {
                return ref Anonymous.Anonymous.metalness;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map diffuse_roughness
        {
            get
            {
                return ref Anonymous.Anonymous.diffuse_roughness;
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
        public ref ufbx_material_map specular_ior
        {
            get
            {
                return ref Anonymous.Anonymous.specular_ior;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map specular_anisotropy
        {
            get
            {
                return ref Anonymous.Anonymous.specular_anisotropy;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map specular_rotation
        {
            get
            {
                return ref Anonymous.Anonymous.specular_rotation;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map transmission_factor
        {
            get
            {
                return ref Anonymous.Anonymous.transmission_factor;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map transmission_color
        {
            get
            {
                return ref Anonymous.Anonymous.transmission_color;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map transmission_depth
        {
            get
            {
                return ref Anonymous.Anonymous.transmission_depth;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map transmission_scatter
        {
            get
            {
                return ref Anonymous.Anonymous.transmission_scatter;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map transmission_scatter_anisotropy
        {
            get
            {
                return ref Anonymous.Anonymous.transmission_scatter_anisotropy;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map transmission_dispersion
        {
            get
            {
                return ref Anonymous.Anonymous.transmission_dispersion;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map transmission_roughness
        {
            get
            {
                return ref Anonymous.Anonymous.transmission_roughness;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map transmission_extra_roughness
        {
            get
            {
                return ref Anonymous.Anonymous.transmission_extra_roughness;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map transmission_priority
        {
            get
            {
                return ref Anonymous.Anonymous.transmission_priority;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map transmission_enable_in_aov
        {
            get
            {
                return ref Anonymous.Anonymous.transmission_enable_in_aov;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map subsurface_factor
        {
            get
            {
                return ref Anonymous.Anonymous.subsurface_factor;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map subsurface_color
        {
            get
            {
                return ref Anonymous.Anonymous.subsurface_color;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map subsurface_radius
        {
            get
            {
                return ref Anonymous.Anonymous.subsurface_radius;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map subsurface_scale
        {
            get
            {
                return ref Anonymous.Anonymous.subsurface_scale;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map subsurface_anisotropy
        {
            get
            {
                return ref Anonymous.Anonymous.subsurface_anisotropy;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map subsurface_tint_color
        {
            get
            {
                return ref Anonymous.Anonymous.subsurface_tint_color;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map subsurface_type
        {
            get
            {
                return ref Anonymous.Anonymous.subsurface_type;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map sheen_factor
        {
            get
            {
                return ref Anonymous.Anonymous.sheen_factor;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map sheen_color
        {
            get
            {
                return ref Anonymous.Anonymous.sheen_color;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map sheen_roughness
        {
            get
            {
                return ref Anonymous.Anonymous.sheen_roughness;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map coat_factor
        {
            get
            {
                return ref Anonymous.Anonymous.coat_factor;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map coat_color
        {
            get
            {
                return ref Anonymous.Anonymous.coat_color;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map coat_roughness
        {
            get
            {
                return ref Anonymous.Anonymous.coat_roughness;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map coat_ior
        {
            get
            {
                return ref Anonymous.Anonymous.coat_ior;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map coat_anisotropy
        {
            get
            {
                return ref Anonymous.Anonymous.coat_anisotropy;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map coat_rotation
        {
            get
            {
                return ref Anonymous.Anonymous.coat_rotation;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map coat_normal
        {
            get
            {
                return ref Anonymous.Anonymous.coat_normal;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map coat_affect_base_color
        {
            get
            {
                return ref Anonymous.Anonymous.coat_affect_base_color;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map coat_affect_base_roughness
        {
            get
            {
                return ref Anonymous.Anonymous.coat_affect_base_roughness;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map thin_film_factor
        {
            get
            {
                return ref Anonymous.Anonymous.thin_film_factor;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map thin_film_thickness
        {
            get
            {
                return ref Anonymous.Anonymous.thin_film_thickness;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map thin_film_ior
        {
            get
            {
                return ref Anonymous.Anonymous.thin_film_ior;
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
        public ref ufbx_material_map opacity
        {
            get
            {
                return ref Anonymous.Anonymous.opacity;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map indirect_diffuse
        {
            get
            {
                return ref Anonymous.Anonymous.indirect_diffuse;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map indirect_specular
        {
            get
            {
                return ref Anonymous.Anonymous.indirect_specular;
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
        public ref ufbx_material_map tangent_map
        {
            get
            {
                return ref Anonymous.Anonymous.tangent_map;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map displacement_map
        {
            get
            {
                return ref Anonymous.Anonymous.displacement_map;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map matte_factor
        {
            get
            {
                return ref Anonymous.Anonymous.matte_factor;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map matte_color
        {
            get
            {
                return ref Anonymous.Anonymous.matte_color;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map ambient_occlusion
        {
            get
            {
                return ref Anonymous.Anonymous.ambient_occlusion;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map glossiness
        {
            get
            {
                return ref Anonymous.Anonymous.glossiness;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map coat_glossiness
        {
            get
            {
                return ref Anonymous.Anonymous.coat_glossiness;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_map transmission_glossiness
        {
            get
            {
                return ref Anonymous.Anonymous.transmission_glossiness;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("ufbx_material_map[56]")]
            public _maps_e__FixedBuffer maps;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_ufbx_L2538_C3")]
            public _Anonymous_e__Struct Anonymous;

            public partial struct _Anonymous_e__Struct
            {
                public ufbx_material_map base_factor;

                public ufbx_material_map base_color;

                public ufbx_material_map roughness;

                public ufbx_material_map metalness;

                public ufbx_material_map diffuse_roughness;

                public ufbx_material_map specular_factor;

                public ufbx_material_map specular_color;

                public ufbx_material_map specular_ior;

                public ufbx_material_map specular_anisotropy;

                public ufbx_material_map specular_rotation;

                public ufbx_material_map transmission_factor;

                public ufbx_material_map transmission_color;

                public ufbx_material_map transmission_depth;

                public ufbx_material_map transmission_scatter;

                public ufbx_material_map transmission_scatter_anisotropy;

                public ufbx_material_map transmission_dispersion;

                public ufbx_material_map transmission_roughness;

                public ufbx_material_map transmission_extra_roughness;

                public ufbx_material_map transmission_priority;

                public ufbx_material_map transmission_enable_in_aov;

                public ufbx_material_map subsurface_factor;

                public ufbx_material_map subsurface_color;

                public ufbx_material_map subsurface_radius;

                public ufbx_material_map subsurface_scale;

                public ufbx_material_map subsurface_anisotropy;

                public ufbx_material_map subsurface_tint_color;

                public ufbx_material_map subsurface_type;

                public ufbx_material_map sheen_factor;

                public ufbx_material_map sheen_color;

                public ufbx_material_map sheen_roughness;

                public ufbx_material_map coat_factor;

                public ufbx_material_map coat_color;

                public ufbx_material_map coat_roughness;

                public ufbx_material_map coat_ior;

                public ufbx_material_map coat_anisotropy;

                public ufbx_material_map coat_rotation;

                public ufbx_material_map coat_normal;

                public ufbx_material_map coat_affect_base_color;

                public ufbx_material_map coat_affect_base_roughness;

                public ufbx_material_map thin_film_factor;

                public ufbx_material_map thin_film_thickness;

                public ufbx_material_map thin_film_ior;

                public ufbx_material_map emission_factor;

                public ufbx_material_map emission_color;

                public ufbx_material_map opacity;

                public ufbx_material_map indirect_diffuse;

                public ufbx_material_map indirect_specular;

                public ufbx_material_map normal_map;

                public ufbx_material_map tangent_map;

                public ufbx_material_map displacement_map;

                public ufbx_material_map matte_factor;

                public ufbx_material_map matte_color;

                public ufbx_material_map ambient_occlusion;

                public ufbx_material_map glossiness;

                public ufbx_material_map coat_glossiness;

                public ufbx_material_map transmission_glossiness;
            }

            [InlineArray(56)]
            public partial struct _maps_e__FixedBuffer
            {
                public ufbx_material_map e0;
            }
        }
    }
}
