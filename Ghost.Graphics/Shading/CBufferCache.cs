using Ghost.Graphics.D3D12;
using Misaki.HighPerformance.LowLevel.Buffer;
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

    public GraphicsBuffer GpuResource
    {
        get;
    }

    private readonly uint _alignedSize;

    internal unsafe CBufferCache(uint bufferSize)
    {
        _alignedSize = (bufferSize + 255u) & ~255u;

        CpuData = new((int)_alignedSize, Allocator.Persistent);
        GpuResource = GraphicsBuffer.Create(_alignedSize, GraphicsBuffer.Usage.Constant);
        GpuResource.Name = "Material_CBufferCache";

        UploadToGpu();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void UploadToGpu()
    {
        GpuResource.SetData(CpuData.AsSpan(), 0);
    }

    public readonly void Dispose()
    {
        CpuData.Dispose();
        GpuResource.Dispose();
    }
}