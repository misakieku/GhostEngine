using System.Runtime.CompilerServices;

namespace Ghost.Core;

public readonly struct TypeHandle
{
    public readonly IntPtr Value
    {
        get;
    }

    private TypeHandle(IntPtr value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the type handle for the specified type.
    /// </summary>
    /// <param name="type">The type to get the handle for.</param>
    /// <returns>The type handle as a nint.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypeHandle Get(Type type) => new TypeHandle(type.TypeHandle.Value);

    /// <summary>
    /// Gets the type handle for the specified type.
    /// </summary>
    /// <typeparam name="T">The type to get the handle for.</typeparam>
    /// <returns>The type handle as a nint.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypeHandle Get<T>() => Get(typeof(T));

    /// <summary>
    /// Converts a TypeHandle to a Type.
    /// </summary>
    /// <param name="handle">The TypeHandle to convert.</param>
    /// <returns>The corresponding Type.</returns>
    public Type? ToType()
    {
        return Type.GetTypeFromHandle(RuntimeTypeHandle.FromIntPtr(Value));
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static implicit operator TypeHandle(IntPtr value)
    {
        return new TypeHandle(value);
    }

    public static implicit operator IntPtr(TypeHandle handle)
    {
        return handle.Value;
    }

    public static implicit operator TypeHandle(Type type)
    {
        return Get(type);
    }

    public static implicit operator Type?(TypeHandle handle)
    {
        return handle.ToType();
    }
}