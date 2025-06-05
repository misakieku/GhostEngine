using System.Runtime.CompilerServices;

namespace Ghost.Entities.Utilities;

internal static class TypeHandle
{
    /// <summary>
    /// Gets the type handle for the specified type.
    /// </summary>
    /// <typeparam name="T">The type to get the handle for.</typeparam>
    /// <returns>The type handle as a nint.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nint Get<T>()
    {
        return typeof(T).TypeHandle.Value;
    }

    /// <summary>
    /// Gets the type handle for the specified type.
    /// </summary>
    /// <param name="type">The type to get the handle for.</param>
    /// <returns>The type handle as a nint.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nint Get(Type type)
    {
        return type.TypeHandle.Value;
    }
}