using System.Runtime.CompilerServices;

namespace Ghost.MeshOptimizer
{
    /// <include file='meshopt_Bounds.xml' path='doc/member[@name="meshopt_Bounds"]/*' />
    public partial struct meshopt_Bounds
    {
        /// <include file='meshopt_Bounds.xml' path='doc/member[@name="meshopt_Bounds.center"]/*' />
        [NativeTypeName("float[3]")]
        public _center_e__FixedBuffer center;

        /// <include file='meshopt_Bounds.xml' path='doc/member[@name="meshopt_Bounds.radius"]/*' />
        public float radius;

        /// <include file='meshopt_Bounds.xml' path='doc/member[@name="meshopt_Bounds.cone_apex"]/*' />
        [NativeTypeName("float[3]")]
        public _cone_apex_e__FixedBuffer cone_apex;

        /// <include file='meshopt_Bounds.xml' path='doc/member[@name="meshopt_Bounds.cone_axis"]/*' />
        [NativeTypeName("float[3]")]
        public _cone_axis_e__FixedBuffer cone_axis;

        /// <include file='meshopt_Bounds.xml' path='doc/member[@name="meshopt_Bounds.cone_cutoff"]/*' />
        public float cone_cutoff;

        /// <include file='meshopt_Bounds.xml' path='doc/member[@name="meshopt_Bounds.cone_axis_s8"]/*' />
        [NativeTypeName("signed char[3]")]
        public _cone_axis_s8_e__FixedBuffer cone_axis_s8;

        /// <include file='meshopt_Bounds.xml' path='doc/member[@name="meshopt_Bounds.cone_cutoff_s8"]/*' />
        [NativeTypeName("signed char")]
        public sbyte cone_cutoff_s8;

        /// <include file='_center_e__FixedBuffer.xml' path='doc/member[@name="_center_e__FixedBuffer"]/*' />
        [InlineArray(3)]
        public partial struct _center_e__FixedBuffer
        {
            public float e0;
        }

        /// <include file='_cone_apex_e__FixedBuffer.xml' path='doc/member[@name="_cone_apex_e__FixedBuffer"]/*' />
        [InlineArray(3)]
        public partial struct _cone_apex_e__FixedBuffer
        {
            public float e0;
        }

        /// <include file='_cone_axis_e__FixedBuffer.xml' path='doc/member[@name="_cone_axis_e__FixedBuffer"]/*' />
        [InlineArray(3)]
        public partial struct _cone_axis_e__FixedBuffer
        {
            public float e0;
        }

        /// <include file='_cone_axis_s8_e__FixedBuffer.xml' path='doc/member[@name="_cone_axis_s8_e__FixedBuffer"]/*' />
        [InlineArray(3)]
        public partial struct _cone_axis_s8_e__FixedBuffer
        {
            public sbyte e0;
        }
    }
}
