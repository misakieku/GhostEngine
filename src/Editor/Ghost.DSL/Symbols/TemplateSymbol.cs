using Ghost.DSL.Properties;
using Ghost.DSL.ShaderParser.Syntax;
namespace Ghost.DSL.Symbols;

public sealed class TemplateSlotSymbol
{
    public required string InterfaceQualifiedName { get; set; }
    public required ulong InterfaceId { get; set; }
    public string? DefaultImplementationQualifiedName { get; set; }
    public ulong? DefaultImplementationId { get; set; }
}

public sealed class TemplatePassSymbol
{
    public required string Name { get; init; }
    public List<string> ComposedInterfaces { get; init; } = new();
    public List<ulong> ComposedInterfaceIds { get; init; } = new();
    public required PassBlockSyntax Syntax { get; init; }
}

public sealed class TemplateSymbol
{
    public required string QualifiedName { get; init; }
    public required ulong Id { get; init; }
    public required bool IsExported { get; init; }
    public required string SourceFile { get; init; }
    public string? ModuleName { get; init; }
    public List<TemplateSlotSymbol> Slots { get; init; } = new();
    public List<TemplatePassSymbol> Passes { get; init; } = new();
    public required TemplateDeclarationSyntax Syntax { get; init; }
    public PropertySchema? PropertySchema { get; set; }

    public override string ToString()
    {
        return $"template \"{QualifiedName}\" ({Passes.Count} passes, 0x{Id:X16})";
    }
}
