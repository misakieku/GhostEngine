using System.Runtime.CompilerServices;

namespace Ghost.Ufbx
{
    public partial struct ufbx_panic
    {
        [NativeTypeName("_Bool")]
        public bool did_panic;

        [NativeTypeName("size_t")]
        public nuint message_length;

        [NativeTypeName("char[128]")]
        public _message_e__FixedBuffer message;

        [InlineArray(128)]
        public partial struct _message_e__FixedBuffer
        {
            public sbyte e0;
        }
    }
}
