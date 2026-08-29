using System.Runtime.CompilerServices;

namespace Ghost.Ufbx
{
    /// <include file='ufbx_panic.xml' path='doc/member[@name="ufbx_panic"]/*' />
    public partial struct ufbx_panic
    {
        /// <include file='ufbx_panic.xml' path='doc/member[@name="ufbx_panic.did_panic"]/*' />
        [NativeTypeName("_Bool")]
        public bool did_panic;

        /// <include file='ufbx_panic.xml' path='doc/member[@name="ufbx_panic.message_length"]/*' />
        [NativeTypeName("size_t")]
        public nuint message_length;

        /// <include file='ufbx_panic.xml' path='doc/member[@name="ufbx_panic.message"]/*' />
        [NativeTypeName("char[128]")]
        public _message_e__FixedBuffer message;

        /// <include file='_message_e__FixedBuffer.xml' path='doc/member[@name="_message_e__FixedBuffer"]/*' />
        [InlineArray(128)]
        public partial struct _message_e__FixedBuffer
        {
            public sbyte e0;
        }
    }
}
