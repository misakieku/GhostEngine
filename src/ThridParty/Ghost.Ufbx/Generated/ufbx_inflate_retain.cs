using System.Runtime.CompilerServices;

namespace Ghost.Ufbx
{
    public partial struct ufbx_inflate_retain
    {
        [NativeTypeName("_Bool")]
        public bool initialized;

        [NativeTypeName("uint64_t[1024]")]
        public _data_e__FixedBuffer data;

        [InlineArray(1024)]
        public partial struct _data_e__FixedBuffer
        {
            public ulong e0;
        }
    }
}
