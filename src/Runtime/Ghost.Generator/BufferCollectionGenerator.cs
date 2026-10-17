using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Ghost.Generator;

//[Generator]
internal class BufferCollectionGenerator // : IIncrementalGenerator
{
    private class BufferInfo
    {
        public INamedTypeSymbol Symbol { get; }
        public int Capacity { get; }
        public BufferInfo(INamedTypeSymbol symbol, int capacity)
        {
            Symbol = symbol;
            Capacity = capacity;
        }
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var collection = context.SyntaxProvider.ForAttributeWithMetadataName(
                "Ghost.Entities.InlineBufferCapacityAttribute",
                predicate: (node, _) => node is StructDeclarationSyntax,
                transform: GetBufferSymbol)
            .Where(x => x != null)
            .Collect();

        context.RegisterSourceOutput(collection, GenerateBufferCollectionType);
    }

    private BufferInfo? GetBufferSymbol(GeneratorAttributeSyntaxContext context, CancellationToken token)
    {
        var node = (StructDeclarationSyntax)context.TargetNode;
        if (context.SemanticModel.GetDeclaredSymbol(node) is not INamedTypeSymbol symbol)
        {
            return null;
        }

        var attributeData = context.Attributes.FirstOrDefault(ad => ad.AttributeClass?.ToDisplayString() == "Ghost.Entities.InlineBufferCapacityAttribute");
        if (attributeData == null)
        {
            return null;
        }

        var capacity = attributeData.ConstructorArguments.FirstOrDefault().Value as int?;
        if (!capacity.HasValue)
        {
            return null;
        }

        return new BufferInfo(symbol, capacity.Value);
    }


    private void GenerateBufferCollectionType(SourceProductionContext context, ImmutableArray<BufferInfo?> array)
    {
        throw new NotImplementedException();
    }
}
