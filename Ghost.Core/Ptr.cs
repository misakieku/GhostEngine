using System.Runtime.CompilerServices;

namespace Ghost.Core;

public unsafe readonly struct Ptr<T>
    where T : unmanaged
{
    public readonly T* value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Ptr(T* value)
    {
        this.value = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T*(Ptr<T> ptr)
    {
        return ptr.value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Ptr<T>(T* value)
    {
        return new Ptr<T>(value);
    }
}
