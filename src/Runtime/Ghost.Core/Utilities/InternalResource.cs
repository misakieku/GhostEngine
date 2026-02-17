using Ghost.Core.Contracts;

namespace Ghost.Core.Utilities;

internal static class InternalResource
{
    public static void Release<T>(ref T? resource)
        where T : IReleasable
    {
        resource?.InternalRelease();
    }
}