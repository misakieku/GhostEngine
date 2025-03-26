namespace Ghost.Entities.Helpers;

internal static class ThreadLocker
{
    private static Lock? _worldLock;
    public static Lock WorldLock => _worldLock ??= new();
}