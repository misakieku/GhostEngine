using Ghost.DSL.Symbols;

namespace Ghost.DSL.Composition;

public sealed class ResolvedShaderComposition
{
    public required ShaderSymbol Shader { get; init; }
    public TemplateSymbol? BaseTemplate { get; init; }
    public required IReadOnlyList<ResolvedPassSpecializationSet> Passes { get; init; }

    public int TotalSpecializationCount => Passes.Sum(p => p.Specializations.Count);

    public override string ToString()
    {
        return $"Resolved Composition \"{Shader.QualifiedName}\" ({Passes.Count} passes, {TotalSpecializationCount} total specializations)";
    }
}
