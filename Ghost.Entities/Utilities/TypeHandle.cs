namespace Ghost.Entities.Utilities;

internal static class TypeHandle<T>
{
    public static nint Value => typeof(T).TypeHandle.Value;
}