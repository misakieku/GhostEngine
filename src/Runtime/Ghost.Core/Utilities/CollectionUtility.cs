using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Core.Utilities;

public static class CollectionUtility
{
    public static UnsafeArray<T> Clone<T>(this UnsafeArray<T> src, AllocationHandle allocationHandle)
        where T : unmanaged
    {
        if (!src.IsCreated || src.Count == 0)
        {
            return default;
        }

        var dst = new UnsafeArray<T>(src.Count, allocationHandle);
        src.CopyTo(dst);

        return dst;
    }

    public static UnsafeList<T> Clone<T>(this UnsafeList<T> src, AllocationHandle allocationHandle)
        where T : unmanaged
    {
        if (!src.IsCreated || src.Count == 0)
        {
            return default;
        }

        var dst = new UnsafeList<T>(src.Count, allocationHandle);
        src.CopyTo(dst);

        return dst;
    }
}
