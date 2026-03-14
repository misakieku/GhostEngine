using System.Runtime.CompilerServices;

namespace Ghost.Ufbx
{
    public partial struct ufbx_error
    {
        public ufbx_error_type type;

        public ufbx_string description;

        [NativeTypeName("uint32_t")]
        public uint stack_size;

        [NativeTypeName("ufbx_error_frame[8]")]
        public _stack_e__FixedBuffer stack;

        [NativeTypeName("size_t")]
        public nuint info_length;

        [NativeTypeName("char[256]")]
        public _info_e__FixedBuffer info;

        [InlineArray(8)]
        public partial struct _stack_e__FixedBuffer
        {
            public ufbx_error_frame e0;
        }

        [InlineArray(256)]
        public partial struct _info_e__FixedBuffer
        {
            public sbyte e0;
        }
    }
}
