using Ghost.DSL.ShaderParser.Syntax;

namespace Ghost.DSL.Composition;

public sealed class ResolvedPassSpecializationSet
{
    public required string PassName { get; init; }
    public required int PassIndex { get; init; }
    public required bool IsTemplateShared { get; init; }
    public ulong? TemplatePassId { get; init; }
    public required IReadOnlyList<PassSpecialization> Specializations { get; init; }
    public required PassBlockSyntax Syntax { get; init; }

    public override string ToString()
    {
        var sharedStr = IsTemplateShared ? " [Shared]" : "";
        return $"Pass \"{PassName}\"{sharedStr} ({Specializations.Count} specializations)";
    }
}
