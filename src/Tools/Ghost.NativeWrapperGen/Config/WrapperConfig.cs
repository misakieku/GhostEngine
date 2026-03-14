namespace Ghost.NativeWrapperGen.Config;

public sealed class WrapperConfig
{
    public required string LibraryName { get; init; }
    public required string NativeNamespace { get; init; }
    public required string WrapperNamespace { get; init; }
    public required string NativeTypePrefix { get; init; }
    public string? StaticApiClassName { get; init; }
    public List<string> SkipTypes { get; init; } = [];
    public Dictionary<string, string> TypeNameOverrides { get; init; } = new(StringComparer.Ordinal);
    public Dictionary<string, string> PublicTypeOverrides { get; init; } = new(StringComparer.Ordinal);
    public WrapperKindsConfig Wrappers { get; init; } = new();
    public SpecialTypesConfig SpecialTypes { get; init; } = new();
    public List<OwnedTypeConfig> OwnedTypes { get; init; } = [];
    /// <summary>
    /// Manual overrides for specific functions (adapters, special parameters, etc.).
    /// Functions not listed here are auto-routed by the 3-rule dispatch logic.
    /// </summary>
    public List<StaticMethodConfig> StaticMethods { get; init; } = [];
    public List<MarshalledTypeConfig> MarshalledTypes { get; init; } = [];
    public List<string> PartialTypes { get; init; } = [];
    /// <summary>
    /// Native types that are treated as plain value types in wrapper land (not wrapped in a pointer struct).
    /// Functions returning or taking these as non-pointer params pass them by value.
    /// </summary>
    public List<string> SkipFunctionPrefixes { get; init; } = [];
}

public sealed class WrapperKindsConfig
{
    public string DefaultKind { get; init; } = "struct";
    public string DefaultOwnedKind { get; init; } = "class";
    public Dictionary<string, string> Kinds { get; init; } = new(StringComparer.Ordinal);
}

public sealed class SpecialTypesConfig
{
    public List<StringTypeConfig> Strings { get; init; } = [];
    public List<BlobTypeConfig> Blobs { get; init; } = [];
}

public sealed class StringTypeConfig
{
    public required string Type { get; init; }
    public string DataField { get; init; } = "data";
    public string LengthField { get; init; } = "length";
    public int CharSize { get; init; } = 8;
    public string Encoding { get; init; } = "utf8";
    public bool EmitRawSpanProperty { get; init; } = true;
    public bool EmitStringProperty { get; init; } = true;
}

public sealed class BlobTypeConfig
{
    public required string Type { get; init; }
    public string DataField { get; init; } = "data";
    public string LengthField { get; init; } = "size";
    public string ElementType { get; init; } = "byte";
}

public sealed class OwnedTypeConfig
{
    public required string NativeType { get; init; }
    public string? FreeFunction { get; init; }
    public string? RetainFunction { get; init; }
    public string? WrapperKind { get; init; }
    public string? StaticType { get; init; }
}

public sealed class StaticMethodConfig
{
    public required string NativeFunction { get; init; }
    public string? MethodName { get; init; }
    public string? StaticType { get; init; }
    public bool ThrowOnNullReturn { get; init; }
    public string? FailureMessageMember { get; init; }
    public List<StaticParameterConfig> Parameters { get; init; } = [];
}

public sealed class StaticParameterConfig
{
    public required string Native { get; init; }
    public required string Adapter { get; init; }
    public string? PublicName { get; init; }
    public string? Type { get; init; }
    public string? Source { get; init; }
    public bool OptionalDefault { get; init; }
}

public sealed class MarshalledTypeConfig
{
    public required string NativeType { get; init; }
    /// <summary>
    /// Fields that need special (user-implemented partial) marshalling logic.
    /// The generator emits a backing field + partial property stub for these;
    /// the Dispose() partial stub is always emitted so the user can free them.
    /// </summary>
    public List<MarshalledPropertyConfig> MarshalledProperties { get; init; } = [];
}

public sealed class MarshalledPropertyConfig
{
    /// <summary>Native field name (snake_case).</summary>
    public required string Native { get; init; }
    /// <summary>The C# wrapper type for this field (e.g. "cstring").</summary>
    public required string Type { get; init; }
}
