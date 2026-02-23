using System.Runtime.CompilerServices;

namespace Ghost.Nvtt.Native
{
    public unsafe partial struct NvttRefImage
    {
        [NativeTypeName("const void *")]
        public void* data;

        public int width;

        public int height;

        public int depth;

        public int num_channels;

        [NativeTypeName("NvttChannelOrder[4]")]
        public _channel_swizzle_e__FixedBuffer channel_swizzle;

        public NvttBoolean channel_interleave;

        [InlineArray(4)]
        public partial struct _channel_swizzle_e__FixedBuffer
        {
            public NvttChannelOrder e0;
        }
    }
}
