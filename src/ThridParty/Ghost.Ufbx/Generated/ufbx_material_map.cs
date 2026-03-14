using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_material_map
    {
        [NativeTypeName("__AnonymousRecord_ufbx_L2287_C2")]
        public _Anonymous_e__Union Anonymous;

        [NativeTypeName("int64_t")]
        public long value_int;

        public ufbx_texture* texture;

        [NativeTypeName("_Bool")]
        public bool has_value;

        [NativeTypeName("_Bool")]
        public bool texture_enabled;

        [NativeTypeName("_Bool")]
        public bool feature_disabled;

        [NativeTypeName("uint8_t")]
        public byte value_components;

        [UnscopedRef]
        public ref float value_real
        {
            get
            {
                return ref Anonymous.value_real;
            }
        }

        [UnscopedRef]
        public ref Misaki.HighPerformance.Mathematics.float2 value_vec2
        {
            get
            {
                return ref Anonymous.value_vec2;
            }
        }

        [UnscopedRef]
        public ref Misaki.HighPerformance.Mathematics.float3 value_vec3
        {
            get
            {
                return ref Anonymous.value_vec3;
            }
        }

        [UnscopedRef]
        public ref Misaki.HighPerformance.Mathematics.float4 value_vec4
        {
            get
            {
                return ref Anonymous.value_vec4;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("ufbx_real")]
            public float value_real;

            [FieldOffset(0)]
            [NativeTypeName("ufbx_vec2")]
            public Misaki.HighPerformance.Mathematics.float2 value_vec2;

            [FieldOffset(0)]
            [NativeTypeName("ufbx_vec3")]
            public Misaki.HighPerformance.Mathematics.float3 value_vec3;

            [FieldOffset(0)]
            [NativeTypeName("ufbx_vec4")]
            public Misaki.HighPerformance.Mathematics.float4 value_vec4;
        }
    }
}
