using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ghost.NativeWrapperGen.Config;

public sealed class WrapperConfig
{
    public required string LibraryName { get; init; }
    public required string NativeNamespace { get; init; }
    public required string OutputNamespace { get; init; }
    public required string NativeTypePrefix { get; init; }
    public List<string> SkipTypes { get; init; } = [];
    public List<string> SkipFunctions { get; init; } = [];
    public List<RemapConfig> Remaps { get; init; } = [];
    public List<ActionConfig> Actions { get; init; } = [];
}

/// <summary>
/// Describes how to remap a native type to a C# type at a call site.
/// </summary>
public sealed class RemapConfig
{
    /// <summary>Native C# type to match (e.g. "sbyte*", "ufbx_string").</summary>
    public required string Src { get; init; }
    /// <summary>C# type to expose in the generated method signature (e.g. "ReadOnlySpan&lt;byte&gt;").</summary>
    public required string Dst { get; init; }
    /// <summary>Which scopes this remap applies to: "parameter" and/or "return".</summary>
    public List<string> Scope { get; init; } = [];
    /// <summary>Optional regex patterns applied to parameter names. If specified, only matching params are remapped.</summary>
    public List<string>? Filter { get; init; }
    /// <summary>If set, a sibling parameter with this suffix is consumed and replaced by the given expression.</summary>
    public DerivesFromConfig? DerivesFrom { get; init; }
    /// <summary>How to convert between src and dst.</summary>
    public AdapterConfig? Adapter { get; init; }
}

/// <summary>
/// Describes a parameter that is derived from another (e.g. name_len derived from name.Length).
/// </summary>
public sealed class DerivesFromConfig
{
    /// <summary>The prefix of the sibling parameter name to consume (e.g. "name_").</summary>
    public string ParamPrefix { get; init; } = string.Empty;
    /// <summary>The suffix of the sibling parameter name to consume (e.g. "_len").</summary>
    public string ParamSuffix { get; init; } = string.Empty;
    public string RegexName { get; init; } = string.Empty;
    /// <summary>Expression to pass in place of the consumed parameter. $arg is replaced with the source param name.</summary>
    public required string Expr { get; init; }
}

/// <summary>
/// Adapter: how to convert between src (native) and dst (C#) types.
/// </summary>
public sealed class AdapterConfig
{
    /// <summary>dst → src conversion: wraps the call site and substitutes the argument.</summary>
    public ConvertBackConfig? ConvertBack { get; init; }
    /// <summary>src → dst conversion expression (for return values). $result is replaced with the src expression.</summary>
    public string? ConvertTo { get; init; }
}

/// <summary>
/// Specifies how to wrap the generated call and what expression to pass for the remapped parameter.
/// Magic variables: $arg = C# parameter name, $CALL = the full Api.xxx(...) call expression.
/// </summary>
public sealed class ConvertBackConfig
{
    /// <summary>
    /// Template that wraps the entire call. Use $arg for the parameter name, $CALL for the call.
    /// Example: "fixed (byte* p$arg = $arg) { $CALL }"
    /// </summary>
    public required string WrapCall { get; init; }
    /// <summary>
    /// Expression passed as the native argument. Use $arg for the parameter name.
    /// Example: "(sbyte*)p$arg"
    /// </summary>
    public required string PassAs { get; init; }
}

/// <summary>
/// Routing rule: determines where a function gets emitted and as what kind of method.
/// </summary>
public sealed class ActionConfig
{
    /// <summary>"EXTERN_API" = all DllImport methods on the Api class.</summary>
    public required string Filter { get; init; }
    /// <summary>
    /// Conditions that must all be true:
    ///   "SELF_PTR"                 — first param type is T* where T is a known binding struct
    ///   "FIRST_PARAM_OTHER_TYPE"   — first param is NOT a known struct pointer
    ///   "RETURN_BINDING_TYPE"      — return type is T* where T is a known binding struct
    ///   "VOID_RETURN"              — return type is void
    ///   "NAME_CONDITION(regex)"    — native function name matches the regex ($TSelf/$TBare substituted)
    /// </summary>
    public List<string> Conditions { get; init; } = [];
    /// <summary>"FIRST_PARAM_TYPE" or "RETURN_TYPE" — which type the method is placed on.</summary>
    public required string TargetType { get; init; }
    /// <summary>
    /// One or more apply steps. In JSON can be a single object or an array.
    /// Supported types: "INSTANCE_METHOD", "STATIC_METHOD", "INHERITANCE".
    /// </summary>
    [JsonConverter(typeof(ActionApplyListConverter))]
    public required List<ActionApplyConfig> Apply { get; init; }
    public string? Comment { get; init; }
}

/// <summary>
/// Describes a single apply step within an action.
/// </summary>
public sealed class ActionApplyConfig
{
    /// <summary>"INSTANCE_METHOD", "STATIC_METHOD", or "INHERITANCE".</summary>
    public required string Type { get; init; }
    /// <summary>Optional per-apply-step options.</summary>
    public ApplyOptsConfig? Opts { get; init; }
}

/// <summary>
/// Strongly-typed options for an <see cref="ActionApplyConfig"/> step.
/// </summary>
public sealed class ApplyOptsConfig
{
    /// <summary>If true, skip the first native parameter from the public signature [INSTANCE_METHOD].</summary>
    public bool? RemoveFirstParam { get; init; }
    /// <summary>Self-pointer expression passed as the first native argument ($TSelf substituted) [INSTANCE_METHOD].</summary>
    public string? PassAs { get; init; }
    /// <summary>Method naming overrides [INSTANCE_METHOD / STATIC_METHOD].</summary>
    public NameOptsConfig? Name { get; init; }
    /// <summary>List of base type / interface names to add to the partial struct [INHERITANCE].</summary>
    public List<string>? BaseType { get; init; }
}

/// <summary>
/// Options controlling how a native function name is transformed into a C# method name.
/// </summary>
public sealed class NameOptsConfig
{
    /// <summary>Fixed method name override.</summary>
    public string? Set { get; init; }
    /// <summary>Ordered list of prefix/token removals (e.g. "PREFIX", "$TBare").</summary>
    public List<string>? Remove { get; init; }
    /// <summary>Target casing style (e.g. "PascalCase", "ALL_CAPS").</summary>
    public string? Style { get; init; }
}

/// <summary>
/// Deserializes the "apply" JSON field as either a single object or an array of objects,
/// always producing a List&lt;ActionApplyConfig&gt;.
/// </summary>
internal sealed class ActionApplyListConverter : JsonConverter<List<ActionApplyConfig>>
{
    public override List<ActionApplyConfig> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            return JsonSerializer.Deserialize<List<ActionApplyConfig>>(ref reader, options)
                   ?? [];
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            var single = JsonSerializer.Deserialize<ActionApplyConfig>(ref reader, options);
            return single is not null ? [single] : [];
        }

        throw new JsonException($"Expected object or array for 'apply', got {reader.TokenType}.");
    }

    public override void Write(Utf8JsonWriter writer, List<ActionApplyConfig> value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value, options);
}
