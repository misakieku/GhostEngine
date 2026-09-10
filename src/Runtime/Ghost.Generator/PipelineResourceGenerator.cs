using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

namespace Ghost.Generator;

[Generator]
public class PipelineResourceGenerator : IIncrementalGenerator
{
    private enum MemberKind
    {
        Handle,
        AssetRef,
        AssetEntry,
        Unsupported
    }

    private readonly struct ResourceMemberInfo
    {
        public string Name { get; }
        public ITypeSymbol Type { get; }
        public MemberKind Kind { get; }
        public string VirtualPath { get; }

        public ResourceMemberInfo(string name, ITypeSymbol type, MemberKind kind, string virtualPath)
        {
            Name = name;
            Type = type;
            Kind = kind;
            VirtualPath = virtualPath;
        }
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is TypeDeclarationSyntax tds && tds.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)),
                transform: static (ctx, ct) => GetTargetType(ctx, ct))
            .Where(static symbol => symbol != null)
            .Collect();

        context.RegisterSourceOutput(candidates, static (spc, types) => Generate(spc, types));
    }

    private static INamedTypeSymbol? GetTargetType(GeneratorSyntaxContext ctx, CancellationToken _)
    {
        var typeDecl = (TypeDeclarationSyntax)ctx.Node;
        if (ctx.SemanticModel.GetDeclaredSymbol(typeDecl) is not INamedTypeSymbol symbol)
        {
            return null;
        }

        // Check if implements IPipelineResource
        var ipipelineResource = ctx.SemanticModel.Compilation.GetTypeByMetadataName("Ghost.Engine.RenderPipeline.IPipelineResource");
        if (ipipelineResource != null && symbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, ipipelineResource)))
        {
            return symbol;
        }

        // Or check if any field or property has ResolveAssetAttribute
        foreach (var member in symbol.GetMembers())
        {
            if (member is IFieldSymbol or IPropertySymbol)
            {
                foreach (var attr in member.GetAttributes())
                {
                    if (attr.AttributeClass?.Name is "ResolveAssetAttribute" or "ResolveAsset" ||
                        attr.AttributeClass?.ToDisplayString() == "Ghost.Engine.Streaming.ResolveAssetAttribute")
                    {
                        return symbol;
                    }
                }
            }
        }

        return null;
    }

    private static MemberKind ClassifyMemberType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol named)
        {
            if (named.IsGenericType && named.Name == "Handle" && named.TypeArguments.Length == 1)
            {
                return MemberKind.Handle;
            }
            if (named.IsGenericType && named.Name == "AssetRef" && named.TypeArguments.Length == 1)
            {
                return MemberKind.AssetRef;
            }
        }

        if (type.Name == "IAssetEntry" || type.AllInterfaces.Any(i => i.Name == "IAssetEntry"))
        {
            return MemberKind.AssetEntry;
        }

        return MemberKind.Unsupported;
    }

    private static void Generate(SourceProductionContext context, ImmutableArray<INamedTypeSymbol?> types)
    {
        if (types.IsDefaultOrEmpty)
        {
            return;
        }

        var distinctTypes = types.Where(t => t != null).Distinct(SymbolEqualityComparer.Default).Cast<INamedTypeSymbol>();

        foreach (var symbol in distinctTypes)
        {
            var resourceMembers = new List<ResourceMemberInfo>();
            foreach (var member in symbol.GetMembers())
            {
                if (member is not (IFieldSymbol or IPropertySymbol))
                {
                    continue;
                }

                var attr = member.GetAttributes().FirstOrDefault(a =>
                    a.AttributeClass?.Name is "ResolveAssetAttribute" or "ResolveAsset" ||
                    a.AttributeClass?.ToDisplayString() == "Ghost.Engine.Streaming.ResolveAssetAttribute");

                if (attr == null)
                {
                    continue;
                }

                string? virtualPath = null;
                if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is string path)
                {
                    virtualPath = path;
                }
                else
                {
                    var named = attr.NamedArguments.FirstOrDefault(kv => kv.Key is "VirtualPath" or "Path");
                    if (named.Value.Value is string np)
                    {
                        virtualPath = np;
                    }
                }

                if (string.IsNullOrEmpty(virtualPath))
                {
                    continue;
                }

                var memberType = member is IFieldSymbol f ? f.Type : ((IPropertySymbol)member).Type;
                var kind = ClassifyMemberType(memberType);
                if (kind == MemberKind.Unsupported)
                {
                    continue;
                }

                resourceMembers.Add(new ResourceMemberInfo(member.Name, memberType, kind, virtualPath!));
            }

            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("#nullable enable");
            sb.AppendLine();

            var ns = symbol.ContainingNamespace.IsGlobalNamespace ? null : symbol.ContainingNamespace.ToDisplayString();
            var indent = "";

            if (ns != null)
            {
                sb.AppendLine($"namespace {ns}");
                sb.AppendLine("{");
                indent = "    ";
            }

            // Containing types hierarchy for nested classes/structs
            var parentList = new List<INamedTypeSymbol>();
            var cur = symbol.ContainingType;
            while (cur != null)
            {
                parentList.Insert(0, cur);
                cur = cur.ContainingType;
            }

            foreach (var parent in parentList)
            {
                var pKind = parent.TypeKind == TypeKind.Struct ? "struct" : "class";
                if (parent.IsRecord)
                {
                    pKind = parent.TypeKind == TypeKind.Struct ? "record struct" : "record class";
                }
                sb.AppendLine($"{indent}partial {pKind} {parent.Name}");
                sb.AppendLine($"{indent}{{");
                indent += "    ";
            }

            string typeKindString = symbol.TypeKind switch
            {
                TypeKind.Struct => "struct",
                _ => "class"
            };
            if (symbol.IsRecord)
            {
                typeKindString = symbol.TypeKind == TypeKind.Struct ? "record struct" : "record class";
            }

            var typeName = symbol.Name;
            if (symbol.IsGenericType)
            {
                typeName += "<" + string.Join(", ", symbol.TypeParameters.Select(p => p.Name)) + ">";
            }

            sb.AppendLine($"{indent}partial {typeKindString} {typeName} : Ghost.Engine.RenderPipeline.IPipelineResource");
            sb.AppendLine($"{indent}{{");

            // Backing asset manager
            sb.AppendLine($"{indent}    private Ghost.Engine.Streaming.AssetManager? _assetManager;");

            // Backing entries for Handle<T> members
            foreach (var m in resourceMembers)
            {
                if (m.Kind == MemberKind.Handle)
                {
                    sb.AppendLine($"{indent}    private Ghost.Engine.Streaming.IAssetEntry? _entry_{m.Name};");
                }
            }

            bool hasExplicitConstructors = symbol.Constructors.Any(c => !c.IsImplicitlyDeclared);
            if (!hasExplicitConstructors && symbol.TypeKind == TypeKind.Class)
            {
                sb.AppendLine();
                sb.AppendLine($"{indent}    public {symbol.Name}() {{ }}");
                sb.AppendLine();
                sb.AppendLine($"{indent}    public {symbol.Name}(Ghost.Engine.Streaming.AssetManager assetManager)");
                sb.AppendLine($"{indent}    {{");
                sb.AppendLine($"{indent}        _assetManager = assetManager;");
                sb.AppendLine($"{indent}    }}");
            }

            // Resolve(AssetManager assetManager)
            sb.AppendLine();
            sb.AppendLine($"{indent}    public void Resolve(Ghost.Engine.Streaming.AssetManager assetManager)");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        _assetManager = assetManager;");

            foreach (var m in resourceMembers)
            {
                var typeStr = m.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                switch (m.Kind)
                {
                    case MemberKind.Handle:
                        sb.AppendLine($"{indent}        _entry_{m.Name} = assetManager.ResolveAsset(\"{m.VirtualPath}\");");
                        sb.AppendLine($"{indent}        var _tmp_{m.Name} = this.{m.Name};");
                        sb.AppendLine($"{indent}        _entry_{m.Name}.ReadAssetData(ref _tmp_{m.Name});");
                        sb.AppendLine($"{indent}        this.{m.Name} = _tmp_{m.Name};");
                        break;
                    case MemberKind.AssetEntry:
                        sb.AppendLine($"{indent}        this.{m.Name} = ({typeStr})assetManager.ResolveAsset(\"{m.VirtualPath}\");");
                        break;
                    case MemberKind.AssetRef:
                        sb.AppendLine($"{indent}        this.{m.Name} = new {typeStr}(assetManager.ResolveAssetID(\"{m.VirtualPath}\"));");
                        break;
                }
            }

            sb.AppendLine($"{indent}        OnResolved();");
            sb.AppendLine($"{indent}    }}");

            // Resolve()
            sb.AppendLine();
            sb.AppendLine($"{indent}    public void Resolve()");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        if (_assetManager == null)");
            sb.AppendLine($"{indent}        {{");
            sb.AppendLine($"{indent}            throw new System.InvalidOperationException(\"AssetManager is not set. Call Resolve(AssetManager) or construct with AssetManager.\");");
            sb.AppendLine($"{indent}        }}");
            sb.AppendLine($"{indent}        Resolve(_assetManager);");
            sb.AppendLine($"{indent}    }}");

            // Lifecycle hooks
            sb.AppendLine();
            sb.AppendLine($"{indent}    partial void OnResolved();");
            sb.AppendLine($"{indent}    partial void OnDisposing();");

            // Dispose()
            sb.AppendLine();
            sb.AppendLine($"{indent}    public void Dispose()");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        OnDisposing();");

            foreach (var m in resourceMembers)
            {
                switch (m.Kind)
                {
                    case MemberKind.Handle:
                        sb.AppendLine($"{indent}        _entry_{m.Name}?.Release();");
                        sb.AppendLine($"{indent}        _entry_{m.Name} = null;");
                        sb.AppendLine($"{indent}        this.{m.Name} = default;");
                        break;
                    case MemberKind.AssetEntry:
                        sb.AppendLine($"{indent}        this.{m.Name}?.Release();");
                        sb.AppendLine($"{indent}        this.{m.Name} = default!;");
                        break;
                    case MemberKind.AssetRef:
                        sb.AppendLine($"{indent}        this.{m.Name} = default;");
                        break;
                }
            }

            sb.AppendLine($"{indent}        _assetManager = null;");
            sb.AppendLine($"{indent}    }}");

            // Close target type
            sb.AppendLine($"{indent}}}");

            // Close containing types
            for (int i = parentList.Count - 1; i >= 0; i--)
            {
                indent = indent.Substring(0, indent.Length - 4);
                sb.AppendLine($"{indent}}}");
            }

            if (ns != null)
            {
                sb.AppendLine("}");
            }

            var safeFileName = symbol.ToDisplayString().Replace(".", "_").Replace("<", "_").Replace(">", "_").Replace("+", "_");
            context.AddSource($"{safeFileName}_PipelineResource.g.cs", sb.ToString());
        }
    }
}
