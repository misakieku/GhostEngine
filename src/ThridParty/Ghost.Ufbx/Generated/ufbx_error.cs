using System.Runtime.CompilerServices;

namespace Ghost.Ufbx
{
    /// <include file='ufbx_error.xml' path='doc/member[@name="ufbx_error"]/*' />
    public partial struct ufbx_error
    {
        /// <include file='ufbx_error.xml' path='doc/member[@name="ufbx_error.type"]/*' />
        public ufbx_error_type type;

        /// <include file='ufbx_error.xml' path='doc/member[@name="ufbx_error.description"]/*' />
        public ufbx_string description;

        /// <include file='ufbx_error.xml' path='doc/member[@name="ufbx_error.stack_size"]/*' />
        [NativeTypeName("uint32_t")]
        public uint stack_size;

        /// <include file='ufbx_error.xml' path='doc/member[@name="ufbx_error.stack"]/*' />
        [NativeTypeName("ufbx_error_frame[8]")]
        public _stack_e__FixedBuffer stack;

        /// <include file='ufbx_error.xml' path='doc/member[@name="ufbx_error.info_length"]/*' />
        [NativeTypeName("size_t")]
        public nuint info_length;

        /// <include file='ufbx_error.xml' path='doc/member[@name="ufbx_error.info"]/*' />
        [NativeTypeName("char[256]")]
        public _info_e__FixedBuffer info;

        /// <include file='_stack_e__FixedBuffer.xml' path='doc/member[@name="_stack_e__FixedBuffer"]/*' />
        [InlineArray(8)]
        public partial struct _stack_e__FixedBuffer
        {
            public ufbx_error_frame e0;
        }

        /// <include file='_info_e__FixedBuffer.xml' path='doc/member[@name="_info_e__FixedBuffer"]/*' />
        [InlineArray(256)]
        public partial struct _info_e__FixedBuffer
        {
            public sbyte e0;
        }
    }
}
