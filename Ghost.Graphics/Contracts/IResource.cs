using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Graphics.Contracts;

public unsafe interface IResource : IDisposable
{
    public ulong GPUAddress
    {
        get;
    }

    public string Name
    {
        get;
        set;
    }

    public bool TempResource
    {
        get;
    }

    public void SetData<T>(Span<T> data)
        where T : unmanaged;

    public void SetData<T>(T* data, uint length)
        where T : unmanaged;

    public void SetData(void* data, uint size);

    public UnsafeArray<T> ReadData<T>(Allocator allocator)
        where T : unmanaged;

    public void ReadData<T>(T* ppData, uint* size)
        where T : unmanaged;

    public void ReadData(void* ppData, uint* size);
}