using Ghost.DSL.Symbols;

namespace Ghost.DSL.Composition;

public sealed class PassSpecialization
{
    public required ulong CompositionKey { get; init; }
    public required IReadOnlyDictionary<ulong, ulong> Bindings { get; init; }
    public required IReadOnlyList<ImplementationSymbol> Implementations { get; init; }
    public required IReadOnlyList<string> CompilerDefines { get; init; }
    public required IReadOnlyList<string> RequiredFeatureProviders { get; init; }

    public override string ToString()
    {
        return $"Specialization [0x{CompositionKey:X16}] ({Implementations.Count} bindings)";
    }
}
