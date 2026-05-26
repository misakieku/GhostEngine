using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    public partial struct ufbx_material_features
    {
        [NativeTypeName("__AnonymousRecord_ufbx_L2600_C2")]
        public _Anonymous_e__Union Anonymous;

        [UnscopedRef]
        public Span<ufbx_material_feature_info> features
        {
            get
            {
                return Anonymous.features;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_feature_info pbr
        {
            get
            {
                return ref Anonymous.Anonymous.pbr;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_feature_info metalness
        {
            get
            {
                return ref Anonymous.Anonymous.metalness;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_feature_info diffuse
        {
            get
            {
                return ref Anonymous.Anonymous.diffuse;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_feature_info specular
        {
            get
            {
                return ref Anonymous.Anonymous.specular;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_feature_info emission
        {
            get
            {
                return ref Anonymous.Anonymous.emission;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_feature_info transmission
        {
            get
            {
                return ref Anonymous.Anonymous.transmission;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_feature_info coat
        {
            get
            {
                return ref Anonymous.Anonymous.coat;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_feature_info sheen
        {
            get
            {
                return ref Anonymous.Anonymous.sheen;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_feature_info opacity
        {
            get
            {
                return ref Anonymous.Anonymous.opacity;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_feature_info ambient_occlusion
        {
            get
            {
                return ref Anonymous.Anonymous.ambient_occlusion;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_feature_info matte
        {
            get
            {
                return ref Anonymous.Anonymous.matte;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_feature_info unlit
        {
            get
            {
                return ref Anonymous.Anonymous.unlit;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_feature_info ior
        {
            get
            {
                return ref Anonymous.Anonymous.ior;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_feature_info diffuse_roughness
        {
            get
            {
                return ref Anonymous.Anonymous.diffuse_roughness;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_feature_info transmission_roughness
        {
            get
            {
                return ref Anonymous.Anonymous.transmission_roughness;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_feature_info thin_walled
        {
            get
            {
                return ref Anonymous.Anonymous.thin_walled;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_feature_info caustics
        {
            get
            {
                return ref Anonymous.Anonymous.caustics;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_feature_info exit_to_background
        {
            get
            {
                return ref Anonymous.Anonymous.exit_to_background;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_feature_info internal_reflections
        {
            get
            {
                return ref Anonymous.Anonymous.internal_reflections;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_feature_info double_sided
        {
            get
            {
                return ref Anonymous.Anonymous.double_sided;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_feature_info roughness_as_glossiness
        {
            get
            {
                return ref Anonymous.Anonymous.roughness_as_glossiness;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_feature_info coat_roughness_as_glossiness
        {
            get
            {
                return ref Anonymous.Anonymous.coat_roughness_as_glossiness;
            }
        }

        [UnscopedRef]
        public ref ufbx_material_feature_info transmission_roughness_as_glossiness
        {
            get
            {
                return ref Anonymous.Anonymous.transmission_roughness_as_glossiness;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("ufbx_material_feature_info[23]")]
            public _features_e__FixedBuffer features;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_ufbx_L2602_C3")]
            public _Anonymous_e__Struct Anonymous;

            public partial struct _Anonymous_e__Struct
            {
                public ufbx_material_feature_info pbr;

                public ufbx_material_feature_info metalness;

                public ufbx_material_feature_info diffuse;

                public ufbx_material_feature_info specular;

                public ufbx_material_feature_info emission;

                public ufbx_material_feature_info transmission;

                public ufbx_material_feature_info coat;

                public ufbx_material_feature_info sheen;

                public ufbx_material_feature_info opacity;

                public ufbx_material_feature_info ambient_occlusion;

                public ufbx_material_feature_info matte;

                public ufbx_material_feature_info unlit;

                public ufbx_material_feature_info ior;

                public ufbx_material_feature_info diffuse_roughness;

                public ufbx_material_feature_info transmission_roughness;

                public ufbx_material_feature_info thin_walled;

                public ufbx_material_feature_info caustics;

                public ufbx_material_feature_info exit_to_background;

                public ufbx_material_feature_info internal_reflections;

                public ufbx_material_feature_info double_sided;

                public ufbx_material_feature_info roughness_as_glossiness;

                public ufbx_material_feature_info coat_roughness_as_glossiness;

                public ufbx_material_feature_info transmission_roughness_as_glossiness;
            }

            [InlineArray(23)]
            public partial struct _features_e__FixedBuffer
            {
                public ufbx_material_feature_info e0;
            }
        }
    }
}
