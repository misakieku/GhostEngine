using Ghost.Graphics.D3D12;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Helpers;
using System.Runtime.CompilerServices;

namespace Ghost.Graphics.Shading;

internal struct CBufferCache : IDisposable
{
    public UnsafeArray<byte> CpuData
    {
        get;
        set;
    }

    public GraphicsResource GpuResource
    {
        get;
    }

    private readonly uint _alignedSize;

    public unsafe CBufferCache(uint bufferSize)
    {
        _alignedSize = (bufferSize + 255u) & ~255u;

        CpuData = new((int)_alignedSize, Allocator.Persistent);
        GpuResource = GraphicsPipeline.ResourceAllocator.CreateUploadBuffer(_alignedSize);
        GpuResource.Name = "Material_CBufferCache";

        UploadToGpu();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void UploadToGpu()
    {
        GpuResource.SetData(CpuData.AsSpan());
    }

    public readonly void Dispose()
    {
        GpuResource.Dispose();
    }
}