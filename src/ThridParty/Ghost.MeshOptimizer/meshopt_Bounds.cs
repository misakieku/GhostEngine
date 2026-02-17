using Ghost.Zeux.MeshOptimizer;
using System.Runtime.CompilerServices;

namespace Ghost.MeshOptimizer
{
    public partial struct meshopt_Bounds
    {
        [NativeTypeName("float[3]")]
        public _center_e__FixedBuffer center;

        public float radius;

        [NativeTypeName("float[3]")]
        public _cone_apex_e__FixedBuffer cone_apex;

        [NativeTypeName("float[3]")]
        public _cone_axis_e__FixedBuffer cone_axis;

        public float cone_cutoff;

        [NativeTypeName("signed char[3]")]
        public _cone_axis_s8_e__FixedBuffer cone_axis_s8;

        [NativeTypeName("signed char")]
        public sbyte cone_cutoff_s8;

        [InlineArray(3)]
        public partial struct _center_e__FixedBuffer
        {
            public float e0;
        }

        [InlineArray(3)]
        public partial struct _cone_apex_e__FixedBuffer
        {
            public float e0;
        }

        [InlineArray(3)]
        public partial struct _cone_axis_e__FixedBuffer
        {
            public float e0;
        }

        [InlineArray(3)]
        public partial struct _cone_axis_s8_e__FixedBuffer
        {
            public sbyte e0;
        }
    }
}
