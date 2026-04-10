using System.Runtime.CompilerServices;

namespace Ghost.DXC;

public static unsafe class UUID
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid* __uuidof<T>()
        where T : unmanaged, INativeGuid
    {
        return T.NativeGuid;
    }

    public static Guid* __uuidof<T>(T* _)
        where T : unmanaged, INativeGuid
    {
        return T.NativeGuid;
    }
}