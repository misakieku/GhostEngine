using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    /// <include file='ufbx_material_pbr_maps.xml' path='doc/member[@name="ufbx_material_pbr_maps"]/*' />
    public partial struct ufbx_material_pbr_maps
    {
        /// <include file='ufbx_material_pbr_maps.xml' path='doc/member[@name="ufbx_material_pbr_maps.Anonymous"]/*' />
        [NativeTypeName("__AnonymousRecord_ufbx_L2542_C2")]
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

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.base_factor"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map base_factor
        {
            get
            {
                return ref Anonymous.Anonymous.base_factor;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.base_color"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map base_color
        {
            get
            {
                return ref Anonymous.Anonymous.base_color;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.roughness"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map roughness
        {
            get
            {
                return ref Anonymous.Anonymous.roughness;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.metalness"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map metalness
        {
            get
            {
                return ref Anonymous.Anonymous.metalness;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.diffuse_roughness"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map diffuse_roughness
        {
            get
            {
                return ref Anonymous.Anonymous.diffuse_roughness;
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

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.specular_ior"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map specular_ior
        {
            get
            {
                return ref Anonymous.Anonymous.specular_ior;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.specular_anisotropy"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map specular_anisotropy
        {
            get
            {
                return ref Anonymous.Anonymous.specular_anisotropy;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.specular_rotation"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map specular_rotation
        {
            get
            {
                return ref Anonymous.Anonymous.specular_rotation;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.transmission_factor"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map transmission_factor
        {
            get
            {
                return ref Anonymous.Anonymous.transmission_factor;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.transmission_color"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map transmission_color
        {
            get
            {
                return ref Anonymous.Anonymous.transmission_color;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.transmission_depth"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map transmission_depth
        {
            get
            {
                return ref Anonymous.Anonymous.transmission_depth;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.transmission_scatter"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map transmission_scatter
        {
            get
            {
                return ref Anonymous.Anonymous.transmission_scatter;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.transmission_scatter_anisotropy"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map transmission_scatter_anisotropy
        {
            get
            {
                return ref Anonymous.Anonymous.transmission_scatter_anisotropy;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.transmission_dispersion"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map transmission_dispersion
        {
            get
            {
                return ref Anonymous.Anonymous.transmission_dispersion;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.transmission_roughness"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map transmission_roughness
        {
            get
            {
                return ref Anonymous.Anonymous.transmission_roughness;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.transmission_extra_roughness"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map transmission_extra_roughness
        {
            get
            {
                return ref Anonymous.Anonymous.transmission_extra_roughness;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.transmission_priority"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map transmission_priority
        {
            get
            {
                return ref Anonymous.Anonymous.transmission_priority;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.transmission_enable_in_aov"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map transmission_enable_in_aov
        {
            get
            {
                return ref Anonymous.Anonymous.transmission_enable_in_aov;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.subsurface_factor"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map subsurface_factor
        {
            get
            {
                return ref Anonymous.Anonymous.subsurface_factor;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.subsurface_color"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map subsurface_color
        {
            get
            {
                return ref Anonymous.Anonymous.subsurface_color;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.subsurface_radius"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map subsurface_radius
        {
            get
            {
                return ref Anonymous.Anonymous.subsurface_radius;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.subsurface_scale"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map subsurface_scale
        {
            get
            {
                return ref Anonymous.Anonymous.subsurface_scale;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.subsurface_anisotropy"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map subsurface_anisotropy
        {
            get
            {
                return ref Anonymous.Anonymous.subsurface_anisotropy;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.subsurface_tint_color"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map subsurface_tint_color
        {
            get
            {
                return ref Anonymous.Anonymous.subsurface_tint_color;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.subsurface_type"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map subsurface_type
        {
            get
            {
                return ref Anonymous.Anonymous.subsurface_type;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.sheen_factor"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map sheen_factor
        {
            get
            {
                return ref Anonymous.Anonymous.sheen_factor;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.sheen_color"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map sheen_color
        {
            get
            {
                return ref Anonymous.Anonymous.sheen_color;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.sheen_roughness"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map sheen_roughness
        {
            get
            {
                return ref Anonymous.Anonymous.sheen_roughness;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.coat_factor"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map coat_factor
        {
            get
            {
                return ref Anonymous.Anonymous.coat_factor;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.coat_color"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map coat_color
        {
            get
            {
                return ref Anonymous.Anonymous.coat_color;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.coat_roughness"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map coat_roughness
        {
            get
            {
                return ref Anonymous.Anonymous.coat_roughness;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.coat_ior"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map coat_ior
        {
            get
            {
                return ref Anonymous.Anonymous.coat_ior;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.coat_anisotropy"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map coat_anisotropy
        {
            get
            {
                return ref Anonymous.Anonymous.coat_anisotropy;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.coat_rotation"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map coat_rotation
        {
            get
            {
                return ref Anonymous.Anonymous.coat_rotation;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.coat_normal"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map coat_normal
        {
            get
            {
                return ref Anonymous.Anonymous.coat_normal;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.coat_affect_base_color"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map coat_affect_base_color
        {
            get
            {
                return ref Anonymous.Anonymous.coat_affect_base_color;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.coat_affect_base_roughness"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map coat_affect_base_roughness
        {
            get
            {
                return ref Anonymous.Anonymous.coat_affect_base_roughness;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.thin_film_factor"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map thin_film_factor
        {
            get
            {
                return ref Anonymous.Anonymous.thin_film_factor;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.thin_film_thickness"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map thin_film_thickness
        {
            get
            {
                return ref Anonymous.Anonymous.thin_film_thickness;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.thin_film_ior"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map thin_film_ior
        {
            get
            {
                return ref Anonymous.Anonymous.thin_film_ior;
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

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.opacity"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map opacity
        {
            get
            {
                return ref Anonymous.Anonymous.opacity;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.indirect_diffuse"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map indirect_diffuse
        {
            get
            {
                return ref Anonymous.Anonymous.indirect_diffuse;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.indirect_specular"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map indirect_specular
        {
            get
            {
                return ref Anonymous.Anonymous.indirect_specular;
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

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.tangent_map"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map tangent_map
        {
            get
            {
                return ref Anonymous.Anonymous.tangent_map;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.displacement_map"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map displacement_map
        {
            get
            {
                return ref Anonymous.Anonymous.displacement_map;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.matte_factor"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map matte_factor
        {
            get
            {
                return ref Anonymous.Anonymous.matte_factor;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.matte_color"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map matte_color
        {
            get
            {
                return ref Anonymous.Anonymous.matte_color;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.ambient_occlusion"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map ambient_occlusion
        {
            get
            {
                return ref Anonymous.Anonymous.ambient_occlusion;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.glossiness"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map glossiness
        {
            get
            {
                return ref Anonymous.Anonymous.glossiness;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.coat_glossiness"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map coat_glossiness
        {
            get
            {
                return ref Anonymous.Anonymous.coat_glossiness;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.transmission_glossiness"]/*' />
        [UnscopedRef]
        public ref ufbx_material_map transmission_glossiness
        {
            get
            {
                return ref Anonymous.Anonymous.transmission_glossiness;
            }
        }

        /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union"]/*' />
        [StructLayout(LayoutKind.Explicit)]
        public partial struct _Anonymous_e__Union
        {
            /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union.maps"]/*' />
            [FieldOffset(0)]
            [NativeTypeName("ufbx_material_map[56]")]
            public _maps_e__FixedBuffer maps;

            /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union.Anonymous"]/*' />
            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_ufbx_L2544_C3")]
            public _Anonymous_e__Struct Anonymous;

            /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct"]/*' />
            public partial struct _Anonymous_e__Struct
            {
                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.base_factor"]/*' />
                public ufbx_material_map base_factor;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.base_color"]/*' />
                public ufbx_material_map base_color;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.roughness"]/*' />
                public ufbx_material_map roughness;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.metalness"]/*' />
                public ufbx_material_map metalness;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.diffuse_roughness"]/*' />
                public ufbx_material_map diffuse_roughness;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.specular_factor"]/*' />
                public ufbx_material_map specular_factor;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.specular_color"]/*' />
                public ufbx_material_map specular_color;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.specular_ior"]/*' />
                public ufbx_material_map specular_ior;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.specular_anisotropy"]/*' />
                public ufbx_material_map specular_anisotropy;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.specular_rotation"]/*' />
                public ufbx_material_map specular_rotation;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.transmission_factor"]/*' />
                public ufbx_material_map transmission_factor;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.transmission_color"]/*' />
                public ufbx_material_map transmission_color;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.transmission_depth"]/*' />
                public ufbx_material_map transmission_depth;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.transmission_scatter"]/*' />
                public ufbx_material_map transmission_scatter;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.transmission_scatter_anisotropy"]/*' />
                public ufbx_material_map transmission_scatter_anisotropy;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.transmission_dispersion"]/*' />
                public ufbx_material_map transmission_dispersion;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.transmission_roughness"]/*' />
                public ufbx_material_map transmission_roughness;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.transmission_extra_roughness"]/*' />
                public ufbx_material_map transmission_extra_roughness;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.transmission_priority"]/*' />
                public ufbx_material_map transmission_priority;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.transmission_enable_in_aov"]/*' />
                public ufbx_material_map transmission_enable_in_aov;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.subsurface_factor"]/*' />
                public ufbx_material_map subsurface_factor;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.subsurface_color"]/*' />
                public ufbx_material_map subsurface_color;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.subsurface_radius"]/*' />
                public ufbx_material_map subsurface_radius;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.subsurface_scale"]/*' />
                public ufbx_material_map subsurface_scale;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.subsurface_anisotropy"]/*' />
                public ufbx_material_map subsurface_anisotropy;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.subsurface_tint_color"]/*' />
                public ufbx_material_map subsurface_tint_color;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.subsurface_type"]/*' />
                public ufbx_material_map subsurface_type;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.sheen_factor"]/*' />
                public ufbx_material_map sheen_factor;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.sheen_color"]/*' />
                public ufbx_material_map sheen_color;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.sheen_roughness"]/*' />
                public ufbx_material_map sheen_roughness;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.coat_factor"]/*' />
                public ufbx_material_map coat_factor;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.coat_color"]/*' />
                public ufbx_material_map coat_color;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.coat_roughness"]/*' />
                public ufbx_material_map coat_roughness;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.coat_ior"]/*' />
                public ufbx_material_map coat_ior;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.coat_anisotropy"]/*' />
                public ufbx_material_map coat_anisotropy;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.coat_rotation"]/*' />
                public ufbx_material_map coat_rotation;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.coat_normal"]/*' />
                public ufbx_material_map coat_normal;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.coat_affect_base_color"]/*' />
                public ufbx_material_map coat_affect_base_color;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.coat_affect_base_roughness"]/*' />
                public ufbx_material_map coat_affect_base_roughness;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.thin_film_factor"]/*' />
                public ufbx_material_map thin_film_factor;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.thin_film_thickness"]/*' />
                public ufbx_material_map thin_film_thickness;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.thin_film_ior"]/*' />
                public ufbx_material_map thin_film_ior;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.emission_factor"]/*' />
                public ufbx_material_map emission_factor;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.emission_color"]/*' />
                public ufbx_material_map emission_color;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.opacity"]/*' />
                public ufbx_material_map opacity;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.indirect_diffuse"]/*' />
                public ufbx_material_map indirect_diffuse;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.indirect_specular"]/*' />
                public ufbx_material_map indirect_specular;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.normal_map"]/*' />
                public ufbx_material_map normal_map;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.tangent_map"]/*' />
                public ufbx_material_map tangent_map;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.displacement_map"]/*' />
                public ufbx_material_map displacement_map;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.matte_factor"]/*' />
                public ufbx_material_map matte_factor;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.matte_color"]/*' />
                public ufbx_material_map matte_color;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.ambient_occlusion"]/*' />
                public ufbx_material_map ambient_occlusion;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.glossiness"]/*' />
                public ufbx_material_map glossiness;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.coat_glossiness"]/*' />
                public ufbx_material_map coat_glossiness;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.transmission_glossiness"]/*' />
                public ufbx_material_map transmission_glossiness;
            }

            /// <include file='_maps_e__FixedBuffer.xml' path='doc/member[@name="_maps_e__FixedBuffer"]/*' />
            [InlineArray(56)]
            public partial struct _maps_e__FixedBuffer
            {
                public ufbx_material_map e0;
            }
        }
    }
}
