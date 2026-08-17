using Ghost.DSL.ShaderParser.Syntax;

namespace Ghost.DSL.Symbols;

public sealed class InterfaceSymbol
{
    public required string QualifiedName { get; init; }
    public required ulong Id { get; init; }
    public required InterfaceScope Scope { get; init; }
    public required bool IsClosed { get; init; }
    public required bool IsExported { get; init; }
    public required string SourceFile { get; init; }
    public string? ModuleName { get; init; }
    public string SignatureBody { get; init; } = string.Empty;
    public required InterfaceDeclarationSyntax Syntax { get; init; }

    public override string ToString()
    {
        var scopeStr = Scope == InterfaceScope.Pipeline ? "pipeline" : "shader";
        var closedStr = IsClosed ? "closed " : "";
        return $"{closedStr}interface {scopeStr} {QualifiedName} (0x{Id:X16})";
    }
}
