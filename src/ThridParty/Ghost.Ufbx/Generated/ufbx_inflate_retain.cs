using System.Runtime.CompilerServices;

namespace Ghost.Ufbx
{
    /// <include file='ufbx_inflate_retain.xml' path='doc/member[@name="ufbx_inflate_retain"]/*' />
    public partial struct ufbx_inflate_retain
    {
        /// <include file='ufbx_inflate_retain.xml' path='doc/member[@name="ufbx_inflate_retain.initialized"]/*' />
        [NativeTypeName("_Bool")]
        public bool initialized;

        /// <include file='ufbx_inflate_retain.xml' path='doc/member[@name="ufbx_inflate_retain.data"]/*' />
        [NativeTypeName("uint64_t[1024]")]
        public _data_e__FixedBuffer data;

        /// <include file='_data_e__FixedBuffer.xml' path='doc/member[@name="_data_e__FixedBuffer"]/*' />
        [InlineArray(1024)]
        public partial struct _data_e__FixedBuffer
        {
            public ulong e0;
        }
    }
}
