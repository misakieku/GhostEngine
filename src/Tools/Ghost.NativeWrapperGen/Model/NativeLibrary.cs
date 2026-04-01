namespace Ghost.NativeWrapperGen.Model;

public sealed class NativeLibrary
{
    public required string NativeNamespace { get; init; }
    public required IReadOnlyList<NativeStruct> Structs { get; init; }
    public required IReadOnlyList<NativeEnum> Enums { get; init; }
    public required IReadOnlyList<NativeFunction> Functions { get; init; }
    public required IReadOnlyDictionary<string, NativeStruct> StructsByName { get; init; }
    public required IReadOnlyDictionary<string, NativeFunction> FunctionsByName { get; init; }
}

public sealed class NativeStruct
{
    public required string Name { get; init; }
    public required string Namespace { get; init; }
    public required IReadOnlyList<NativeMember> Members { get; init; }
    public required bool IsList { get; init; }
    public required bool IsPointerList { get; init; }
    public string? ListElementType { get; init; }
}

public sealed class NativeEnum
{
    public required string Name { get; init; }
    public required IReadOnlyList<string> Members { get; init; }
}

public sealed class NativeFunction
{
    public required string Name { get; init; }
    public required string ReturnType { get; init; }
    public required IReadOnlyList<NativeParameter> Parameters { get; init; }
    public required bool IsDllImport { get; init; }
}

public sealed class NativeParameter
{
    public required string Name { get; init; }
    public required string TypeName { get; init; }

    private string? _rawTypeName;
    public string RawTypeName => _rawTypeName ??= TypeName.TrimEnd('*', '&');
}

public sealed class NativeMember
{
    public required string Name { get; init; }
    public required string TypeName { get; init; }
    public required NativeMemberKind Kind { get; init; }
}

public enum NativeMemberKind
{
    Field,
    Property,
    Constant,
}
