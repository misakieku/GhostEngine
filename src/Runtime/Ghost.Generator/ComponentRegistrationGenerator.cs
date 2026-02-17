using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

namespace Ghost.Generator
{
    // TODO: this should be per assembly, not global
    [Generator]
    public class ComponentRegistrationGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // 1. Pipeline: Find all structs implementing IComponent
            var componentCandidates = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: (s, _) => s is StructDeclarationSyntax,
                    transform: GetComponentSymbol)
                .Where(symbol => symbol != null)
                .Collect();

            // 4. Output: Generate the source using both pieces of data at once
            context.RegisterSourceOutput(componentCandidates, GenerateRegistrationCode);
        }

        // Extraction Logic for Components
        private static INamedTypeSymbol GetComponentSymbol(GeneratorSyntaxContext ctx, CancellationToken _)
        {
            var structSyntax = (StructDeclarationSyntax)ctx.Node;
            if (!(ctx.SemanticModel.GetDeclaredSymbol(structSyntax) is INamedTypeSymbol symbol))
            {
                return null;
            }

            var iComponentSymbol = ctx.SemanticModel.Compilation.GetTypeByMetadataName("Ghost.Entities.IComponent");
            if (iComponentSymbol == null)
            {
                return null;
            }

            foreach (var iface in symbol.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(iface, iComponentSymbol))
                {
                    return symbol;
                }
            }

            return null;
        }

        // The Generation Logic (Stateless)
        private static void GenerateRegistrationCode(SourceProductionContext context, ImmutableArray<INamedTypeSymbol> components)
        {
            if (components.IsDefaultOrEmpty)
            {
                return;
            }

            var name = $"g_component_registration";

            var sb = new StringBuilder();
            sb.Append($@"
[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
internal static class {name}
{{
    [global::System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterIComponentTypes()
    {{");

        foreach (var symbol in components.Distinct(SymbolEqualityComparer.Default))
        {
            if (symbol is null) continue;

            sb.Append($@"
        global::Ghost.Entities.ComponentRegistry.GetOrRegisterComponentID<{symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>();");
        }

        sb.Append(@"
    }
}");

            context.AddSource($"{name}.gen.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }
    }
}
