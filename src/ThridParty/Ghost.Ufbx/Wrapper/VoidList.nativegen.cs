namespace Ghost.Ufbx;

public unsafe readonly ref struct VoidList
{
    private readonly void* _data;
    public int Count { get; }

    internal VoidList(void* data, nuint count)
    {
        _data = data;
        Count = checked((int)count);
    }

    public void* Data => _data;
}
