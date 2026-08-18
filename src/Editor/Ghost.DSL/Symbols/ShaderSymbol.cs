using Ghost.DSL.Properties;
using Ghost.DSL.ShaderParser.Syntax;
namespace Ghost.DSL.Symbols;

public sealed class ShaderSymbol
{
    public required string QualifiedName { get; init; }
    public required ulong Id { get; init; }
    public string? BaseTemplateQualifiedName { get; set; }
    public ulong? BaseTemplateId { get; set; }
    public required bool IsExported { get; init; }
    public required string SourceFile { get; init; }
    public string? ModuleName { get; init; }
    public string? PayloadBody { get; set; }
    public Dictionary<string, ImplementationSymbol> LocalImplementations { get; init; } = new(StringComparer.Ordinal);
    public Dictionary<ulong, ulong> Bindings { get; init; } = new();
    public required ShaderDeclarationSyntax Syntax { get; init; }
    public PropertySchema? PropertySchema { get; set; }

    public override string ToString()
    {
        var baseStr = BaseTemplateQualifiedName != null ? $" : \"{BaseTemplateQualifiedName}\"" : "";
        return $"shader \"{QualifiedName}\"{baseStr} (0x{Id:X16})";
    }
}
