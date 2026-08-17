namespace Ghost.DSL.Symbols;

public sealed class ModuleSymbol
{
    public required string Name { get; init; }
    public required string SourceFile { get; init; }
    public HashSet<string> Imports { get; init; } = new(StringComparer.Ordinal);
    public Dictionary<string, InterfaceSymbol> Interfaces { get; init; } = new(StringComparer.Ordinal);
    public Dictionary<string, ImplementationSymbol> Implementations { get; init; } = new(StringComparer.Ordinal);
    public Dictionary<string, TemplateSymbol> Templates { get; init; } = new(StringComparer.Ordinal);
    public Dictionary<string, ShaderSymbol> Shaders { get; init; } = new(StringComparer.Ordinal);

    public override string ToString()
    {
        return $"module \"{Name}\" ({Interfaces.Count} ifaces, {Implementations.Count} impls, {Templates.Count} tmpls, {Shaders.Count} shdrs)";
    }
}
