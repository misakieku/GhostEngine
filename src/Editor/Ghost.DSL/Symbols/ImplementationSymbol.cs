using Ghost.DSL.ShaderParser.Syntax;

namespace Ghost.DSL.Symbols;

public sealed class ImplementationSymbol
{
    public required string QualifiedName { get; init; }
    public required ulong Id { get; init; }
    public required string InterfaceQualifiedName { get; set; }
    public required ulong InterfaceId { get; set; }
    public required bool IsExported { get; init; }
    public required string SourceFile { get; init; }
    public string? ModuleName { get; init; }
    public string? Provider { get; set; }
    public required string Body { get; init; }
    public required ImplementationDeclarationSyntax Syntax { get; init; }

    public override string ToString()
    {
        return $"implementation {QualifiedName} : {InterfaceQualifiedName} (0x{Id:X16})";
    }
}
