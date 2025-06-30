namespace Ghost.Graphics.Contracts;

public interface IResource : IDisposable
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

    public void SetData<T>(Span<T> data)
        where T : unmanaged;
}