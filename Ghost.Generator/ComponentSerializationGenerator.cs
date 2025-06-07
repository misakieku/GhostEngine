using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Ghost.Generator
{
    [Generator]
    public class ComponentSerializationGenerator : IIncrementalGenerator
    {
        private void GenerateJsonContext(SourceProductionContext context, ImmutableArray<INamedTypeSymbol> symbols)
        {
            if (symbols.IsDefaultOrEmpty)
            {
                return;
            }

            var sb = new StringBuilder();
            sb.Append(@"
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Ghost.Engine.Components.Serialization
{
    [JsonSourceGenerationOptions(WriteIndented = true, IncludeFields = true)]");

            foreach (var symbol in symbols.Distinct(SymbolEqualityComparer.Default))
            {
                if (symbol is null)
                {
                    continue;
                }

                var fqtn = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                sb.Append($@"
    [JsonSerializable(typeof({fqtn}))]");
            }

            sb.Append(@"
    public partial class ComponentJsonContext : JsonSerializerContext 
    {
        private static readonly Dictionary<Type, JsonTypeInfo> _typeLookup = new()
        {");

            foreach (var symbol in symbols.Distinct(SymbolEqualityComparer.Default))
            {
                if (symbol is null)
                {
                    continue;
                }

                var fqtn = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                sb.Append($@"
            {{ typeof({fqtn}), ComponentJsonContext.{symbol.Name} }},");
            }

            sb.Append(@"
        };

        /// <summary>
        /// Tries to retrieve the generated JsonTypeInfo for a given component type.
        /// </summary>
        public static bool TryGetTypeInfo(Type componentType, out JsonTypeInfo jsonTypeInfo) =>
            _typeLookup.TryGetValue(componentType, out jsonTypeInfo);
    }
}");

            context.AddSource("ComponentJsonContext.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var componentCandidates = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: (syntaxNode, _) => syntaxNode is StructDeclarationSyntax,
                    transform: (ctx, _) =>
                    {
                        var structSyntax = (StructDeclarationSyntax)ctx.Node;

                        if (!(ctx.SemanticModel.GetDeclaredSymbol(structSyntax) is INamedTypeSymbol symbol))
                        {
                            return null;
                        }

                        var compilation = ctx.SemanticModel.Compilation;
                        var iComponentDataSymbol = compilation.GetTypeByMetadataName("Ghost.Entities.Components.IComponentData");

                        if (iComponentDataSymbol == null)
                        {
                            return null;
                        }

                        foreach (var iface in symbol.AllInterfaces)
                        {
                            if (SymbolEqualityComparer.Default.Equals(iface, iComponentDataSymbol))
                            {
                                return symbol;
                            }
                        }

                        return null;
                    })
                .Where(symbol => symbol != null)
                .Collect();

            context.RegisterSourceOutput(componentCandidates, GenerateJsonContext);
        }
    }
}
