namespace Ghost.Core;

public unsafe readonly struct ConstPtr<T>
    where T : unmanaged
{
    private readonly T* _ptr;

    public ConstPtr(T* ptr)
    {
        _ptr = ptr;
    }

    public readonly T* Ptr => _ptr;

    public static implicit operator T*(ConstPtr<T> constPtr) => constPtr._ptr;
    public static implicit operator ConstPtr<T>(T* pointer) => new(pointer);
}