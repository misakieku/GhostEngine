using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    /// <include file='ufbx_material_features.xml' path='doc/member[@name="ufbx_material_features"]/*' />
    public partial struct ufbx_material_features
    {
        /// <include file='ufbx_material_features.xml' path='doc/member[@name="ufbx_material_features.Anonymous"]/*' />
        [NativeTypeName("__AnonymousRecord_ufbx_L2606_C2")]
        public _Anonymous_e__Union Anonymous;

        /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union.features"]/*' />
        [UnscopedRef]
        public Span<ufbx_material_feature_info> features
        {
            get
            {
                return Anonymous.features;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.pbr"]/*' />
        [UnscopedRef]
        public ref ufbx_material_feature_info pbr
        {
            get
            {
                return ref Anonymous.Anonymous.pbr;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.metalness"]/*' />
        [UnscopedRef]
        public ref ufbx_material_feature_info metalness
        {
            get
            {
                return ref Anonymous.Anonymous.metalness;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.diffuse"]/*' />
        [UnscopedRef]
        public ref ufbx_material_feature_info diffuse
        {
            get
            {
                return ref Anonymous.Anonymous.diffuse;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.specular"]/*' />
        [UnscopedRef]
        public ref ufbx_material_feature_info specular
        {
            get
            {
                return ref Anonymous.Anonymous.specular;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.emission"]/*' />
        [UnscopedRef]
        public ref ufbx_material_feature_info emission
        {
            get
            {
                return ref Anonymous.Anonymous.emission;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.transmission"]/*' />
        [UnscopedRef]
        public ref ufbx_material_feature_info transmission
        {
            get
            {
                return ref Anonymous.Anonymous.transmission;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.coat"]/*' />
        [UnscopedRef]
        public ref ufbx_material_feature_info coat
        {
            get
            {
                return ref Anonymous.Anonymous.coat;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.sheen"]/*' />
        [UnscopedRef]
        public ref ufbx_material_feature_info sheen
        {
            get
            {
                return ref Anonymous.Anonymous.sheen;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.opacity"]/*' />
        [UnscopedRef]
        public ref ufbx_material_feature_info opacity
        {
            get
            {
                return ref Anonymous.Anonymous.opacity;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.ambient_occlusion"]/*' />
        [UnscopedRef]
        public ref ufbx_material_feature_info ambient_occlusion
        {
            get
            {
                return ref Anonymous.Anonymous.ambient_occlusion;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.matte"]/*' />
        [UnscopedRef]
        public ref ufbx_material_feature_info matte
        {
            get
            {
                return ref Anonymous.Anonymous.matte;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.unlit"]/*' />
        [UnscopedRef]
        public ref ufbx_material_feature_info unlit
        {
            get
            {
                return ref Anonymous.Anonymous.unlit;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.ior"]/*' />
        [UnscopedRef]
        public ref ufbx_material_feature_info ior
        {
            get
            {
                return ref Anonymous.Anonymous.ior;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.diffuse_roughness"]/*' />
        [UnscopedRef]
        public ref ufbx_material_feature_info diffuse_roughness
        {
            get
            {
                return ref Anonymous.Anonymous.diffuse_roughness;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.transmission_roughness"]/*' />
        [UnscopedRef]
        public ref ufbx_material_feature_info transmission_roughness
        {
            get
            {
                return ref Anonymous.Anonymous.transmission_roughness;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.thin_walled"]/*' />
        [UnscopedRef]
        public ref ufbx_material_feature_info thin_walled
        {
            get
            {
                return ref Anonymous.Anonymous.thin_walled;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.caustics"]/*' />
        [UnscopedRef]
        public ref ufbx_material_feature_info caustics
        {
            get
            {
                return ref Anonymous.Anonymous.caustics;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.exit_to_background"]/*' />
        [UnscopedRef]
        public ref ufbx_material_feature_info exit_to_background
        {
            get
            {
                return ref Anonymous.Anonymous.exit_to_background;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.internal_reflections"]/*' />
        [UnscopedRef]
        public ref ufbx_material_feature_info internal_reflections
        {
            get
            {
                return ref Anonymous.Anonymous.internal_reflections;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.double_sided"]/*' />
        [UnscopedRef]
        public ref ufbx_material_feature_info double_sided
        {
            get
            {
                return ref Anonymous.Anonymous.double_sided;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.roughness_as_glossiness"]/*' />
        [UnscopedRef]
        public ref ufbx_material_feature_info roughness_as_glossiness
        {
            get
            {
                return ref Anonymous.Anonymous.roughness_as_glossiness;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.coat_roughness_as_glossiness"]/*' />
        [UnscopedRef]
        public ref ufbx_material_feature_info coat_roughness_as_glossiness
        {
            get
            {
                return ref Anonymous.Anonymous.coat_roughness_as_glossiness;
            }
        }

        /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.transmission_roughness_as_glossiness"]/*' />
        [UnscopedRef]
        public ref ufbx_material_feature_info transmission_roughness_as_glossiness
        {
            get
            {
                return ref Anonymous.Anonymous.transmission_roughness_as_glossiness;
            }
        }

        /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union"]/*' />
        [StructLayout(LayoutKind.Explicit)]
        public partial struct _Anonymous_e__Union
        {
            /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union.features"]/*' />
            [FieldOffset(0)]
            [NativeTypeName("ufbx_material_feature_info[23]")]
            public _features_e__FixedBuffer features;

            /// <include file='_Anonymous_e__Union.xml' path='doc/member[@name="_Anonymous_e__Union.Anonymous"]/*' />
            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_ufbx_L2608_C3")]
            public _Anonymous_e__Struct Anonymous;

            /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct"]/*' />
            public partial struct _Anonymous_e__Struct
            {
                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.pbr"]/*' />
                public ufbx_material_feature_info pbr;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.metalness"]/*' />
                public ufbx_material_feature_info metalness;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.diffuse"]/*' />
                public ufbx_material_feature_info diffuse;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.specular"]/*' />
                public ufbx_material_feature_info specular;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.emission"]/*' />
                public ufbx_material_feature_info emission;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.transmission"]/*' />
                public ufbx_material_feature_info transmission;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.coat"]/*' />
                public ufbx_material_feature_info coat;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.sheen"]/*' />
                public ufbx_material_feature_info sheen;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.opacity"]/*' />
                public ufbx_material_feature_info opacity;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.ambient_occlusion"]/*' />
                public ufbx_material_feature_info ambient_occlusion;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.matte"]/*' />
                public ufbx_material_feature_info matte;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.unlit"]/*' />
                public ufbx_material_feature_info unlit;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.ior"]/*' />
                public ufbx_material_feature_info ior;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.diffuse_roughness"]/*' />
                public ufbx_material_feature_info diffuse_roughness;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.transmission_roughness"]/*' />
                public ufbx_material_feature_info transmission_roughness;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.thin_walled"]/*' />
                public ufbx_material_feature_info thin_walled;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.caustics"]/*' />
                public ufbx_material_feature_info caustics;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.exit_to_background"]/*' />
                public ufbx_material_feature_info exit_to_background;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.internal_reflections"]/*' />
                public ufbx_material_feature_info internal_reflections;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.double_sided"]/*' />
                public ufbx_material_feature_info double_sided;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.roughness_as_glossiness"]/*' />
                public ufbx_material_feature_info roughness_as_glossiness;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.coat_roughness_as_glossiness"]/*' />
                public ufbx_material_feature_info coat_roughness_as_glossiness;

                /// <include file='_Anonymous_e__Struct.xml' path='doc/member[@name="_Anonymous_e__Struct.transmission_roughness_as_glossiness"]/*' />
                public ufbx_material_feature_info transmission_roughness_as_glossiness;
            }

            /// <include file='_features_e__FixedBuffer.xml' path='doc/member[@name="_features_e__FixedBuffer"]/*' />
            [InlineArray(23)]
            public partial struct _features_e__FixedBuffer
            {
                public ufbx_material_feature_info e0;
            }
        }
    }
}
