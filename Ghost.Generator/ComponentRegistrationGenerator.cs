using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

namespace Ghost.Generator
{
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

            // 2. Pipeline: Find the ONE class with [EngineEntry]
            var engineEntryClass = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: (s, _) => s is ClassDeclarationSyntax,
                    transform: GetEngineEntrySymbol)
                .Where(symbol => symbol != null)
                .Collect(); // Returns ImmutableArray<INamedTypeSymbol>

            // 3. COMBINE: Pair the list of components with the list of entry classes
            // The result 'source' in the callback will be a tuple: (ImmutableArray<Info>, ImmutableArray<Entry>)
            var combinedProvider = componentCandidates.Combine(engineEntryClass);

            // 4. Output: Generate the source using both pieces of data at once
            context.RegisterSourceOutput(combinedProvider, GenerateRegistrationCode);
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

        // Extraction Logic for Engine Entry
        private static INamedTypeSymbol GetEngineEntrySymbol(GeneratorSyntaxContext ctx, CancellationToken _)
        {
            var classSyntax = (ClassDeclarationSyntax)ctx.Node;
            if (!(ctx.SemanticModel.GetDeclaredSymbol(classSyntax) is INamedTypeSymbol symbol))
            {
                return null;
            }

            // Check attributes
            foreach (var attribute in symbol.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::Ghost.Engine.EngineEntryAttribute")
                {
                    return symbol;
                }
            }

            return null;
        }

        // The Generation Logic (Stateless)
        private static void GenerateRegistrationCode(
            SourceProductionContext context,
            (ImmutableArray<INamedTypeSymbol> Components, ImmutableArray<INamedTypeSymbol> Entries) source)
        {
            var components = source.Components;
            var entries = source.Entries;

            // 1. Validation: Ensure we found exactly one [EngineEntry] class
            if (entries.IsDefaultOrEmpty)
            {
                return;
            }

            // Pick the first one (if multiple exist, you might want to report a diagnostic error)
            var targetClass = entries[0];

            if (components.IsDefaultOrEmpty)
            {
                return;
            }

            // 2. Extract Namespace and Class Name directly from the symbol found in the pipeline
            var targetNamespace = targetClass.ContainingNamespace.ToDisplayString();
            var targetClassName = targetClass.Name;

            var sb = new StringBuilder();
            sb.Append($@"
namespace {targetNamespace}
{{
    public partial class {targetClassName}
    {{
        private static void RegisterIComponentTypes()
        {{");

            foreach (var symbol in components.Distinct(SymbolEqualityComparer.Default))
            {
                if (symbol is null) continue;

                sb.Append($@"
            global::Ghost.Entities.ComponentRegistry.GetOrRegisterComponent<{symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>();");
            }

            sb.Append(@"
        }
    }
}");

            context.AddSource($"{targetClassName}.ComponentReg.gen.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }
    }
}
