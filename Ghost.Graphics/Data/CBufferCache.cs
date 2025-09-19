using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Graphics.Data;

internal struct CBufferCache
{
    public UnsafeArray<byte> CpuData
    {
        get;
        set;
    }

    public BufferHandle GpuResource
    {
        get;
    }

    private readonly uint _alignedSize;

    internal unsafe CBufferCache(BufferHandle buffer, uint bufferSize)
    {
        _alignedSize = (bufferSize + 255u) & ~255u;

        CpuData = new((int)_alignedSize, Allocator.Persistent);
        GpuResource = buffer;
    }
}