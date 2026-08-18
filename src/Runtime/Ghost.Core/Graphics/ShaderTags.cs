using System.Runtime.InteropServices;

namespace Ghost.Core.Graphics;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct ShaderInterfaceId(ulong Value)
{
    public override string ToString() => $"0x{Value:X16}";
    public static implicit operator ulong(ShaderInterfaceId id) => id.Value;
    public static explicit operator ShaderInterfaceId(ulong value) => new(value);
}

[StructLayout(LayoutKind.Sequential)]
public readonly record struct ShaderImplementationId(ulong Value)
{
    public override string ToString() => $"0x{Value:X16}";
    public static implicit operator ulong(ShaderImplementationId id) => id.Value;
    public static explicit operator ShaderImplementationId(ulong value) => new(value);
}

[StructLayout(LayoutKind.Sequential)]
public readonly record struct ShaderTemplateId(ulong Value)
{
    public override string ToString() => $"0x{Value:X16}";
    public static implicit operator ulong(ShaderTemplateId id) => id.Value;
    public static explicit operator ShaderTemplateId(ulong value) => new(value);
}

[StructLayout(LayoutKind.Sequential)]
public readonly record struct ShaderId(ulong Value)
{
    public override string ToString() => $"0x{Value:X16}";
    public static implicit operator ulong(ShaderId id) => id.Value;
    public static explicit operator ShaderId(ulong value) => new(value);
}

[StructLayout(LayoutKind.Sequential)]
public readonly record struct ShaderPropertySchemaId(ulong Value)
{
    public override string ToString() => $"0x{Value:X16}";
    public static implicit operator ulong(ShaderPropertySchemaId id) => id.Value;
    public static explicit operator ShaderPropertySchemaId(ulong value) => new(value);
}

public interface IShaderInterfaceTag
{
    static abstract ShaderInterfaceId Id { get; }
}

public interface IShaderImplementationTag<TInterface>
    where TInterface : struct, IShaderInterfaceTag
{
    static abstract ShaderImplementationId Id { get; }
}

public interface IShaderTag
{
    static abstract ShaderId Id { get; }
}

public interface IShaderProperties
{
    static abstract ShaderId ShaderId { get; }
    static abstract ShaderPropertySchemaId SchemaId { get; }
    static abstract uint PropertySize { get; }
}
