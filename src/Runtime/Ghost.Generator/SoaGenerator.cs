using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Ghost.Generator;

[Generator]
internal class SoaGenerator : IIncrementalGenerator
{
    private class SoaGenerationContext
    {
        public INamedTypeSymbol TargetType { get; }
        public bool Unmanaged { get; }
        public SoaGenerationContext(INamedTypeSymbol targetType, bool unmanaged)
        {
            TargetType = targetType;
            Unmanaged = unmanaged;
        }
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var soaCandidates = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "Ghost.Core.SoaGenerateAttribute",
                (s, _) => s is ClassDeclarationSyntax || s is StructDeclarationSyntax || s is RecordDeclarationSyntax,
                (ctx, ct) =>
                {
                    var attributeData = ctx.Attributes.FirstOrDefault(attr => attr.AttributeClass?.ToDisplayString() == "Ghost.Core.SoaGenerateAttribute");
                    if (attributeData == null)
                    {
                        return null;
                    }

                    return new SoaGenerationContext((INamedTypeSymbol)ctx.TargetSymbol, (bool)attributeData.ConstructorArguments[0].Value!);
                })
            .Where(ctx => ctx != null)
            .Collect();

        context.RegisterSourceOutput(soaCandidates, GenerateSoaCode);
    }

    private void GenerateSoaCode(SourceProductionContext context, ImmutableArray<SoaGenerationContext?> array)
    {
        if (array.Length == 0)
        {
            return;
        }

        foreach (var item in array)
        {
            if (item == null)
            {
                continue;
            }

            var symbol = item.TargetType;

            var sb = new StringBuilder();
            
            sb.AppendLine($"// Auto-generated SoA for {symbol.Name}");
            sb.AppendLine($"{symbol.DeclaredAccessibility.ToString().ToLower()} struct {symbol.Name}SoA");
            sb.AppendLine("{");
            
            foreach (var member in symbol.GetMembers().OfType<IFieldSymbol>())
            {
                sb.AppendLine($"    public {member.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}[] {member.Name}Array;");
            }
            
            sb.AppendLine("}");

            context.AddSource($"{symbol.Name}SoA.g.cs", sb.ToString());
        }
    }
}
